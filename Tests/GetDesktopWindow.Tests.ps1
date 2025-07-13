describe 'Get-DesktopWindow' {
    it 'handles IncludeHidden parameter' -Skip:(-not $IsWindows) {
        { Get-DesktopWindow -IncludeHidden } | Should -Not -Throw
    }
}
