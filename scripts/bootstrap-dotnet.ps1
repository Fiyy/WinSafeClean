param(
    [string]$Channel = "8.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$toolDir = Join-Path $repoRoot ".tools"
$dotnetDir = Join-Path $toolDir "dotnet"
$installer = Join-Path $toolDir "dotnet-install.ps1"

New-Item -ItemType Directory -Force -Path $toolDir | Out-Null

if (-not (Test-Path $installer)) {
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installer
}

pwsh -NoProfile -ExecutionPolicy Bypass -File $installer -Channel $Channel -InstallDir $dotnetDir -NoPath

& (Join-Path $dotnetDir "dotnet.exe") --info
