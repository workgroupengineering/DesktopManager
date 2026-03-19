param(
    [string] $ProjectBuildConfigPath = "$PSScriptRoot\project.build.json",
    [string] $DotNetPublishConfigPath = "$PSScriptRoot\..\powerforge.dotnetpublish.json",
    [Nullable[bool]] $UpdateVersions,
    [Nullable[bool]] $Build,
    [switch] $PublishNuget,
    [switch] $PublishGitHub,
    [switch] $Plan,
    [string] $PlanPath,
    [bool] $BuildModule = $true,
    [bool] $PublishTools = $true,
    [string[]] $Target,
    [string[]] $Runtimes,
    [string[]] $Frameworks,
    [ValidateSet('Portable', 'PortableCompat', 'PortableSize', 'AotSpeed', 'AotSize')][string[]] $Styles,
    [switch] $SkipRestore,
    [switch] $SkipBuild
)

Import-Module PSPublishModule -Force -ErrorAction Stop

if (-not (Test-Path -LiteralPath $ProjectBuildConfigPath)) {
    throw "Project build config file not found: $ProjectBuildConfigPath"
}
if (-not (Test-Path -LiteralPath $DotNetPublishConfigPath)) {
    throw "DotNet publish config file not found: $DotNetPublishConfigPath"
}

$invokeProjectBuild = @{
    ConfigPath = $ProjectBuildConfigPath
}
if ($null -ne $UpdateVersions) {
    $invokeProjectBuild.UpdateVersions = $UpdateVersions
}
if ($null -ne $Build) {
    $invokeProjectBuild.Build = $Build
}
if ($PublishNuget) {
    $invokeProjectBuild.PublishNuget = $true
}
if ($PublishGitHub) {
    $invokeProjectBuild.PublishGitHub = $true
}
if ($Plan) {
    $invokeProjectBuild.Plan = $true
}
if ($PlanPath) {
    $invokeProjectBuild.PlanPath = $PlanPath
}

Invoke-ProjectBuild @invokeProjectBuild

if ($Plan -and $BuildModule) {
    Write-Warning 'Plan mode skips Build-Module.ps1 execution because the module pipeline does not expose a standalone plan surface here.'
}

if ($BuildModule -and -not $Plan) {
    & "$PSScriptRoot\Build-Module.ps1" -SkipInstall
}

if ($PublishTools) {
    $invokeDotNetPublish = @{
        ConfigPath = $DotNetPublishConfigPath
    }
    if ($Plan) {
        $invokeDotNetPublish.Plan = $true
    }
    if ($Target) {
        $invokeDotNetPublish.Target = $Target
    }
    if ($Runtimes) {
        $invokeDotNetPublish.Runtimes = $Runtimes
    }
    if ($Frameworks) {
        $invokeDotNetPublish.Frameworks = $Frameworks
    }
    if ($Styles) {
        $invokeDotNetPublish.Styles = $Styles
    }
    if ($SkipRestore) {
        $invokeDotNetPublish.SkipRestore = $true
    }
    if ($SkipBuild) {
        $invokeDotNetPublish.SkipBuild = $true
    }

    Invoke-DotNetPublish @invokeDotNetPublish
}
