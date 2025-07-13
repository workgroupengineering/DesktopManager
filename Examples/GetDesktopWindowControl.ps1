Import-Module ./DesktopManager.psd1 -Force

# List controls for all Notepad windows
Get-DesktopWindowControl -Name '*Notepad*' | Format-Table Id, ClassName, Text
