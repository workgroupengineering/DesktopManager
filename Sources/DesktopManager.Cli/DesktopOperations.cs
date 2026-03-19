using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace DesktopManager.Cli;

internal static class DesktopOperations {
    public static IReadOnlyList<WindowResult> ListWindows(WindowSelectionCriteria criteria) {
        return SelectWindows(criteria).Select(MapWindow).ToArray();
    }

    public static WindowResult GetActiveWindow() {
        var manager = new WindowManager();
        WindowInfo? window = manager.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
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

    public static WindowChangeResult TypeWindowText(WindowSelectionCriteria criteria, string text, bool paste, int delayMilliseconds) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        var manager = new WindowManager();
        IReadOnlyList<WindowInfo> windows = SelectTargetWindows(criteria);
        foreach (WindowInfo window in windows) {
            if (paste) {
                manager.PasteText(window, text);
            } else {
                manager.TypeText(window, text, delayMilliseconds);
            }
        }

        return BuildWindowChangeResult(manager, paste ? "paste-text" : "type-text", windows);
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

    public static IReadOnlyList<ControlResult> ListControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        return GetControlTargets(windowCriteria, controlCriteria, allWindows)
            .Select(MapControl)
            .ToArray();
    }

    public static ControlActionResult ClickControl(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string button, bool allWindows) {
        MouseButton mouseButton = ParseMouseButton(button);
        var manager = new WindowManager();
        IReadOnlyList<WindowControlTargetInfo> controls = SelectTargetControls(windowCriteria, controlCriteria, allWindows);
        foreach (WindowControlTargetInfo control in controls) {
            manager.ClickControl(control.Control, mouseButton);
        }

        return BuildControlActionResult("click-control", controls);
    }

    public static ControlActionResult SetControlText(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string text, bool allWindows) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        IReadOnlyList<WindowControlTargetInfo> controls = SelectTargetControls(windowCriteria, controlCriteria, allWindows);
        foreach (WindowControlTargetInfo control in controls) {
            MonitorNativeMethods.SendMessage(control.Control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text);
        }

