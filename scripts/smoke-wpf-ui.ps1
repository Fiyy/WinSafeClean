[CmdletBinding()]
param(
    [string]$AppPath,

    [string]$OutputRoot,

    [int]$WaitSeconds = 15,

    [switch]$KeepOpen
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($AppPath)) {
    $AppPath = Join-Path $repoRoot "artifacts\publish\WinSafeClean.Ui\WinSafeClean.Ui.exe"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts\smoke"
}

if (-not (Test-Path -LiteralPath $AppPath)) {
    throw "WPF UI executable was not found: $AppPath"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$samplePlanPath = Join-Path $repoRoot "tests\WinSafeClean.Core.Tests\Planning\fixtures\cleanup-plan-v0.2.json"
$recentHistoryPath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "WinSafeClean\recent-documents.json"
$recentHistoryParent = Split-Path -Parent $recentHistoryPath
$hadRecentHistory = Test-Path -LiteralPath $recentHistoryPath
$recentHistoryBackup = if ($hadRecentHistory) { Get-Content -Raw -LiteralPath $recentHistoryPath } else { $null }
$preparedRecentHistory = $false

if (Test-Path -LiteralPath $samplePlanPath) {
    New-Item -ItemType Directory -Force -Path $recentHistoryParent | Out-Null
    $recentEntries = @(
        [pscustomobject]@{
            Kind = "CleanupPlan"
            Path = [System.IO.Path]::GetFullPath($samplePlanPath)
            LastOpenedAt = (Get-Date).ToString("o")
        }
    )
    $recentEntries | ConvertTo-Json -AsArray | Set-Content -LiteralPath $recentHistoryPath -Encoding UTF8
    $preparedRecentHistory = $true
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class WinSafeCleanSmokeWindow
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
"@

function Wait-MainWindowHandle {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            throw "WPF UI exited during startup smoke with code $($Process.ExitCode)."
        }

        $Process.Refresh()
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            return $Process.MainWindowHandle
        }

        Start-Sleep -Milliseconds 250
    }

    throw "WPF UI did not create a main window within $TimeoutSeconds seconds."
}

function Test-ImageHasVariation {
    param([System.Drawing.Bitmap]$Bitmap)

    $baseColor = $Bitmap.GetPixel(0, 0)
    $stepX = [Math]::Max(1, [int]($Bitmap.Width / 20))
    $stepY = [Math]::Max(1, [int]($Bitmap.Height / 20))

    for ($x = 0; $x -lt $Bitmap.Width; $x += $stepX) {
        for ($y = 0; $y -lt $Bitmap.Height; $y += $stepY) {
            $color = $Bitmap.GetPixel($x, $y)
            $delta = [Math]::Abs($color.R - $baseColor.R) + [Math]::Abs($color.G - $baseColor.G) + [Math]::Abs($color.B - $baseColor.B)
            if ($delta -gt 24) {
                return $true
            }
        }
    }

    return $false
}

function Get-WindowBounds {
    param([IntPtr]$Handle)

    $rect = New-Object WinSafeCleanSmokeWindow+RECT
    if (-not [WinSafeCleanSmokeWindow]::GetWindowRect($Handle, [ref]$rect)) {
        throw "Could not read WPF UI window bounds."
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) {
        throw "WPF UI window bounds are invalid: ${width}x${height}."
    }

    return [pscustomobject]@{
        left = $rect.Left
        top = $rect.Top
        width = $width
        height = $height
    }
}

function Capture-WindowScreenshot {
    param(
        [IntPtr]$Handle,
        [string]$Label,
        [string]$DestinationRoot
    )

    $bounds = Get-WindowBounds -Handle $Handle
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $screenshotPath = Join-Path $DestinationRoot "wpf-ui-$timestamp-$Label.png"
    $captureMethod = "PrintWindow"

    $bitmap = New-Object System.Drawing.Bitmap $bounds.width, $bounds.height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $hdc = $graphics.GetHdc()
        try {
            $printed = [WinSafeCleanSmokeWindow]::PrintWindow($Handle, $hdc, 2)
        }
        finally {
            $graphics.ReleaseHdc($hdc)
        }

        if (-not $printed -or -not (Test-ImageHasVariation -Bitmap $bitmap)) {
            $captureMethod = "CopyFromScreen"
            $graphics.Clear([System.Drawing.Color]::White)
            $sourcePoint = New-Object System.Drawing.Point $bounds.left, $bounds.top
            $targetPoint = [System.Drawing.Point]::Empty
            $size = New-Object System.Drawing.Size $bounds.width, $bounds.height
            $graphics.CopyFromScreen($sourcePoint, $targetPoint, $size)
        }

        if (-not (Test-ImageHasVariation -Bitmap $bitmap)) {
            throw "WPF UI screenshot appears blank for $Label."
        }

        $bitmap.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    return [pscustomobject]@{
        label = $Label
        path = [System.IO.Path]::GetFullPath($screenshotPath)
        width = $bounds.width
        height = $bounds.height
        captureMethod = $captureMethod
    }
}

function Find-AutomationElementByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)

    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Select-TabItem {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $element = Find-AutomationElementByName -Root $Root -Name $Name
    if ($null -eq $element) {
        throw "Could not find tab item '$Name' for WPF UI smoke."
    }

    $pattern = $null
    if (-not $element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) {
        throw "Tab item '$Name' does not support selection."
    }

    $pattern.Select()
    Start-Sleep -Milliseconds 500
}

