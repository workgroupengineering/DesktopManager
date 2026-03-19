using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class DesktopOperations {
    public static IReadOnlyList<WindowResult> ListWindows(WindowSelectionCriteria criteria) {
        return SelectWindows(criteria).Select(MapWindow).ToArray();
    }

    public static WindowResult GetActiveWindow() {
        IntPtr handle = MonitorNativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero) {
            throw new CommandLineException("No active window was detected.");
        }

        var criteria = new WindowSelectionCriteria {
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            Handle = $"0x{handle.ToInt64():X}"
        };

        var window = SelectWindows(criteria).FirstOrDefault();
        if (window == null) {
            throw new CommandLineException("The active window could not be resolved.");
        }

        return MapWindow(window);
    }

    public static WindowChangeResult MoveWindow(WindowSelectionCriteria criteria, int? monitorIndex, int? x, int? y, int? width, int? height, bool activate) {
        var manager = new WindowManager();
        IReadOnlyList<WindowInfo> windows = SelectTargetWindows(criteria);

        Monitor? monitor = null;
        if (monitorIndex.HasValue) {
            monitor = new Monitors().GetMonitors(index: monitorIndex.Value).FirstOrDefault();
            if (monitor == null) {
                throw new CommandLineException($"Monitor with index {monitorIndex.Value} was not found.");
            }
        }

        foreach (WindowInfo window in windows) {
            if (monitor != null) {
                manager.MoveWindowToMonitor(window, monitor);
            }

            if (x.HasValue || y.HasValue || width.HasValue || height.HasValue) {
                manager.SetWindowPosition(
                    window,
                    x ?? -1,
                    y ?? -1,
                    width ?? -1,
                    height ?? -1);
            }

            if (activate) {
                manager.ActivateWindow(window);
            }
        }

        return BuildWindowChangeResult(manager, "move", windows);
    }

    public static WindowChangeResult FocusWindow(WindowSelectionCriteria criteria) {
        var manager = new WindowManager();
        IReadOnlyList<WindowInfo> windows = SelectTargetWindows(criteria);
        foreach (WindowInfo window in windows) {
            manager.ActivateWindow(window);
        }

        return BuildWindowChangeResult(manager, "focus", windows);
    }

    public static WindowChangeResult MinimizeWindows(WindowSelectionCriteria criteria) {
        var manager = new WindowManager();
        IReadOnlyList<WindowInfo> windows = SelectTargetWindows(criteria);
        foreach (WindowInfo window in windows) {
            manager.MinimizeWindow(window);
        }

        return BuildWindowChangeResult(manager, "minimize", windows);
    }

    public static WindowChangeResult SnapWindow(WindowSelectionCriteria criteria, string position) {
        if (!TryParseSnapPosition(position, out SnapPosition snapPosition)) {
            throw new CommandLineException($"Unsupported snap position '{position}'.");
        }

        var manager = new WindowManager();
        IReadOnlyList<WindowInfo> windows = SelectTargetWindows(criteria);
        foreach (WindowInfo window in windows) {
            manager.SnapWindow(window, snapPosition);
        }

        return BuildWindowChangeResult(manager, "snap", windows);
    }

    public static IReadOnlyList<MonitorResult> ListMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null) {
        return new Monitors().GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index)
            .Select(monitor => new MonitorResult {
                Index = monitor.Index,
                DeviceName = monitor.DeviceName,
                DeviceString = monitor.DeviceString,
                DeviceId = monitor.DeviceId,
                IsConnected = monitor.IsConnected,
                IsPrimary = monitor.IsPrimary,
                Left = monitor.PositionLeft,
                Top = monitor.PositionTop,
                Right = monitor.PositionRight,
                Bottom = monitor.PositionBottom,
                Manufacturer = monitor.Manufacturer,
                SerialNumber = monitor.SerialNumber
            })
            .ToArray();
    }

    public static NamedStateResult SaveLayout(string name) {
        string path = NamedStorage.GetLayoutPath(name);
        new WindowManager().SaveLayout(path);
        return new NamedStateResult {
            Action = "save",
            Name = name,
            Path = path
        };
    }

    public static NamedStateResult ApplyLayout(string name, bool validate) {
        string path = NamedStorage.GetLayoutPath(name);
        if (!File.Exists(path)) {
            throw new CommandLineException($"Named layout '{name}' was not found.");
        }

        new WindowManager().LoadLayout(path, validate);
        return new NamedStateResult {
            Action = "apply",
            Name = name,
            Path = path
        };
    }

    public static IReadOnlyList<string> ListLayouts() {
        return NamedStorage.ListNames("layouts");
    }

    public static NamedStateResult SaveSnapshot(string name) {
        string path = NamedStorage.GetSnapshotPath(name);
        new WindowManager().SaveLayout(path);
        return new NamedStateResult {
            Action = "save",
            Name = name,
            Path = path,
            Scope = "windows-only"
        };
    }

    public static NamedStateResult RestoreSnapshot(string name, bool validate) {
        string path = NamedStorage.GetSnapshotPath(name);
        if (!File.Exists(path)) {
            throw new CommandLineException($"Named snapshot '{name}' was not found.");
        }

        new WindowManager().LoadLayout(path, validate);
        return new NamedStateResult {
            Action = "restore",
            Name = name,
            Path = path,
            Scope = "windows-only"
        };
    }

    public static IReadOnlyList<string> ListSnapshots() {
        return NamedStorage.ListNames("snapshots");
    }

    public static object GetCurrentSnapshotSummary() {
        return new {
            ActiveWindow = SafeGetActiveWindow(),
            Monitors = ListMonitors(connectedOnly: true),
            Windows = ListWindows(new WindowSelectionCriteria())
        };
    }

    private static WindowResult? SafeGetActiveWindow() {
        try {
            return GetActiveWindow();
        } catch {
            return null;
        }
    }

    private static IReadOnlyList<WindowInfo> SelectTargetWindows(WindowSelectionCriteria criteria) {
        var windows = SelectWindows(criteria);
        if (windows.Count == 0) {
            throw new CommandLineException("No matching windows were found.");
        }

        if (criteria.All) {
            return windows;
        }

        return new[] { windows[0] };
    }

    private static List<WindowInfo> SelectWindows(WindowSelectionCriteria criteria) {
        var query = new WindowQueryOptions {
            TitlePattern = criteria.TitlePattern,
            ProcessNamePattern = criteria.ProcessNamePattern,
            ClassNamePattern = criteria.ClassNamePattern,
            IncludeHidden = criteria.IncludeHidden,
            IncludeCloaked = criteria.IncludeCloaked,
            IncludeOwned = criteria.IncludeOwned,
            ProcessId = criteria.ProcessId ?? 0
        };

        var manager = new WindowManager();
        var windows = manager.GetWindows(query);

        if (!string.IsNullOrWhiteSpace(criteria.Handle)) {
            IntPtr handle = ParseHandle(criteria.Handle);
            windows = windows.Where(window => window.Handle == handle).ToList();
        }

        if (!criteria.IncludeEmptyTitles && !HasExplicitSelector(criteria)) {
            windows = windows.Where(window => !string.IsNullOrWhiteSpace(window.Title)).ToList();
        }

        return windows;
    }

    private static WindowChangeResult BuildWindowChangeResult(WindowManager manager, string action, IReadOnlyList<WindowInfo> windows) {
        return new WindowChangeResult {
            Action = action,
            Count = windows.Count,
            Windows = windows.Select(window => RefreshWindow(manager, window)).ToArray()
        };
    }

    private static WindowResult RefreshWindow(WindowManager manager, WindowInfo window) {
        List<WindowInfo> current = manager.GetWindows(
            processId: (int)window.ProcessId,
            includeHidden: true,
            includeCloaked: true,
            includeOwned: true);
        WindowInfo? refreshed = current.FirstOrDefault(candidate => candidate.Handle == window.Handle);
        return refreshed != null ? MapWindow(refreshed) : MapWindow(window);
    }

    private static WindowResult MapWindow(WindowInfo window) {
        return new WindowResult {
            Title = window.Title,
            Handle = $"0x{window.Handle.ToInt64():X}",
            ProcessId = window.ProcessId,
            ThreadId = window.ThreadId,
            IsVisible = window.IsVisible,
            IsTopMost = window.IsTopMost,
            State = window.State?.ToString(),
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            MonitorIndex = window.MonitorIndex,
            MonitorDeviceName = window.MonitorDeviceName
        };
    }

    private static IntPtr ParseHandle(string value) {
        bool isHex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            value.IndexOfAny(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f' }) >= 0;
        if (isHex) {
            string normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value.Substring(2) : value;
            if (long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue)) {
                return new IntPtr(hexValue);
            }
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long decimalValue)) {
            return new IntPtr(decimalValue);
        }

        throw new CommandLineException("The provided handle is not a valid decimal or hexadecimal window handle.");
    }

    private static bool HasExplicitSelector(WindowSelectionCriteria criteria) {
        return !string.IsNullOrWhiteSpace(criteria.TitlePattern) && criteria.TitlePattern != "*" ||
            !string.IsNullOrWhiteSpace(criteria.ProcessNamePattern) && criteria.ProcessNamePattern != "*" ||
            !string.IsNullOrWhiteSpace(criteria.ClassNamePattern) && criteria.ClassNamePattern != "*" ||
            criteria.ProcessId.HasValue ||
            !string.IsNullOrWhiteSpace(criteria.Handle);
    }

    private static bool TryParseSnapPosition(string value, out SnapPosition position) {
        switch (value.ToLowerInvariant()) {
            case "left":
                position = SnapPosition.Left;
                return true;
            case "right":
                position = SnapPosition.Right;
                return true;
            case "top-left":
                position = SnapPosition.TopLeft;
                return true;
            case "top-right":
                position = SnapPosition.TopRight;
                return true;
            case "bottom-left":
                position = SnapPosition.BottomLeft;
                return true;
            case "bottom-right":
                position = SnapPosition.BottomRight;
                return true;
            default:
                position = default;
                return false;
        }
    }
}
