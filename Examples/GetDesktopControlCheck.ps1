Import-Module ./DesktopManager.psd1 -Force

# Retrieve the check state of a control
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object Text -eq 'Wrap around'
Get-DesktopControlCheck -Control $control
