Import-Module ./DesktopManager.psd1 -Force

# Toggle the check state of a control
$control = Get-DesktopWindowControl -Name '*Notepad*' | Where-Object Text -eq 'Wrap around'
Set-DesktopControlCheck -Control $control -Check $true
