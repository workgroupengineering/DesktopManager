# DesktopManager CLI MVP

The repository now includes a `DesktopManager.Cli` project that exposes a small, noun-based command tree over the existing `DesktopManager` C# library.

## Goals

- Keep the CLI aligned with the current C# surface area.
- Provide a stable foundation for MCP hosting.
- Reuse existing window, monitor, and layout APIs rather than duplicating desktop logic.

## Command groups

```text
desktopmanager window list
desktopmanager window exists
desktopmanager window active-matches
desktopmanager window wait
desktopmanager window type
desktopmanager window move
desktopmanager window focus
desktopmanager window minimize
desktopmanager window snap

desktopmanager control list
desktopmanager control exists
desktopmanager control wait
desktopmanager control click
desktopmanager control set-text
desktopmanager control send-keys

desktopmanager monitor list

desktopmanager process start

desktopmanager screenshot desktop
desktopmanager screenshot window

desktopmanager layout save
desktopmanager layout apply
desktopmanager layout list

desktopmanager snapshot save
desktopmanager snapshot restore
desktopmanager snapshot list

desktopmanager mcp serve
```

## Current behavior

- `layout` stores named JSON files under `%AppData%\DesktopManager\layouts`.
- `snapshot` stores named JSON files under `%AppData%\DesktopManager\snapshots`.
- `screenshot` stores generated PNG files under `%AppData%\DesktopManager\captures` when `--output` is not provided.
- snapshots currently reuse the window layout format and are therefore windows-only for now.
- `process start` launches a desktop application and can optionally wait for input idle.
- `window wait` polls for a matching window and returns when one appears.
- `window exists` and `window active-matches` provide non-mutating verification commands.
- `control exists` and `control wait` provide the same inspect-first verification model for controls.
- `control` works with child window controls and can also use UI Automation-oriented selectors.
- control selectors can now match `value`, `enabled`, and `focusable` state through the shared library.
- `window type` sends text to the target window, either by simulated typing or clipboard paste.
- `window` commands support exact handle targeting and active-window targeting for safer selection when multiple windows match.
- `screenshot window` now prefers real window rendering before falling back to screen pixels, which improves captures for covered windows.
- `mcp serve` hosts a stdio MCP server.

## Why this shape

- `window`, `monitor`, `layout`, and `snapshot` scale better than flat verbs.
- `process` and `screenshot` add the first inspect-launch-wait loop needed for desktop automation.
- `control` and `window type` add the first direct interaction layer for classic desktop controls.
- the CLI mirrors existing concepts already present in the library and PowerShell module.
- the CLI and MCP server reuse the same desktop operations and storage conventions.
- window selection and text-entry reliability now live in the shared C# library so CLI, MCP, and PowerShell stay aligned.

## Current Limits

- Child-window targeting is still the simplest path for classic Win32 controls.
- UIA discovery and action fallback now work through the shared library, but selector validation is still wise before unattended runs.
