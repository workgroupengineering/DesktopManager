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
- `get_window_geometry`
- `window_exists`
- `active_window_matches`
- `wait_for_window`
- `list_window_controls`
- `diagnose_window_controls`
- `control_exists`
- `wait_for_control`
- `move_window`
- `click_window_point`
- `drag_window_points`
- `scroll_window_point`
- `type_window_text`
- `focus_window`
- `minimize_windows`
- `snap_window`
- `list_monitors`
- `screenshot_desktop`
- `screenshot_window`
- `launch_process`
- `list_named_targets`
- `get_named_target`
- `save_window_target`
- `resolve_window_target`
- `list_named_control_targets`
- `get_named_control_target`
- `save_control_target`
- `resolve_control_target`
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
- `desktop://targets`
- `desktop://control-targets`
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
   - Use `get_window_geometry` when you need outer-window and client-area bounds before a coordinate-based action.
   - When multiple windows match, prefer an exact `handle` over a broad process selector.
2. Launch when needed.
   - Use `launch_process` for the app under test.
   - Use `waitForWindowMs` when you want launch to spend a short time correlating the real app window before returning.
   - When you know what kind of window should appear, pass `windowTitle` or `windowClassName` and set `requireWindow=true`.
   - Use `wait_for_window` before trying to move, focus, or capture the window.
3. Inspect controls before trying to interact.
   - Use `list_window_controls` to discover target handles, classes, visible text, automation ids, control types, and control bounds.
   - Use `diagnose_window_controls` when a modern app is not exposing the controls you expect. It will tell you whether Win32 or UIA discovery produced results, whether foreground preparation succeeded, and what each probed UIA root returned.
- `diagnose_window_controls` also accepts a saved `targetName`, so reusable control targets and one-off selectors share the same diagnostic path.
- `diagnose_window_controls` also accepts `includeActionProbe`, which adds a read-only UIA action-resolution probe for the first matched UIA control.
- Diagnostic results now include elapsed times for the overall pass, and the optional action probe includes its own elapsed time too.
   - Use `control_exists` or `wait_for_control` when the target control can appear asynchronously or when you want a structured assertion before clicking.
   - When modern controls expose state through UI Automation, filter by current value, enabled state, or keyboard focusability instead of guessing from text alone.
   - If a UIA-heavy query is host-sensitive, opt into foreground assistance before falling back to brittle retries.
   - If structure discovery still fails, use `screenshot_window` plus `get_window_geometry`, then target `click_window_point`, `drag_window_points`, or `scroll_window_point`.
   - Prefer ratio-based targeting with `clientArea=true` when you want the action to scale with different window sizes.
   - When the same coordinate fallback will be reused, save it once with `save_window_target` and resolve or reuse it by name.
   - When the same control selector will be reused, save it once with `save_control_target` and resolve or reuse it by name.
   - Prefer `type_window_text` for whole-window text entry.
   - Prefer `click_control`, `set_control_text`, and `send_control_keys` for control-level interactions.
