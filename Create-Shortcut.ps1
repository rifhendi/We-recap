$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$Shortcut = $WshShell.CreateShortcut("$DesktopPath\We-Recap Site Rembang.lnk")
$Shortcut.TargetPath = "d:\We-recap\Buka-Aplikasi-Laptop.bat"
$Shortcut.WorkingDirectory = "d:\We-recap"
$Shortcut.Description = "Aplikasi Rekap Absensi Engineer On Site - Manage Service SIG Rembang"
$Shortcut.IconLocation = "C:\Program Files\Google\Chrome\Application\chrome.exe,0"
$Shortcut.Save()
Write-Output "SUCCESS: Shortcut created at $DesktopPath\We-Recap Site Rembang.lnk"

