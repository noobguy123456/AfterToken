Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinOps {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
}
"@
$p = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if ($p) {
    [WinOps]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
    [WinOps]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 300
    [WinOps]::SetCursorPos(700, 400) | Out-Null
    Start-Sleep -Milliseconds 200
    [WinOps]::SetCursorPos(750, 420) | Out-Null
    "focused " + $p.MainWindowTitle
} else { "no unity window" }
