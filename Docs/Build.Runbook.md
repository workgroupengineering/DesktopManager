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
desktopmanager mcp serve --allow-mutations
desktopmanager mcp serve --allow-mutations --allow-process notepad
```

- Fast MCP contract verification lives in `McpServerTests`.
- The disposable live-app MCP harness lives in `McpServerEndToEndTests` and is gated by:

```powershell
$env:RUN_UI_TESTS = "true"
$env:RUN_DESTRUCTIVE_UI_TESTS = "true"
$env:RUN_EXPERIMENTAL_UI_TESTS = "true"
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadRoundTrip
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadTargetAreaRoundTrip
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadWindowMutationRoundTrip
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadWorkflowRoundTrip
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadAllowedProcessPolicy
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadDeniedProcessPolicy
dotnet test Sources/DesktopManager.sln -f net8.0-windows --filter McpServer_NotepadDryRunPolicy
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter "McpServer_NotepadRoundTrip|McpServer_NotepadTargetAreaRoundTrip|McpServer_NotepadWindowMutationRoundTrip|McpServer_NotepadWorkflowRoundTrip|McpServer_NotepadAllowedProcessPolicy|McpServer_NotepadDeniedProcessPolicy|McpServer_NotepadDryRunPolicy"
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter McpServer_EdgeForegroundInputPolicy_BlocksOmniboxEnterWithoutServerOptIn
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter McpServer_EdgeForegroundInputPolicy_AllowsOmniboxEnterWithServerOptIn
```

- Those live MCP desktop tests intentionally run under `net8.0-windows` only because they all drive the shared `DesktopManager.Cli.exe` host and the same real desktop session.
- The safety-policy harness now covers one allowed scoped mutation, one denied scoped mutation, and one dry-run scoped mutation preview against a disposable Notepad window.
- The stable live MCP desktop pack now stays on the Notepad-backed flows, while both Chromium-style foreground-input harnesses live behind `RUN_EXPERIMENTAL_UI_TESTS=true` so they can be exercised manually without destabilizing regular regression runs.
- When the experimental Chromium opt-in harness goes inconclusive, it now keeps a screenshot plus control-diagnostic bundle under `%TEMP%\DesktopManager.Tests\McpE2E\Experimental`, exercises temporary named window/control targets so the fallback path stays aligned with the shared targeting workflow, and writes `decision-trace.txt` plus `comparison.txt` for follow-up analysis.
