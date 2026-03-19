# DesktopManager Build Runbook

DesktopManager now uses one repo entrypoint for package, module, and CLI outputs:

```powershell
.\Build\Build-Project.ps1
```

## Build Surfaces

- `Build/Build-Project.ps1`
  Orchestrates repository package build, PowerShell module build, and CLI publish.
- `Build/project.build.json`
  Controls `Invoke-ProjectBuild` for the `DesktopManager` NuGet package and GitHub/NuGet release settings.
- `Build/Build-Module.ps1`
  Builds the PowerShell module artefacts using `Invoke-ModuleBuild`.
- `powerforge.dotnetpublish.json`
  Controls `Invoke-DotNetPublish` for the `desktopmanager.exe` publish output.

## Common Commands

Build everything:

```powershell
.\Build\Build-Project.ps1
```

Plan package and CLI work without executing build steps:

```powershell
.\Build\Build-Project.ps1 -Plan
```

Build package only:

```powershell
.\Build\Build-Project.ps1 -BuildModule:$false -PublishTools:$false
```

Build module only:

```powershell
.\Build\Build-Module.ps1 -SkipInstall
```

Build CLI output only:

```powershell
.\Build\Build-Project.ps1 -Build:$false -BuildModule:$false
```

Publish the NuGet package using the repo config:

```powershell
.\Build\Build-Project.ps1 -PublishNuget:$true -BuildModule:$false -PublishTools:$false
```

Publish the GitHub release asset using the repo config:

```powershell
.\Build\Build-Project.ps1 -PublishGitHub:$true -BuildModule:$false -PublishTools:$false
```

Publish a specific CLI runtime:

```powershell
.\Build\Build-Project.ps1 -Build:$false -BuildModule:$false -Runtimes win-x64
```

## Output Locations

- NuGet package/release staging:
  `Artefacts/ProjectBuild`
- PowerShell module artefacts:
  `Artefacts/Unpacked`
  `Artefacts/Packed`
- CLI publish output and manifests:
  `Artefacts/PowerForge/DesktopManager`

## Notes

- `Build-Project.ps1` is the only package and release entrypoint in this repo.
- `Build-Project.ps1 -Plan` skips the module build execution because the module path does not expose the same standalone plan surface here.
- The CLI publish target packages `Sources/DesktopManager.Cli/DesktopManager.Cli.csproj` as `desktopmanager.exe`.
- The CLI includes the MCP server entrypoint exposed by:

```powershell
desktopmanager mcp serve
```
