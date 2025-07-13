BeforeAll {
    Import-Module "$PSScriptRoot/..\DesktopManager.psd1" -Force
}

describe 'Set-DesktopWindowVisibility' {
    it 'supports WhatIf mode' -Skip:(-not $IsWindows) {
        { Set-DesktopWindowVisibility -Name '*' -Show -WhatIf } | Should -Not -Throw
    }
}
