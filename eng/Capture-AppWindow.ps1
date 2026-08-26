param(
    [Parameter(Mandatory = $true)]
    [int] $ProcessId,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

Add-Type -AssemblyName System.Drawing.Common
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class GatekeeperDemoWindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
}
"@

$process = Get-Process -Id $ProcessId
$null = $process.WaitForInputIdle(5000)
$handle = $process.MainWindowHandle
if ($handle -eq [IntPtr]::Zero) {
    throw "Process $ProcessId does not have a visible main window."
}

$null = [GatekeeperDemoWindowCapture]::ShowWindow($handle, 9)
$null = [GatekeeperDemoWindowCapture]::MoveWindow($handle, 0, 0, 1600, 960, $true)
$null = [GatekeeperDemoWindowCapture]::SetWindowPos($handle, [IntPtr](-1), 0, 0, 1600, 960, 0x0040)
$null = [GatekeeperDemoWindowCapture]::SetForegroundWindow($handle)
Start-Sleep -Milliseconds 800

$rect = New-Object GatekeeperDemoWindowCapture+Rect
$null = [GatekeeperDemoWindowCapture]::GetWindowRect($handle, [ref] $rect)
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
    $bitmap.Save($resolvedOutput, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output $resolvedOutput
}
finally {
    $null = [GatekeeperDemoWindowCapture]::SetWindowPos($handle, [IntPtr](-2), 0, 0, 0, 0, 0x0003)
    $graphics.Dispose()
    $bitmap.Dispose()
}
