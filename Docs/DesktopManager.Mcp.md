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
- `window_exists`
- `active_window_matches`
- `wait_for_window`
- `list_window_controls`
- `move_window`
- `type_window_text`
- `focus_window`
- `minimize_windows`
- `snap_window`
- `list_monitors`
- `screenshot_desktop`
- `screenshot_window`
- `launch_process`
- `click_control`
- `set_control_text`
- `send_control_keys`
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
   - Use `window_exists` or `active_window_matches` when you want a structured assertion before acting.
   - Use `screenshot_desktop` or `screenshot_window` when visual confirmation is needed.
   - When multiple windows match, prefer an exact `handle` over a broad process selector.
2. Launch when needed.
   - Use `launch_process` for the app under test.
   - Use `wait_for_window` before trying to move, focus, or capture the window.
3. Inspect controls before trying to interact.
   - Use `list_window_controls` to discover target handles, classes, visible text, automation ids, and control types.
   - Prefer `type_window_text` for whole-window text entry.
   - Prefer `click_control`, `set_control_text`, and `send_control_keys` for control-level interactions.
4. Prefer named state when available.
   - Use `list_named_layouts` before moving windows one by one.
   - Use `apply_named_layout` or `restore_saved_snapshot` when a saved state exists.
5. Make reversible changes.
   - Prefer `focus_window`, `snap_window`, and `minimize_windows` over destructive actions.
   - Save the current layout or snapshot before larger rearrangements.
6. Explain intent.
   - Say what will change before applying a layout or minimizing multiple windows.
7. Avoid assumptions.
   - Match windows by title, process, class, pid, or handle.
   - Start with specific selectors when possible.
   - Remember that `activeWindow` means the current foreground window, which may be the agent host if it has focus.

## CLI Fallback Patterns

Use the CLI when MCP is unavailable or when validating the same operation outside the MCP transport.

```text
desktopmanager window list
desktopmanager window exists --title "Codex"
desktopmanager window active-matches --title "Codex"
desktopmanager window wait --process notepad --timeout-ms 5000
desktopmanager window list --process notepad --json
desktopmanager window type --handle 0x30A263C --text "Hello world"
desktopmanager window type --process notepad --text "Hello world"
desktopmanager control list --window-process notepad
desktopmanager control list --window-active --uia --control-type Button
desktopmanager control click --window-process notepad --class RichEditD2DPT
desktopmanager control set-text --window-process notepad --class RichEditD2DPT --text "Hello world"
desktopmanager control send-keys --window-process notepad --class RichEditD2DPT --keys VK_CONTROL,VK_A
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
- Window screenshots prefer native window rendering and fall back to screen capture when that is unavailable.
- Snapshots are windows-only for now.
- Control discovery supports both child-window selectors and UIA-oriented selectors.
- Prefer minimizing distractions instead of closing applications.
- When targeting multiple windows, verify selectors carefully before using `all`.
