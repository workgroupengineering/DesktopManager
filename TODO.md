# DesktopManager Roadmap (Windows 10/11 + Server 2016+)

## Easy (Low Risk)
- Window query improvements: owner/child windows, top-level only, UWP/AppContainer, cloaked windows.
- Combined filters: title (wildcards/regex), class name, process name, visibility, minimized/maximized, z-order index.
- Window state helpers: MinimizeAll, RestoreAll, BringToFrontIfVisible, EnsureOnScreen.
- Process/window utilities: GetOwnerProcess, GetWindowThread, GetWindowModulePath, IsElevatedOwner.
- Clipboard + input reliability: retry/backoff, safe mode (no mouse move/foreground), optional delays.
- Wallpaper helpers: per-monitor cache, background color retrieval, COM + registry fallback.

## Medium (More Variability)
- Layout engine: relative layout (percent-based), profile per monitor count/orientation, DPI-aware restore.
- Virtual desktops: list/create/remove/rename, move window to desktop, query window desktop (Win10+).
- Display configuration: enumerate modes, set per-monitor resolution/refresh/rotation, rollback on failure.
- Taskbar & shell: auto-hide/position/size, per-monitor taskbar detection, safe explorer restart.
- Power/session events: monitor power, session lock/unlock, robust event dispatch and disposal.

## Hard (High Variability)
- UI Automation (UIA): element tree, invoke/set text/toggle/select; Win32 fallback.
- High-performance capture: Desktop Duplication (D3D11), region/window capture, recording.
- Input recording/playback: timed sequences bound to window handles; dry-run and focus constraints.
- RDP/service session support: session isolation, explicit “not supported” paths.
- Policy/admin operations: system wallpaper/lock screen/default display settings with opt-in and rollback.

## Support Checklist (All Features)
- Windows 10/11 + Server 2016+ compatibility gating and runtime checks.
- C# API returns clear results/errors; PowerShell wrappers thin with -WhatIf/-Confirm.
- Fallbacks: COM -> Win32 -> registry/system parameters.
- Diagnostics: structured logs, actionable error messages.
- Tests: non-interactive defaults; UI/desktop-changing tests opt-in by env vars.
- Docs: C# and PowerShell usage examples per feature.
