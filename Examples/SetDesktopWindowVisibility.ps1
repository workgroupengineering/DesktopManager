Import-Module ./DesktopManager.psd1 -Force

# Hide the first Notepad window
Set-DesktopWindowVisibility -Name '*Notepad*' -Hide

# Show the window again
Set-DesktopWindowVisibility -Name '*Notepad*' -Show
