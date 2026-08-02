$paths = @()
Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue | ForEach-Object {
    $dn = $_.GetValue("DisplayName")
    if ($dn -like "*Unity*") { $paths += "$dn => $($_.GetValue('InstallLocation')) $($_.GetValue('DisplayIcon'))" }
}
$paths
$hub = Get-ChildItem "HKCU:\SOFTWARE\Unity Technologies\Unity Hub" -ErrorAction SilentlyContinue
if ($hub) { $hub.Property }
Get-ChildItem "D:\","E:\" -Filter "Unity.exe" -Recurse -Depth 3 -ErrorAction SilentlyContinue | Select-Object -First 3 FullName
