Import-Module ./DesktopManager.psd1 -Force

$proc = Start-Process notepad -PassThru
Start-Sleep -Seconds 1
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object ClassName -eq 'Edit' | Select-Object -First 1
Send-DesktopControlKey -Control $control -Keys @([DesktopManager.VirtualKey]::VK_H, [DesktopManager.VirtualKey]::VK_I)
$proc | Stop-Process -ErrorAction SilentlyContinue
