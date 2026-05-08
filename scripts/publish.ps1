[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",

    [string]$OutputRoot,

    [switch]$SelfContained,

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

function Get-FullPath {
    param([string]$Path)

    return [System.IO.Path]::GetFullPath($Path)
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
