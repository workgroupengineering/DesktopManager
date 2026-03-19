---
name: desktopmanager-operator
description: Operate and validate Windows desktop state through DesktopManager using MCP first and CLI second. Use when Codex needs to inspect windows and monitors, capture screenshots, launch desktop applications, wait for windows to appear, focus or move windows, apply named layouts, save or restore snapshots, clean up distractions, or prepare the desktop for coding or screen sharing.
---

# DesktopManager Operator

Use this skill to operate the Windows desktop through DesktopManager.

## Golden Path

1. Prefer MCP first:
   `desktopmanager mcp serve`
2. Start with inspection, not mutation.
   - Read resources first:
     `desktop://windows/active`
     `desktop://windows/visible`
     `desktop://monitors`
   - Use `get_active_window` when focus matters.
   - Use `screenshot_desktop` or `screenshot_window` when visual confirmation is needed.
3. Launch and wait when the target app is not ready yet.
   - Use `launch_process` to start the app.
   - Use `wait_for_window` before moving, focusing, or capturing it.
4. Prefer named state over one-off moves.
   - Use `list_named_layouts` before manually moving windows.
   - Use `apply_named_layout` or `restore_saved_snapshot` when the desired setup already exists.
5. Make the smallest safe change.
   - Prefer `focus_window`, `snap_window`, or `minimize_windows`.
   - Save the current state before larger changes:
     `save_current_layout`
     `save_current_snapshot`
6. Explain what changed after mutating actions.
7. Use CLI only as fallback or verification.

## MCP Surface

Tools:

- `get_active_window`
- `list_windows`
- `wait_for_window`
- `move_window`
- `focus_window`
- `minimize_windows`
- `snap_window`
- `list_monitors`
- `screenshot_desktop`
- `screenshot_window`
- `launch_process`
- `list_named_layouts`
- `save_current_layout`
- `apply_named_layout`
- `list_named_snapshots`
- `save_current_snapshot`
- `restore_saved_snapshot`

Resources:

- `desktop://monitors`
- `desktop://windows/active`
- `desktop://windows/visible`
- `desktop://layouts`
- `desktop://snapshot/current`

Prompts:

- `prepare_for_coding`
- `prepare_for_screen_sharing`
- `clean_up_distractions`

## CLI Fallbacks

```text
desktopmanager window list
desktopmanager window wait --process notepad --timeout-ms 5000
desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
desktopmanager screenshot desktop
desktopmanager screenshot window --process notepad
desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400
desktopmanager window focus --process code
desktopmanager window snap --title "Visual Studio Code" --position left
desktopmanager monitor list
desktopmanager layout list
desktopmanager layout save coding
desktopmanager layout apply coding
desktopmanager snapshot save before-meeting
desktopmanager snapshot restore before-meeting
```

## Decision Rules

- Prefer reading resources before calling mutating tools.
- Prefer screenshot tools when the task needs visual validation rather than only structural window data.
- Prefer `launch_process` plus `wait_for_window` over blind retries.
- Prefer named layouts and snapshots over repeated manual window placement.
- Prefer minimizing distracting windows over closing them.
- Use specific selectors when possible:
  title, process, class, pid, or handle.
- Be careful with `all`; verify the target set first.
- Remember that snapshots are windows-only for now.

## Reference Files

- `Docs/DesktopManager.Cli.md`
- `Docs/DesktopManager.Mcp.md`
- `Sources/DesktopManager.Cli/McpCatalog.cs`
