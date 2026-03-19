# DesktopManager CLI MVP

The repository now includes a `DesktopManager.Cli` project that exposes a small, noun-based command tree over the existing `DesktopManager` C# library.

## Goals

- Keep the CLI aligned with the current C# surface area.
- Provide a stable foundation for MCP hosting.
- Reuse existing window, monitor, and layout APIs rather than duplicating desktop logic.

## Command groups

```text
desktopmanager window list
desktopmanager window wait
desktopmanager window move
desktopmanager window focus
desktopmanager window minimize
desktopmanager window snap

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
- `mcp serve` hosts a stdio MCP server.

## Why this shape

- `window`, `monitor`, `layout`, and `snapshot` scale better than flat verbs.
- `process` and `screenshot` add the first inspect-launch-wait loop needed for desktop automation.
- the CLI mirrors existing concepts already present in the library and PowerShell module.
- the CLI and MCP server reuse the same desktop operations and storage conventions.
