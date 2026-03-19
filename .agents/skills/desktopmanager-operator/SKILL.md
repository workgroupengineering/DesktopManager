---
name: desktopmanager-operator
description: Operate and validate Windows desktop state through DesktopManager using MCP first and CLI second. Use when Codex needs to inspect windows and monitors, capture screenshots, launch desktop applications, wait for windows to appear, inspect child controls, click controls, send text or keys to windows and controls, focus or move windows, apply named layouts, save or restore snapshots, clean up distractions, or prepare the desktop for coding or screen sharing.
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
   - Use `window_exists` or `active_window_matches` when you need an explicit assertion first.
   - Use `screenshot_desktop` or `screenshot_window` when visual confirmation is needed.
   - When more than one window matches, switch to an exact handle before mutating anything.
3. Launch and wait when the target app is not ready yet.
   - Use `launch_process` to start the app.
   - Use `wait_for_window` before moving, focusing, or capturing it.
4. Inspect controls before interacting.
   - Use `list_window_controls` to discover control handles, classes, text, automation ids, and control types.
   - Use `diagnose_window_controls` when a modern app is not exposing the controls you expected. It will show whether Win32 or UIA discovery produced anything and whether foreground preparation helped.
   - Use UIA-oriented selectors when modern apps do not expose useful child-window controls.
   - Use `control_exists` or `wait_for_control` when the control can appear asynchronously or when you want an explicit precondition before clicking.
   - When available, prefer value, enabled, or focusable checks over brittle text-only guesses.
   - If UIA discovery is flaky on a background window, retry with the shared foreground hint before inventing wrapper-specific workarounds.
   - If the app still stays structurally opaque, switch to the shared coordinate-based fallback: capture the window, pick a relative point, and use `click_window_point`.
   - Use `type_window_text` for whole-window entry.
   - Use `click_control`, `set_control_text`, or `send_control_keys` for control-level work.
5. Prefer named state over one-off moves.
   - Use `list_named_layouts` before manually moving windows.
   - Use `apply_named_layout` or `restore_saved_snapshot` when the desired setup already exists.
6. Make the smallest safe change.
   - Prefer `focus_window`, `snap_window`, or `minimize_windows`.
   - Save the current state before larger changes:
     `save_current_layout`
     `save_current_snapshot`
7. Explain what changed after mutating actions.
8. Use CLI only as fallback or verification.

## MCP Surface

Tools:

- `get_active_window`
- `list_windows`
- `window_exists`
- `active_window_matches`
- `wait_for_window`
- `list_window_controls`
- `diagnose_window_controls`
- `control_exists`
- `wait_for_control`
- `move_window`
- `click_window_point`
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
desktopmanager window exists --title "Codex"
desktopmanager window active-matches --title "Codex"
desktopmanager window wait --process notepad --timeout-ms 5000
desktopmanager window click --handle 0xFF1802 --x 200 --y 200
desktopmanager window type --process notepad --text "Hello world"
desktopmanager control list --window-process notepad
desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --json
desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar"
desktopmanager control wait --window-active --uia --control-type Button --text-pattern "Show sidebar" --timeout-ms 5000
desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable
desktopmanager control wait --window-handle 0x5BB15E4 --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground --timeout-ms 5000
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
```

## Decision Rules

- Prefer reading resources before calling mutating tools.
- Prefer screenshot tools when the task needs visual validation rather than only structural window data.
- Prefer `launch_process` plus `wait_for_window` over blind retries.
- Prefer `list_window_controls` before guessing a control handle.
- Prefer `diagnose_window_controls` when Chromium-style apps or background windows are not returning expected controls.
- Prefer `click_window_point` over inventing wrapper-specific click hacks when screenshots give you a reliable target and the app exposes no usable controls.
- Prefer window-level typing when control-level targeting is uncertain.
- Prefer named layouts and snapshots over repeated manual window placement.
- Prefer minimizing distracting windows over closing them.
- Use specific selectors when possible:
  title, process, class, pid, or handle.
- Prefer `handle` over `process` when multiple windows from the same app are open.
- Remember that `activeWindow` means the current foreground window and may resolve to Codex or the terminal if they have focus.
- Be careful with `all`; verify the target set first.
- Remember that snapshots are windows-only for now.
- Remember that UIA selectors and actions now run through the shared library, but verifying selectors in the current host is still smart before relying on them unattended.

## Reference Files

- `Docs/DesktopManager.Cli.md`
- `Docs/DesktopManager.Mcp.md`
- `Sources/DesktopManager.Cli/McpCatalog.cs`
