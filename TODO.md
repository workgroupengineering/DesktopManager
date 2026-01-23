# DesktopManager TODO (Win10/11/Server 2016+)

## Easy
- Add public enums for layout options (`TextPosition`, `Target`, wallpaper fit) to replace string switches in PowerBGInfo.
- Add guardrails to avoid UI disruptions in tests (already started) and document env vars for UI test opt-in.
- Add convenience APIs: center window, snap to monitor edge, move to primary, toggle topmost, bring to front with retries.
- Normalize DPI-aware coordinate helpers (logical vs physical) and expose conversions in `WindowManager`.
- Expand documentation and examples for common tasks (move/resize, enumerate, screenshot).

## Medium
- Virtual desktop management (create/switch/move window) via `IVirtualDesktopManager`.
- Monitor profiles (save/restore layouts, resolutions, refresh rate, orientation).
- Taskbar/appbar control: auto-hide, position, size, multi-monitor behavior.
- Per-monitor wallpaper pipeline with caching and rollback.
- Input injection improvements (SendInput + UI Automation fallback for blocked controls).

## Hard
- Full display settings control (resolution, refresh rate, HDR, scaling) via DXGI + DisplayConfig APIs.
- GPU/adapter-aware monitor enumeration, color profiles, and EDID parsing.
- DWM effects: blur, acrylic, thumbnails, live previews, z-order-safe animations.
- Secure desktop detection (UAC, Winlogon) and safe no-op behavior.
- RDP/multi-session awareness and remote-safe window/control operations.

## Ecosystem Alignment
- Keep CodeGlyphX dependency-free for barcode/QR; ensure ImagePlayground uses it before any ImageSharp path.
- Split ImagePlayground backends (ImageSharp vs GDI+) and let PowerBGInfo pick the lightest backend.
- Standardize error handling + diagnostics (loggers, structured errors, optional trace hooks).
