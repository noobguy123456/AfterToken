Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class MouseOps {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    public const uint LEFTDOWN = 0x0002;
    public const uint LEFTUP = 0x0004;
}
"@
[MouseOps]::SetCursorPos(585, 350) | Out-Null
Start-Sleep -Milliseconds 150
[MouseOps]::mouse_event([MouseOps]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 60
[MouseOps]::mouse_event([MouseOps]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 150
[MouseOps]::SetCursorPos(600, 360) | Out-Null
Start-Sleep -Milliseconds 150
[MouseOps]::SetCursorPos(610, 370) | Out-Null
"done"
