[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",

    [string]$OutputRoot,

    [string]$PackageRoot,

    [switch]$SelfContained,

    [switch]$CreateArchive,

    [switch]$SkipTests,

    [switch]$Restore
)

$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $repoRoot ".tools\dotnet\dotnet.exe"
$testScript = Join-Path $PSScriptRoot "test.ps1"

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts\publish"
}

if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    $PackageRoot = Join-Path $repoRoot "artifacts\release"
}

function Get-FullPath {
    param([string]$Path)

    return [System.IO.Path]::GetFullPath($Path)
}

function Get-ReleaseVersion {
    $propsPath = Join-Path $repoRoot "Directory.Build.props"
    if (-not (Test-Path $propsPath)) {
        return "0.0.0"
    }

    [xml]$props = Get-Content -Raw $propsPath
    $version = $props.Project.PropertyGroup.VersionPrefix | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($version)) {
        return "0.0.0"
    }

    return $version
}

function Test-IsPathInside {
    param(
        [string]$Candidate,
        [string]$Root
    )

    $candidatePath = Get-FullPath $Candidate
    $rootPath = (Get-FullPath $Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    return $candidatePath.Equals($rootPath, [System.StringComparison]::OrdinalIgnoreCase) `
        -or $candidatePath.StartsWith($rootPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) `
        -or $candidatePath.StartsWith($rootPath + [System.IO.Path]::AltDirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-SafeOutputRoot {
    param([string]$Path)

    $fullPath = Get-FullPath $Path
    $systemRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    if (-not [string]::IsNullOrWhiteSpace($systemRoot) -and (Test-IsPathInside $fullPath $systemRoot)) {
        throw "OutputRoot must not be inside the Windows directory."
    }

    foreach ($reserved in @("src", "tests", "docs", ".tools")) {
        $reservedPath = Join-Path $repoRoot $reserved
        if (Test-IsPathInside $fullPath $reservedPath) {
            throw "OutputRoot must not be inside $reserved."
        }
    }
}

if (-not (Test-Path $dotnet)) {
    & (Join-Path $PSScriptRoot "bootstrap-dotnet.ps1")
}

Assert-SafeOutputRoot $OutputRoot
if ($CreateArchive) {
    Assert-SafeOutputRoot $PackageRoot
}

if (-not $SkipTests) {
    if ($Restore) {
        & $testScript -Restore
    }
    else {
        & $testScript
    }

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
elseif ($Restore) {
    & $dotnet restore (Join-Path $repoRoot "WinSafeClean.sln")
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$projects = @(
    @{
        Name = "WinSafeClean.Cli"
        Path = Join-Path $repoRoot "src\WinSafeClean.Cli\WinSafeClean.Cli.csproj"
    },
    @{
        Name = "WinSafeClean.Ui"
        Path = Join-Path $repoRoot "src\WinSafeClean.Ui\WinSafeClean.Ui.csproj"
    }
)

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

foreach ($project in $projects) {
    $output = Join-Path $OutputRoot $project.Name
    $description = "$($project.Name) to $output"
    if ($PSCmdlet.ShouldProcess($description, "dotnet publish")) {
        & $dotnet publish $project.Path `
            --configuration $Configuration `
            --runtime $Runtime `
            --self-contained $selfContainedValue `
            --output $output

        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}

Write-Host "Publish output root: $OutputRoot"

if ($CreateArchive) {
    $releaseVersion = Get-ReleaseVersion
    $archivePaths = @()

    if ($PSCmdlet.ShouldProcess($PackageRoot, "create release package directory")) {
        New-Item -ItemType Directory -Force -Path $PackageRoot | Out-Null
    }

    foreach ($project in $projects) {
        $source = Join-Path $OutputRoot $project.Name
        if (-not $WhatIfPreference -and -not (Test-Path $source)) {
            throw "Publish output is missing for $($project.Name)."
        }

        $archivePath = Join-Path $PackageRoot "$($project.Name)-$releaseVersion-$Runtime.zip"
        if ($PSCmdlet.ShouldProcess($archivePath, "create release archive")) {
            Compress-Archive -Path (Join-Path $source "*") -DestinationPath $archivePath -Force
            $archivePaths += $archivePath
        }
    }

    $manifestPath = Join-Path $PackageRoot "SHA256SUMS.txt"
    if ($archivePaths.Count -gt 0 -and $PSCmdlet.ShouldProcess($manifestPath, "write checksum manifest")) {
        $hashLines = $archivePaths |
            Sort-Object |
            ForEach-Object {
                $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $_
                "$($hash.Hash.ToLowerInvariant())  $(Split-Path -Leaf $_)"
            }

        Set-Content -LiteralPath $manifestPath -Value $hashLines -Encoding UTF8
    }

    Write-Host "Release package root: $PackageRoot"
}
