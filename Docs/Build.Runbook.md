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
- The CLI publish targets package `Sources/DesktopManager.Cli/DesktopManager.Cli.csproj` as `desktopmanager.exe` for both `net8.0-windows` and `net10.0-windows`.
- The CLI includes the MCP server entrypoint exposed by:

```powershell
desktopmanager mcp serve
desktopmanager mcp serve --allow-mutations
desktopmanager mcp serve --allow-mutations --allow-process notepad
```

- Fast MCP contract verification lives in `McpServerTests`.
- UI test gates are intentionally split so operators can enable only the risk level they want:

| Gate | Purpose | Notes |
| ---- | ------- | ----- |
| `RUN_UI_TESTS` | Top-level UI pack enablement | Required before any UI slice can run |
| `RUN_OWNED_WINDOW_UI_TESTS` | Owned-window UI tests | Uses repo-created harness windows only |
| `RUN_DESTRUCTIVE_UI_TESTS` | Owned-window mutation tests | Move/resize/hide/snap/transparency on harness windows |
| `RUN_FOREGROUND_UI_TESTS` | Foreground-focus tests | Intentionally steals focus to prove active-window behavior |
| `RUN_SYSTEM_UI_TESTS` | System-wide desktop mutations | Wallpaper, brightness, resolution, and other monitor/session changes |
| `RUN_EXTERNAL_UI_TESTS` | External application harnesses | Launches real desktop apps when a test requires them |
| `RUN_EXPERIMENTAL_UI_TESTS` | Experimental live harnesses | Extra manual-validation paths, not part of the stable pack |

- The disposable live-app MCP harness lives in `McpServerEndToEndTests` and is gated by:

```powershell
$env:RUN_UI_TESTS = "true"
$env:RUN_OWNED_WINDOW_UI_TESTS = "true"
$env:RUN_DESTRUCTIVE_UI_TESTS = "true"
$env:RUN_EXTERNAL_UI_TESTS = "true"
$env:RUN_EXPERIMENTAL_UI_TESTS = "true"
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter "McpServer_TestApp"
```

- Those live MCP desktop tests intentionally run under `net8.0-windows` in the runbook examples because they all drive the shared `DesktopManager.Cli.exe` host and the same real desktop session; the same flows can also be exercised under `net10.0-windows` when validating the newer runtime target.
- The stable live MCP pack is now repo-owned `DesktopManager.TestApp` coverage rather than external Notepad/Edge coverage.
- Foreground and hosted-session validations should only be enabled in sacrificial or explicitly prepared sessions.

Owned-window mutation slice without system-wide or foreground changes:

```powershell
$env:RUN_UI_TESTS = "true"
$env:RUN_OWNED_WINDOW_UI_TESTS = "true"
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter "WindowPositionTests|WindowStateHelpersTests|WindowVisibilityTests|WindowTransparencyTests|WindowStyleModificationTests|WindowLayoutTests|WindowActivationPositioningTests"
```

Foreground-window slice:

```powershell
$env:RUN_UI_TESTS = "true"
$env:RUN_OWNED_WINDOW_UI_TESTS = "true"
$env:RUN_DESTRUCTIVE_UI_TESTS = "true"
$env:RUN_FOREGROUND_UI_TESTS = "true"
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter "DesktopAutomationAssertionTests|WindowManagerFilterTests|WindowTopMostActivationTests"
```

System-wide desktop mutation slice:

```powershell
$env:RUN_UI_TESTS = "true"
$env:RUN_OWNED_WINDOW_UI_TESTS = "true"
$env:RUN_DESTRUCTIVE_UI_TESTS = "true"
$env:RUN_SYSTEM_UI_TESTS = "true"
dotnet test Sources/DesktopManager.Tests/DesktopManager.Tests.csproj -f net8.0-windows --no-build --filter "BackgroundColorTests|MonitorBrightnessTests|MonitorFallbackTests|MonitorResolutionOrientationTests|LogonWallpaperTests"
```

## Hosted-Session Diagnostics

When the repo-owned hosted-session typing harness goes inconclusive, inspect artifacts in this order:

1. Open the newest `Artifacts\HostedSessionTyping\*.summary.txt` companion first.
2. Use the `RetryHistory` line to decide whether the interruption was a repeated single culprit or mixed contention.
3. Only open the matching `.json` snapshot if the summary is not enough.

Common summary categories:

- `browser-electron`
  Usually means Edge, Codex, ChatGPT, or another Chromium/Electron-style window kept stealing focus.
- `mixed`
  More than one foreground category interrupted the run, so the desktop session was generally noisy.
- `none`
  No retained external culprit was captured, so use the raw JSON snapshot and `LastObservedForeground*` fields for follow-up.

Artifact behavior:

- Each hosted-session diagnostic set includes one `.json` snapshot and one companion `*.summary.txt` file.
- The companion summary filename now carries the retry-history category hint when available.
- Older hosted-session diagnostic sets are trimmed automatically and the newest sets are kept.

Quick PowerShell inspection flow:

```powershell
Get-DesktopHostedSessionDiagnostic -RepositoryRoot C:\Support\GitHub\DesktopManager -SummaryOnly
.\Build\Get-HostedSessionDiagnostic.ps1 -SummaryOnly
desktopmanager diagnostic hosted-session --repository-root C:\Support\GitHub\DesktopManager --summary-only
```

JSON fallback for the newest hosted-session artifact:

```powershell
Get-DesktopHostedSessionDiagnostic -RepositoryRoot C:\Support\GitHub\DesktopManager
.\Build\Get-HostedSessionDiagnostic.ps1
.\Build\Get-HostedSessionDiagnostic.ps1 -AsJson
desktopmanager diagnostic hosted-session --repository-root C:\Support\GitHub\DesktopManager --json
```
