BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Get-DesktopControlCheck' {
    it 'validates control handle parameter' -Skip:(-not $IsWindows) {
        $info = [DesktopManager.WindowControlInfo]::new()
        { Get-DesktopControlCheck -Control $info } | Should -Throw
    }
}
