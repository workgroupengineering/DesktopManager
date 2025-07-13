describe 'Get-DesktopWindowControl' {
    it 'enumerates Notepad controls' -Skip:(-not $IsWindows) {
        $proc = Start-Process notepad -PassThru
        try {
            $window = Wait-DesktopWindow -Name '*Notepad*' -TimeoutMs 10000
            $controls = Get-DesktopWindowControl -Name '*Notepad*'
            $controls | Should -Not -BeNullOrEmpty
            $controls | Where-Object { $_.ClassName -eq 'Edit' } | Should -Not -BeNullOrEmpty
        } finally {
            $proc | Stop-Process -ErrorAction SilentlyContinue
        }
    }
}