function Invoke-AutomationElementByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $element = Find-AutomationElementByName -Root $Root -Name $Name
    if ($null -eq $element) {
        throw "Could not find element '$Name' for WPF UI smoke."
    }

    $invoked = $false
    $pattern = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
        try {
            $pattern.Invoke()
            $invoked = $true
        }
        catch {
            $invoked = $false
        }
    }

    if (-not $invoked) {
        $bounds = $element.Current.BoundingRectangle
        if ($bounds.Width -le 0 -or $bounds.Height -le 0) {
            throw "Element '$Name' could not be invoked and has invalid bounds."
        }

        $x = [int]($bounds.Left + ($bounds.Width / 2))
        $y = [int]($bounds.Top + ($bounds.Height / 2))
        [WinSafeCleanSmokeWindow]::SetCursorPos($x, $y) | Out-Null
        [WinSafeCleanSmokeWindow]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        [WinSafeCleanSmokeWindow]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    }

    Start-Sleep -Milliseconds 800
}

function Scroll-ElementIntoView {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $element = Find-AutomationElementByName -Root $Root -Name $Name
    if ($null -eq $element) {
        throw "Could not find element '$Name' for WPF UI smoke."
    }

    $pattern = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern, [ref]$pattern)) {
        $pattern.ScrollIntoView()
    }
    else {
        try {
            $element.SetFocus()
        }
        catch {
            [System.Windows.Forms.SendKeys]::SendWait("{END}")
        }
    }

    Start-Sleep -Milliseconds 500
}

function Scroll-ReadOnlyOpsLeftPaneToBottom {
    param([IntPtr]$Handle)

    $bounds = Get-WindowBounds -Handle $Handle
    $x = $bounds.left + [Math]::Min(320, [int]($bounds.width * 0.30))
    $y = $bounds.top + [Math]::Min($bounds.height - 90, [int]($bounds.height * 0.86))

    [WinSafeCleanSmokeWindow]::SetForegroundWindow($Handle) | Out-Null
    [WinSafeCleanSmokeWindow]::SetCursorPos($x, $y) | Out-Null
    [WinSafeCleanSmokeWindow]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [WinSafeCleanSmokeWindow]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 100
    for ($index = 0; $index -lt 36; $index++) {
        [WinSafeCleanSmokeWindow]::mouse_event(0x0800, 0, 0, 4294967176, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 50
    }
    Start-Sleep -Milliseconds 700
}

$process = $null
$captures = @()

try {
    $process = Start-Process -FilePath $AppPath -PassThru
    $handle = Wait-MainWindowHandle -Process $process -TimeoutSeconds $WaitSeconds

    $workingArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $windowWidth = [Math]::Min(1180, $workingArea.Width)
    $windowHeight = [Math]::Min(740, $workingArea.Height)

    [WinSafeCleanSmokeWindow]::ShowWindow($handle, 5) | Out-Null
    [WinSafeCleanSmokeWindow]::MoveWindow($handle, $workingArea.Left, $workingArea.Top, $windowWidth, $windowHeight, $true) | Out-Null
    [WinSafeCleanSmokeWindow]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Seconds 1

    $automationRoot = [System.Windows.Automation.AutomationElement]::FromHandle($handle)

    $captures += Capture-WindowScreenshot -Handle $handle -Label "scan-report" -DestinationRoot $OutputRoot

    if ($preparedRecentHistory) {
        Invoke-AutomationElementByName -Root $automationRoot -Name "Open Recent"
        $captures += Capture-WindowScreenshot -Handle $handle -Label "cleanup-plan-loaded" -DestinationRoot $OutputRoot
    }
    else {
        Select-TabItem -Root $automationRoot -Name "Cleanup Plan"
        $captures += Capture-WindowScreenshot -Handle $handle -Label "cleanup-plan" -DestinationRoot $OutputRoot
    }

    Select-TabItem -Root $automationRoot -Name "Guided Review"
    $captures += Capture-WindowScreenshot -Handle $handle -Label "guided-review-top" -DestinationRoot $OutputRoot

    Scroll-ElementIntoView -Root $automationRoot -Name "Guarded CLI Handoff"
    Scroll-ReadOnlyOpsLeftPaneToBottom -Handle $handle
    $captures += Capture-WindowScreenshot -Handle $handle -Label "guided-review-handoff" -DestinationRoot $OutputRoot

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $metadataPath = Join-Path $OutputRoot "wpf-ui-$timestamp.json"
    [pscustomobject]@{
        appPath = [System.IO.Path]::GetFullPath($AppPath)
        processId = $process.Id
        captures = $captures
        capturedAt = (Get-Date).ToString("o")
    } | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host "Published WPF UI screenshot smoke passed."
    foreach ($capture in $captures) {
        Write-Host "Screenshot ($($capture.label)): $($capture.path)"
    }
}
finally {
    if ($null -ne $process -and -not $KeepOpen -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
    }

    if ($preparedRecentHistory) {
        if ($hadRecentHistory) {
            Set-Content -LiteralPath $recentHistoryPath -Value $recentHistoryBackup -Encoding UTF8
        }
        elseif (Test-Path -LiteralPath $recentHistoryPath) {
            Remove-Item -LiteralPath $recentHistoryPath
        }
    }
}
