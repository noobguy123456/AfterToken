$p = Get-Process Unity -ErrorAction SilentlyContinue
$c1 = $p.CPU
Start-Sleep -Seconds 3
$p2 = Get-Process Unity
Write-Output ("CPU delta in 3s: " + ($p2.CPU - $c1) + "s")
Write-Output ("Responding: " + $p2.Responding)
