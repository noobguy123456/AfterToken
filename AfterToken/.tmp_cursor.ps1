param([int]$dx = 0, [int]$dy = 0)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$p = [System.Windows.Forms.Cursor]::Position
if ($dx -ne 0 -or $dy -ne 0) {
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(($p.X + $dx), ($p.Y + $dy))
    $p = [System.Windows.Forms.Cursor]::Position
}
Write-Output "$($p.X),$($p.Y)"