- Saved control targets are especially useful for modern Chromium-style apps because they preserve the same capability-aware selector profile across runs.
- The same saved control target can now drive read-only discovery too, not just actions, so agents can `list`, `exists`, and `wait` against one reusable selector profile.
   - For classic handle-backed controls, `set_control_text` and `send_control_keys` now route directly to the target control instead of depending on foreground focus.
   - For zero-handle UIA controls in modern apps, foreground-based text or key fallback is now shared too, but should be treated as an explicit opt-in because it can affect the live focused app.
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
desktopmanager window geometry --handle 0xFF1802 --json
desktopmanager window list --process notepad --json
desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
desktopmanager target resolve editor-center --handle 0xFF1802 --json
desktopmanager control-target save edge-address --control-type Edit --background-text --uia
desktopmanager control-target resolve edge-address --process msedge --json
desktopmanager control list --window-title "Codex" --target codex-sidebar-toggle --json
desktopmanager control exists --window-title "Codex" --target codex-sidebar-toggle --json
desktopmanager control wait --window-title "Codex" --target codex-sidebar-toggle --timeout-ms 1000 --interval-ms 100 --json
desktopmanager window click --handle 0xFF1802 --x 200 --y 200
desktopmanager window click --handle 0xFF1802 --x-ratio 0.5 --y-ratio 0.5 --client-area
desktopmanager window click --handle 0xFF1802 --target editor-center
desktopmanager window drag --handle 0xFF1802 --start-x 200 --start-y 200 --end-x 400 --end-y 220 --client-area
desktopmanager window drag --handle 0xFF1802 --start-x-ratio 0.2 --start-y-ratio 0.2 --end-x-ratio 0.6 --end-y-ratio 0.2 --client-area
desktopmanager window drag --handle 0xFF1802 --start-target editor-center --end-target editor-right
desktopmanager window scroll --handle 0xFF1802 --x 200 --y 200 --delta -120 --client-area
desktopmanager window scroll --handle 0xFF1802 --x-ratio 0.5 --y-ratio 0.5 --delta -120 --client-area
desktopmanager window scroll --handle 0xFF1802 --target editor-center --delta -120
desktopmanager window type --handle 0x30A263C --text "Hello world"
desktopmanager window type --process notepad --text "Hello world"
desktopmanager control list --window-process notepad
desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --json
desktopmanager control diagnose --window-title "Codex" --target codex-sidebar-toggle --sample-limit 5 --json
desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar"
desktopmanager control wait --window-active --uia --control-type Button --text-pattern "Show sidebar" --timeout-ms 5000
desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable
desktopmanager control wait --window-handle 0x5BB15E4 --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground --timeout-ms 5000
desktopmanager control list --window-active --uia --control-type Button
desktopmanager control click --window-title "Codex" --target codex-sidebar-toggle
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
- Control assertions and waits are available on the same shared selector model as list/click/set-text.
- Control diagnostics are available on the same shared selector model and help explain Win32 versus UIA discovery gaps.
- Control diagnostics now include per-root UIA probe details, which makes Chromium-style discovery failures much easier to explain and debug.
- Control diagnostics now also show whether a preferred UIA root was learned and reused inside the current long-lived process, which is most relevant for MCP sessions and in-process waits.
- Control diagnostics now also show whether cached UIA root controls were reused, which helps explain why repeated MCP reads can get cheaper after the first expensive Chromium-style discovery pass.
- Control listings now include shared bounds metadata, which makes structural discovery easier to line up with screenshots and window geometry.
- Window-relative clicking, dragging, and scrolling are available as shared fallbacks for apps that do not expose stable controls.
- Coordinate fallbacks can target the client area, which is usually more reliable for browser/editor content than raw outer-window coordinates.
- Ratio-based coordinate fallbacks are available, which makes screenshot-assisted targeting more portable across different window sizes.
- Named window targets are available on the same shared geometry model, which makes repeated coordinate fallbacks much less brittle.
- Window screenshots now return geometry metadata in JSON so agents can align the image with client-area coordinates.
- Window typing now avoids raw `SendInput` when Windows refuses to foreground the target window, which reduces the chance of text leaking into whatever app currently owns focus.
- Process launch can now wait for a launched window and prefers windows from the launched process or newer post-launch handles instead of blindly reusing an older matching window.
- Process launch can also validate the launched window by title/class and require a real match before returning.
- The shared control selector model now supports value, enabled, and keyboard-focusable checks.
- The shared control selector model now also exposes background-safe capability metadata for click, text, keys, and explicit foreground fallback.
- The shared control selector model also supports an opt-in foreground hint for UIA discovery when a target window needs focus.
- Handle-backed control text and key actions now use shared direct message routing, which is safer for background automation than stealing focus first.
- UIA control actions now reuse the same shared fallback-root search strategy as UIA discovery, which reduces action mismatches when controls live under Chromium-style child roots.
- Zero-handle UIA controls now also support shared foreground-based text and key fallback paths, but they should only be enabled intentionally for sacrificial or tightly controlled windows.
- Saved control targets still pay the underlying UIA discovery cost on modern apps, so target-based `wait` is reusable and safer, but not necessarily cheap.
- Preferred-root reuse is process-local. It helps a long-running MCP server more than separate one-shot CLI invocations.
- The short-lived shared UIA control cache is also process-local, so long-running MCP sessions benefit far more than one-shot CLI calls.
- Repeated UIA actions in the same long-lived process now also try a cached exact-match lookup before a broader root walk, which should reduce repeated click/set-text/send-keys overhead for stable modern-app targets.
- Shared control waits now also prefer already-seen matching window handles inside the same process before they fall back to broad rediscovery, which matters most for long-lived MCP sessions on stable app windows.
- Prefer minimizing distractions instead of closing applications.
- When targeting multiple windows, verify selectors carefully before using `all`.
- Monitor metadata is intended to align with the desktop-coordinate bounds used by monitor screenshots.