        return BuildControlActionResult("set-control-text", controls);
    }

    public static ControlActionResult SendControlKeys(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, IReadOnlyList<string> keys, bool allWindows) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        IReadOnlyList<WindowControlTargetInfo> controls = SelectTargetControls(windowCriteria, controlCriteria, allWindows);
        foreach (WindowControlTargetInfo control in controls) {
            KeyboardInputService.SendToControl(control.Control, virtualKeys);
        }

        return BuildControlActionResult("send-control-keys", controls);
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

    public static ScreenshotResult CaptureDesktopScreenshot(int? monitorIndex, string? deviceId, string? deviceName, int? left, int? top, int? width, int? height, string? outputPath) {
        bool hasRegionSelector = left.HasValue || top.HasValue || width.HasValue || height.HasValue;
        if (hasRegionSelector && (!left.HasValue || !top.HasValue || !width.HasValue || !height.HasValue)) {
            throw new CommandLineException("Region capture requires --left, --top, --width, and --height.");
        }

        if (hasRegionSelector) {
            using Bitmap bitmap = ScreenshotService.CaptureRegion(left!.Value, top!.Value, width!.Value, height!.Value);
            return SaveScreenshot(bitmap, "region", "region", outputPath);
        }

        if (monitorIndex.HasValue || !string.IsNullOrWhiteSpace(deviceId) || !string.IsNullOrWhiteSpace(deviceName)) {
            Monitor monitor = new Monitors().GetMonitors(index: monitorIndex, deviceId: deviceId, deviceName: deviceName).FirstOrDefault()
                ?? throw new CommandLineException("No matching monitor was found.");

            using Bitmap bitmap = ScreenshotService.CaptureMonitor(index: monitor.Index, deviceId: monitor.DeviceId, deviceName: monitor.DeviceName);
            return SaveScreenshot(bitmap, "monitor", $"monitor-{monitor.Index}", outputPath, monitorIndex: monitor.Index, monitorDeviceName: monitor.DeviceName);
        }

        using Bitmap desktopBitmap = ScreenshotService.CaptureScreen();
        return SaveScreenshot(desktopBitmap, "desktop", "desktop", outputPath);
    }

    public static ScreenshotResult CaptureWindowScreenshot(WindowSelectionCriteria criteria, string? outputPath) {
        WindowInfo window = SelectSingleWindow(criteria);
        using Bitmap bitmap = ScreenshotService.CaptureWindow(window.Handle);
        return SaveScreenshot(bitmap, "window", $"window-{window.ProcessId}", outputPath, window: MapWindow(window));
    }

    public static ProcessLaunchResult LaunchProcess(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new CommandLineException("A process path or command is required.");
        }

        if (waitForInputIdleMilliseconds.HasValue && waitForInputIdleMilliseconds.Value < 0) {
            throw new CommandLineException("waitForInputIdleMilliseconds must be zero or greater.");
        }

        if (!string.IsNullOrWhiteSpace(workingDirectory) && !Directory.Exists(workingDirectory)) {
            throw new CommandLineException($"The working directory '{workingDirectory}' does not exist.");
        }

        var startInfo = new ProcessStartInfo(filePath) {
            UseShellExecute = true
        };
        if (!string.IsNullOrWhiteSpace(arguments)) {
            startInfo.Arguments = arguments;
        }
        if (!string.IsNullOrWhiteSpace(workingDirectory)) {
            startInfo.WorkingDirectory = workingDirectory;
        }

        using Process process = Process.Start(startInfo) ?? throw new CommandLineException($"Failed to start process '{filePath}'.");

        if (waitForInputIdleMilliseconds.HasValue && waitForInputIdleMilliseconds.Value > 0) {
            try {
                process.WaitForInputIdle(waitForInputIdleMilliseconds.Value);
            } catch (InvalidOperationException) {
                // Ignore processes without a message loop or already exited.
            }
        }

        process.Refresh();
        Thread.Sleep(200);
        WindowResult? mainWindow = TryGetWindowForProcess(process.Id) ?? TryGetWindowForProcessName(filePath);

        return new ProcessLaunchResult {
            FilePath = filePath,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? null : Path.GetFullPath(workingDirectory),
            ProcessId = process.Id,
            HasExited = process.HasExited,
            MainWindow = mainWindow
        };
    }

    public static WaitForWindowResult WaitForWindow(WindowSelectionCriteria criteria, int timeoutMilliseconds, int intervalMilliseconds) {
        if (timeoutMilliseconds <= 0) {
            throw new CommandLineException("timeoutMilliseconds must be greater than zero.");
        }

        if (intervalMilliseconds <= 0) {
            throw new CommandLineException("intervalMilliseconds must be greater than zero.");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            var windows = SelectWindows(criteria);
            if (windows.Count > 0) {
                IReadOnlyList<WindowInfo> selected = criteria.All ? windows : new[] { windows[0] };
                return new WaitForWindowResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    Count = selected.Count,
                    Windows = selected.Select(MapWindow).ToArray()
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new CommandLineException($"Timed out after {timeoutMilliseconds}ms waiting for a matching window.");
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

    private static WindowInfo SelectSingleWindow(WindowSelectionCriteria criteria) {
        IReadOnlyList<WindowInfo> windows = SelectTargetWindows(criteria);
        return windows[0];
    }

    private static IReadOnlyList<WindowControlTargetInfo> SelectTargetControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        List<WindowControlTargetInfo> controls = GetControlTargets(windowCriteria, controlCriteria, allWindows);
        if (controls.Count == 0) {
            throw new CommandLineException("No matching controls were found.");
        }

        if (controlCriteria.All) {
            return controls;
        }

        return new[] { controls[0] };
    }

    private static List<WindowControlTargetInfo> GetControlTargets(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        WindowManager manager = new WindowManager();
        WindowQueryOptions query = CreateWindowQuery(windowCriteria);
        List<WindowInfo> windows = manager.GetWindows(query);
        if (windows.Count == 0) {
            throw new CommandLineException("No matching windows were found.");
        }

        if (!allWindows && windows.Count > 1) {
            windows = new List<WindowInfo> { windows[0] };
        }

        var results = new List<WindowControlTargetInfo>();
        WindowControlQueryOptions controlQuery = CreateControlQuery(controlCriteria);
        foreach (WindowInfo window in windows) {
            List<WindowControlInfo> controls = manager.GetControls(window, controlQuery);
            foreach (WindowControlInfo control in controls) {
                results.Add(new WindowControlTargetInfo {
                    Window = window,
                    Control = control
                });
            }
        }

        return results;
    }

    private static List<WindowInfo> SelectWindows(WindowSelectionCriteria criteria) {
        return new WindowManager().GetWindows(CreateWindowQuery(criteria));
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

    private static ControlResult MapControl(WindowControlTargetInfo target) {
        return new ControlResult {
            Handle = $"0x{target.Control.Handle.ToInt64():X}",
            ClassName = target.Control.ClassName,
            Id = target.Control.Id,
            Text = WindowTextHelper.GetWindowText(target.Control.Handle),
            ParentWindow = MapWindow(target.Window)
        };
    }

    private static ControlActionResult BuildControlActionResult(string action, IReadOnlyList<WindowControlTargetInfo> controls) {
        return new ControlActionResult {
            Action = action,
            Count = controls.Count,
            Controls = controls.Select(MapControl).ToArray()
        };
    }

    private static WindowResult? TryGetWindowForProcess(int processId) {
        var criteria = new WindowSelectionCriteria {
            ProcessId = processId,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true,
            All = true
        };

        var windows = SelectWindows(criteria);
        WindowInfo? preferred = windows.FirstOrDefault(window => !string.IsNullOrWhiteSpace(window.Title))
            ?? windows.FirstOrDefault();
        return preferred == null ? null : MapWindow(preferred);
    }

    private static WindowResult? TryGetWindowForProcessName(string filePath) {
        string processName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(processName)) {
            return null;
        }

        var criteria = new WindowSelectionCriteria {
            ProcessNamePattern = processName,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true,
            All = true
        };

        var windows = SelectWindows(criteria);
        WindowInfo? preferred = windows.FirstOrDefault(window => !string.IsNullOrWhiteSpace(window.Title))
            ?? windows.FirstOrDefault();
        return preferred == null ? null : MapWindow(preferred);
    }

    private static MouseButton ParseMouseButton(string? value) {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("left", StringComparison.OrdinalIgnoreCase)) {
            return MouseButton.Left;
        }

        if (value.Equals("right", StringComparison.OrdinalIgnoreCase)) {
            return MouseButton.Right;
        }

        throw new CommandLineException($"Unsupported mouse button '{value}'.");
    }

    private static VirtualKey[] ParseVirtualKeys(IReadOnlyList<string> values) {
        if (values == null || values.Count == 0) {
            throw new CommandLineException("At least one key is required.");
        }

        var keys = new List<VirtualKey>();
        foreach (string raw in values) {
            foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                keys.Add(ParseVirtualKey(part));
            }
        }

        if (keys.Count == 0) {
            throw new CommandLineException("At least one key is required.");
        }

        return keys.ToArray();
    }

    private static VirtualKey ParseVirtualKey(string value) {
        if (Enum.TryParse<VirtualKey>(value, ignoreCase: true, out VirtualKey parsed)) {
            return parsed;
        }

        if (value.Length == 1) {
            char character = char.ToUpperInvariant(value[0]);
            string enumName = character switch {
                >= 'A' and <= 'Z' => $"VK_{character}",
                >= '0' and <= '9' => $"VK_{character}",
                ' ' => "VK_SPACE",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(enumName) && Enum.TryParse<VirtualKey>(enumName, ignoreCase: true, out parsed)) {
                return parsed;
            }
        }

        throw new CommandLineException($"Unsupported key '{value}'.");
    }

    private static ScreenshotResult SaveScreenshot(Bitmap bitmap, string kind, string prefix, string? outputPath, int? monitorIndex = null, string? monitorDeviceName = null, WindowResult? window = null) {
        string path = ResolveScreenshotPath(prefix, outputPath);
        bitmap.Save(path, ImageFormat.Png);
        return new ScreenshotResult {
            Kind = kind,
            Path = path,
            Width = bitmap.Width,
            Height = bitmap.Height,
            MonitorIndex = monitorIndex,
            MonitorDeviceName = monitorDeviceName,
            Window = window
        };
    }

    private static string ResolveScreenshotPath(string prefix, string? outputPath) {
        string path = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(NamedStorage.GetCapturesDirectory(), $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png")
            : outputPath;

        if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) {
            path += ".png";
        }

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
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
            criteria.Active ||
            !string.IsNullOrWhiteSpace(criteria.Handle);
    }

    private static WindowQueryOptions CreateWindowQuery(WindowSelectionCriteria criteria) {
        return new WindowQueryOptions {
            TitlePattern = criteria.TitlePattern,
            ProcessNamePattern = criteria.ProcessNamePattern,
            ClassNamePattern = criteria.ClassNamePattern,
            Handle = string.IsNullOrWhiteSpace(criteria.Handle) ? null : ParseHandle(criteria.Handle),
            ActiveWindow = criteria.Active,
            IncludeHidden = criteria.IncludeHidden,
            IncludeCloaked = criteria.IncludeCloaked,
            IncludeOwned = criteria.IncludeOwned,
            IncludeEmptyTitles = criteria.IncludeEmptyTitles ? true : HasExplicitSelector(criteria) ? null : false,
            ProcessId = criteria.ProcessId ?? 0
        };
    }

    private static WindowControlQueryOptions CreateControlQuery(ControlSelectionCriteria criteria) {
        return new WindowControlQueryOptions {
            ClassNamePattern = criteria.ClassNamePattern,
            TextPattern = criteria.TextPattern,
            Id = criteria.Id,
            Handle = string.IsNullOrWhiteSpace(criteria.Handle) ? null : ParseHandle(criteria.Handle)
        };
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
