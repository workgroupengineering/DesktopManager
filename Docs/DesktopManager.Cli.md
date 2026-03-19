# DesktopManager CLI MVP

The repository now includes a `DesktopManager.Cli` project that exposes a small, noun-based command tree over the existing `DesktopManager` C# library.

## Goals

- Keep the CLI aligned with the current C# surface area.
- Provide a stable foundation for MCP hosting.
- Reuse existing window, monitor, and layout APIs rather than duplicating desktop logic.

## Command groups

```text
desktopmanager window list
desktopmanager window move
desktopmanager window focus
desktopmanager window minimize
desktopmanager window snap

desktopmanager monitor list

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
- snapshots currently reuse the window layout format and are therefore windows-only for now.
- `mcp serve` hosts a stdio MCP server.

## Why this shape

- `window`, `monitor`, `layout`, and `snapshot` scale better than flat verbs.
- the CLI mirrors existing concepts already present in the library and PowerShell module.
- the CLI and MCP server reuse the same desktop operations and storage conventions.
