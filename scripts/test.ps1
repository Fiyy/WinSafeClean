param(
    [switch]$Restore
)

$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $repoRoot ".tools\dotnet\dotnet.exe"
$solution = Join-Path $repoRoot "WinSafeClean.sln"

if (-not (Test-Path $dotnet)) {
    & (Join-Path $PSScriptRoot "bootstrap-dotnet.ps1")
}

if ($Restore) {
    & $dotnet restore $solution
}

& $dotnet test $solution --no-restore
