BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Get-DesktopControlCheck' {
    it 'returns boolean state' -Skip:(-not $IsWindows) {
        $info = [DesktopManager.WindowControlInfo]::new()
        { Get-DesktopControlCheck -Control $info } | Should -Not -Throw
    }
}
