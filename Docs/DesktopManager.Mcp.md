# DesktopManager MCP and Operator Notes

DesktopManager exposes two automation surfaces:

- CLI:
  `desktopmanager ...`
- MCP over stdio:
  `desktopmanager mcp serve`

For agent-driven desktop automation, prefer MCP first and use CLI as fallback.

## Current MCP Tools

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

## Current MCP Resources

- `desktop://monitors`
- `desktop://windows/active`
- `desktop://windows/visible`
- `desktop://layouts`
- `desktop://snapshot/current`

## Current MCP Prompts

- `prepare_for_coding`
- `prepare_for_screen_sharing`
- `clean_up_distractions`

## Recommended Agent Workflow

1. Inspect first.
   - Read `desktop://windows/visible`, `desktop://windows/active`, and `desktop://monitors`.
   - Use `get_active_window` when focus matters.
   - Use `screenshot_desktop` or `screenshot_window` when visual confirmation is needed.
2. Launch when needed.
   - Use `launch_process` for the app under test.
   - Use `wait_for_window` before trying to move, focus, or capture the window.
3. Prefer named state when available.
   - Use `list_named_layouts` before moving windows one by one.
   - Use `apply_named_layout` or `restore_saved_snapshot` when a saved state exists.
4. Make reversible changes.
   - Prefer `focus_window`, `snap_window`, and `minimize_windows` over destructive actions.
   - Save the current layout or snapshot before larger rearrangements.
5. Explain intent.
   - Say what will change before applying a layout or minimizing multiple windows.
6. Avoid assumptions.
   - Match windows by title, process, class, pid, or handle.
   - Start with specific selectors when possible.

## CLI Fallback Patterns

Use the CLI when MCP is unavailable or when validating the same operation outside the MCP transport.

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
desktopmanager mcp serve
```

## Safety Notes

- DesktopManager currently focuses on non-destructive window and layout operations.
- Screenshots are written to PNG files and returned as file paths.
- Snapshots are windows-only for now.
- Prefer minimizing distractions instead of closing applications.
- When targeting multiple windows, verify selectors carefully before using `all`.
