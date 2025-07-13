Import-Module .\DesktopManager.psd1 -Force

Get-DesktopWindow | Format-Table *

# Filter windows by process name
Get-DesktopWindow -ProcessName 'notepad'

# Filter windows by process ID
$notepad = Get-Process -Name notepad -ErrorAction SilentlyContinue | Select-Object -First 1
if ($notepad) {
    Get-DesktopWindow -ProcessId $notepad.Id
}

# Filter by class name
Get-DesktopWindow -ClassName 'Notepad'

# Filter using regex
Get-DesktopWindow -Regex '.*Notepad.*'

Set-DesktopWindow -Name '*Notepad' -Height 800 -Width 1200 -Left 100 -Activate

Set-DesktopWindow -Name '*Notepad' -TopMost

# Get all hidden windows
Get-DesktopWindow -IncludeHidden | Where-Object { -not $_.IsVisible }
