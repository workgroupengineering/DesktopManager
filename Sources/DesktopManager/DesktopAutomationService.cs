using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides higher-level desktop automation orchestration on top of WindowManager and related services.
/// </summary>
public sealed class DesktopAutomationService {
    private const int PreferredWindowRediscoveryMilliseconds = 1000;
    private readonly WindowManager _windowManager;
    private readonly Monitors _monitors;
    private static readonly JsonSerializerOptions TargetSerializerOptions = new() {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopAutomationService"/> class.
    /// </summary>
    public DesktopAutomationService() {
        _windowManager = new WindowManager();
        _monitors = new Monitors();
    }

    /// <summary>
    /// Gets matching monitors.
    /// </summary>
    public IReadOnlyList<Monitor> GetMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null) {
        return _monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName);
    }

    /// <summary>
    /// Gets matching windows.
    /// </summary>
    public List<WindowInfo> GetWindows(WindowQueryOptions options) {
        return _windowManager.GetWindows(options);
    }

    /// <summary>
    /// Gets the current foreground window when it can be resolved.
    /// </summary>
    public WindowInfo? GetActiveWindow(bool includeHidden = true, bool includeCloaked = true, bool includeOwned = true, bool includeEmptyTitles = true) {
        return _windowManager.GetActiveWindow(includeHidden, includeCloaked, includeOwned, includeEmptyTitles);
    }

    /// <summary>
    /// Determines whether at least one window matches the supplied query.
    /// </summary>
    public bool WindowExists(WindowQueryOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        return _windowManager.GetWindows(options).Count > 0;
    }

    /// <summary>
    /// Determines whether the current active window matches the supplied query.
    /// </summary>
    public bool ActiveWindowMatches(WindowQueryOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        WindowQueryOptions activeWindowOptions = new WindowQueryOptions {
            TitlePattern = options.TitlePattern,
            ProcessNamePattern = options.ProcessNamePattern,
            ClassNamePattern = options.ClassNamePattern,
            TitleRegex = options.TitleRegex,
            ProcessId = options.ProcessId,
            Handle = options.Handle,
            ActiveWindow = true,
            IncludeEmptyTitles = options.IncludeEmptyTitles ?? true,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IsVisible = options.IsVisible,
            State = options.State,
            IsTopMost = options.IsTopMost,
            ZOrderMin = options.ZOrderMin,
            ZOrderMax = options.ZOrderMax
        };

        return _windowManager.GetWindows(activeWindowOptions).Count > 0;
    }

    /// <summary>
    /// Moves and optionally resizes matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> MoveWindows(WindowQueryOptions options, int? monitorIndex, int? x, int? y, int? width, int? height, bool activate, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);

        Monitor? monitor = null;
        if (monitorIndex.HasValue) {
            monitor = _monitors.GetMonitors(index: monitorIndex.Value).FirstOrDefault();
            if (monitor == null) {
                throw new InvalidOperationException($"Monitor with index {monitorIndex.Value} was not found.");
            }
        }

        foreach (WindowInfo window in windows) {
            if (monitor != null) {
                _windowManager.MoveWindowToMonitor(window, monitor);
            }

            if (x.HasValue || y.HasValue || width.HasValue || height.HasValue) {
                _windowManager.SetWindowPosition(window, x ?? -1, y ?? -1, width ?? -1, height ?? -1);
            }

            if (activate) {
                _windowManager.ActivateWindow(window);
            }
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Focuses matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> FocusWindows(WindowQueryOptions options, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.ActivateWindow(window);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Minimizes matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> MinimizeWindows(WindowQueryOptions options, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.MinimizeWindow(window);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Snaps matching windows to a predefined position.
    /// </summary>
    public IReadOnlyList<WindowInfo> SnapWindows(WindowQueryOptions options, SnapPosition position, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.SnapWindow(window, position);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Gets window and client-area geometry for one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopWindowGeometry> GetWindowGeometry(WindowQueryOptions options, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        return ResolveWindows(options, all)
            .Select(DescribeWindowGeometry)
            .ToArray();
    }

    /// <summary>
    /// Sends text to matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> TypeWindowText(WindowQueryOptions options, string text, bool paste, int delayMilliseconds, bool all = false) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (paste) {
                _windowManager.PasteText(window, text);
            } else {
                _windowManager.TypeText(window, text, delayMilliseconds);
            }
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Sends keys to matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> SendWindowKeys(WindowQueryOptions options, IReadOnlyList<VirtualKey> keys, bool activate, bool all = false) {
        if (keys == null) {
            throw new ArgumentNullException(nameof(keys));
        }
        if (keys.Count == 0) {
            throw new ArgumentException("No keys specified.", nameof(keys));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.SendKeys(window, keys, new WindowInputOptions {
                ActivateWindow = activate
            });
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Clicks a point relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowPoint(WindowQueryOptions options, int x, int y, MouseButton button, bool activate, bool all = false) {
        return ClickWindowPoint(options, x, y, null, null, button, activate, clientArea: false, all);
    }

    /// <summary>
    /// Clicks a point relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowPoint(WindowQueryOptions options, int x, int y, MouseButton button, bool activate, bool clientArea, bool all = false) {
        return ClickWindowPoint(options, x, y, null, null, button, activate, clientArea, all);
    }

    /// <summary>
    /// Clicks a point relative to each matching window using either pixels or normalized ratios.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowPoint(WindowQueryOptions options, int? x, int? y, double? xRatio, double? yRatio, MouseButton button, bool activate, bool clientArea, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            (int screenX, int screenY) = ResolveWindowPoint(window, x, y, xRatio, yRatio, clientArea);
            Thread.Sleep(50);
            _windowManager.MoveMouse(screenX, screenY);
            _windowManager.ClickMouse(button);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Drags between two points relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowPoints(WindowQueryOptions options, int startX, int startY, int endX, int endY, MouseButton button, int stepDelayMilliseconds, bool activate, bool clientArea, bool all = false) {
        return DragWindowPoints(options, startX, startY, null, null, endX, endY, null, null, button, stepDelayMilliseconds, activate, clientArea, all);
    }

    /// <summary>
    /// Drags between two points relative to each matching window using either pixels or normalized ratios.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowPoints(WindowQueryOptions options, int? startX, int? startY, double? startXRatio, double? startYRatio, int? endX, int? endY, double? endXRatio, double? endYRatio, MouseButton button, int stepDelayMilliseconds, bool activate, bool clientArea, bool all = false) {
        if (stepDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(stepDelayMilliseconds), "stepDelayMilliseconds must be zero or greater.");
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            (int startScreenX, int startScreenY) = ResolveWindowPoint(window, startX, startY, startXRatio, startYRatio, clientArea);
            (int endScreenX, int endScreenY) = ResolveWindowPoint(window, endX, endY, endXRatio, endYRatio, clientArea);
            _windowManager.DragMouse(button, startScreenX, startScreenY, endScreenX, endScreenY, stepDelayMilliseconds);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Scrolls the mouse wheel at a point relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowPoint(WindowQueryOptions options, int x, int y, int delta, bool activate, bool clientArea, bool all = false) {
        return ScrollWindowPoint(options, x, y, null, null, delta, activate, clientArea, all);
    }

    /// <summary>
    /// Scrolls the mouse wheel at a point relative to each matching window using either pixels or normalized ratios.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowPoint(WindowQueryOptions options, int? x, int? y, double? xRatio, double? yRatio, int delta, bool activate, bool clientArea, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            (int screenX, int screenY) = ResolveWindowPoint(window, x, y, xRatio, yRatio, clientArea);
            Thread.Sleep(50);
            _windowManager.MoveMouse(screenX, screenY);
            _windowManager.ScrollMouse(delta);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Saves a reusable window-relative target definition.
    /// </summary>
    public DesktopWindowTargetDefinition SaveWindowTarget(string name, DesktopWindowTargetDefinition definition) {
        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }

        ValidateWindowTargetDefinition(definition);

        string path = DesktopStateStore.GetTargetPath(name);
        File.WriteAllText(path, JsonSerializer.Serialize(definition, TargetSerializerOptions));
        return definition;
    }

    /// <summary>
    /// Gets a previously saved reusable window-relative target definition.
    /// </summary>
    public DesktopWindowTargetDefinition GetWindowTarget(string name) {
        string path = DesktopStateStore.GetTargetPath(name);
        if (!File.Exists(path)) {
            throw new InvalidOperationException($"Named target '{name}' was not found.");
        }

        DesktopWindowTargetDefinition? definition = JsonSerializer.Deserialize<DesktopWindowTargetDefinition>(File.ReadAllText(path));
        if (definition == null) {
            throw new InvalidOperationException($"Named target '{name}' could not be read.");
        }

        ValidateWindowTargetDefinition(definition);
        return definition;
    }

    /// <summary>
    /// Lists saved reusable window-relative target names.
    /// </summary>
    public IReadOnlyList<string> ListWindowTargets() {
        return DesktopStateStore.ListNames("targets");
    }

    /// <summary>
    /// Saves a reusable control target definition.
    /// </summary>
    public DesktopControlTargetDefinition SaveControlTarget(string name, DesktopControlTargetDefinition definition) {
        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }

        ValidateControlTargetDefinition(definition);

        string path = DesktopStateStore.GetControlTargetPath(name);
        File.WriteAllText(path, JsonSerializer.Serialize(definition, TargetSerializerOptions));
        return definition;
    }

    /// <summary>
    /// Gets a previously saved reusable control target definition.
    /// </summary>
    public DesktopControlTargetDefinition GetControlTarget(string name) {
        string path = DesktopStateStore.GetControlTargetPath(name);
        if (!File.Exists(path)) {
            throw new InvalidOperationException($"Named control target '{name}' was not found.");
        }

        DesktopControlTargetDefinition? definition = JsonSerializer.Deserialize<DesktopControlTargetDefinition>(File.ReadAllText(path));
        if (definition == null) {
            throw new InvalidOperationException($"Named control target '{name}' could not be read.");
        }

        ValidateControlTargetDefinition(definition);
        return definition;
    }

    /// <summary>
    /// Lists saved reusable control target names.
    /// </summary>
    public IReadOnlyList<string> ListControlTargets() {
        return DesktopStateStore.ListNames("control-targets");
    }

    /// <summary>
    /// Resolves a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopResolvedControlTarget> ResolveControlTargets(WindowQueryOptions options, string targetName, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        WindowControlQueryOptions controlOptions = CreateControlQuery(definition);
        IReadOnlyList<WindowControlTargetInfo> matches = RequireControls(options, controlOptions, allWindows, allControls);
        return matches
            .Select(match => new DesktopResolvedControlTarget {
                Name = targetName,
                Definition = CloneControlTargetDefinition(definition),
                Window = match.Window,
                Control = match.Control
            })
            .ToArray();
    }

    /// <summary>
    /// Gets a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> GetControlTargets(WindowQueryOptions options, string targetName, bool allWindows = false, bool allControls = true) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return GetControls(options, CreateControlQuery(definition), allWindows, allControls);
    }

    /// <summary>
    /// Collects control discovery diagnostics for a saved control target.
    /// </summary>
    public IReadOnlyList<DesktopControlDiscoveryDiagnostics> GetControlTargetDiagnostics(WindowQueryOptions options, string targetName, bool allWindows = false, int sampleLimit = 10, bool includeActionProbe = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return GetControlDiagnostics(options, CreateControlQuery(definition), allWindows, sampleLimit, includeActionProbe);
    }

    /// <summary>
    /// Determines whether a saved control target resolves against at least one matching window.
    /// </summary>
    public bool ControlTargetExists(WindowQueryOptions options, string targetName, bool allWindows = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return ControlExists(options, CreateControlQuery(definition), allWindows);
    }

    /// <summary>
    /// Waits for a saved control target to resolve against one or more matching windows.
    /// </summary>
    public DesktopControlWaitResult WaitForControlTarget(WindowQueryOptions options, string targetName, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return WaitForControls(options, CreateControlQuery(definition), timeoutMilliseconds, intervalMilliseconds, allWindows, allControls);
    }

    /// <summary>
    /// Clicks a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> ClickControlTarget(WindowQueryOptions options, string targetName, MouseButton button, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return ClickControls(options, CreateControlQuery(definition), button, allWindows, allControls);
    }

    /// <summary>
    /// Sets text on a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SetControlTargetText(WindowQueryOptions options, string targetName, string text, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        WindowControlQueryOptions controlOptions = CreateControlQuery(definition);
        controlOptions.EnsureForegroundWindow = controlOptions.EnsureForegroundWindow || ensureForegroundWindow;
        controlOptions.AllowForegroundInputFallback = allowForegroundInputFallback;
        return SetControlText(options, controlOptions, text, allWindows, allControls);
    }

    /// <summary>
    /// Sends keys to a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SendControlTargetKeys(WindowQueryOptions options, string targetName, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        WindowControlQueryOptions controlOptions = CreateControlQuery(definition);
        controlOptions.EnsureForegroundWindow = controlOptions.EnsureForegroundWindow || ensureForegroundWindow;
        controlOptions.AllowForegroundInputFallback = allowForegroundInputFallback;
        return SendControlKeys(options, controlOptions, keys, allWindows, allControls);
    }

    /// <summary>
    /// Resolves a saved target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopResolvedWindowTarget> ResolveWindowTargets(WindowQueryOptions options, string targetName, bool all = false) {
        DesktopWindowTargetDefinition definition = GetWindowTarget(targetName);
        return ResolveWindowTargets(options, targetName, definition, all);
    }

    /// <summary>
    /// Captures the area described by a named window target against a matching window.
    /// </summary>
    public DesktopCapture CaptureWindowTarget(WindowQueryOptions options, string targetName) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopResolvedWindowTarget target = ResolveWindowTargets(options, targetName, all: false).FirstOrDefault()
            ?? throw new InvalidOperationException($"Named target '{targetName}' could not be resolved against a matching window.");
        if (!target.ScreenWidth.HasValue || !target.ScreenHeight.HasValue) {
            throw new InvalidOperationException($"Named target '{targetName}' does not define a capture area. Save it with width/height or widthRatio/heightRatio.");
        }

        return new DesktopCapture {
            Kind = "window-target",
            Bitmap = ScreenshotService.CaptureRegion(target.ScreenX, target.ScreenY, target.ScreenWidth.Value, target.ScreenHeight.Value),
            Window = target.Geometry.Window,
            Geometry = target.Geometry
        };
    }

    /// <summary>
    /// Clicks a saved target relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowTarget(WindowQueryOptions options, string targetName, MouseButton button, bool activate, bool all = false) {
        IReadOnlyList<DesktopResolvedWindowTarget> targets = ResolveWindowTargets(options, targetName, all);
        foreach (DesktopResolvedWindowTarget target in targets) {
            if (activate) {
                _windowManager.ActivateWindow(target.Geometry.Window);
                Thread.Sleep(100);
            }

            Thread.Sleep(50);
            _windowManager.MoveMouse(target.ScreenX, target.ScreenY);
            _windowManager.ClickMouse(button);
        }

        return RefreshWindows(targets.Select(target => target.Geometry.Window).ToArray());
    }

    /// <summary>
    /// Scrolls at a saved target relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowTarget(WindowQueryOptions options, string targetName, int delta, bool activate, bool all = false) {
        IReadOnlyList<DesktopResolvedWindowTarget> targets = ResolveWindowTargets(options, targetName, all);
        foreach (DesktopResolvedWindowTarget target in targets) {
            if (activate) {
                _windowManager.ActivateWindow(target.Geometry.Window);
                Thread.Sleep(100);
            }

            Thread.Sleep(50);
            _windowManager.MoveMouse(target.ScreenX, target.ScreenY);
            _windowManager.ScrollMouse(delta);
        }

        return RefreshWindows(targets.Select(target => target.Geometry.Window).ToArray());
    }

    /// <summary>
    /// Drags between two saved targets relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowTargets(WindowQueryOptions options, string startTargetName, string endTargetName, MouseButton button, int stepDelayMilliseconds, bool activate, bool all = false) {
        if (stepDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(stepDelayMilliseconds), "stepDelayMilliseconds must be zero or greater.");
        }

        DesktopWindowTargetDefinition startDefinition = GetWindowTarget(startTargetName);
        DesktopWindowTargetDefinition endDefinition = GetWindowTarget(endTargetName);
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
            DesktopResolvedWindowTarget startTarget = ResolveWindowTarget(startTargetName, startDefinition, geometry);
            DesktopResolvedWindowTarget endTarget = ResolveWindowTarget(endTargetName, endDefinition, geometry);
            _windowManager.DragMouse(button, startTarget.ScreenX, startTarget.ScreenY, endTarget.ScreenX, endTarget.ScreenY, stepDelayMilliseconds);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Gets matching controls for one or more windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> GetControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false, bool allControls = true) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        IReadOnlyList<WindowInfo> windows = GetMatchingWindows(windowOptions, allWindows);
        IReadOnlyList<WindowControlTargetInfo> controls = GetControls(windows, controlOptions, allControls: true);
        if (controls.Count == 0) {
            return controls;
        }

        if (allControls) {
            return controls;
        }

        return new[] { controls[0] };
    }

    /// <summary>
    /// Determines whether at least one control matches the supplied query.
    /// </summary>
    public bool ControlExists(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        return GetControls(windowOptions, controlOptions, allWindows, allControls: true).Count > 0;
    }

    /// <summary>
    /// Collects control discovery diagnostics for one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopControlDiscoveryDiagnostics> GetControlDiagnostics(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false, int sampleLimit = 10, bool includeActionProbe = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        if (sampleLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "sampleLimit must be zero or greater.");
        }

        List<WindowInfo> windows = _windowManager.GetWindows(windowOptions);
        if (!allWindows && windows.Count > 1) {
            windows = new List<WindowInfo> { windows[0] };
        }

        return windows
            .Select(window => _windowManager.DiagnoseControls(window, controlOptions, sampleLimit, includeActionProbe))
            .ToArray();
    }

    /// <summary>
    /// Clicks matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> ClickControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, MouseButton button, bool allWindows = false, bool allControls = false) {
        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            ClickControl(control.Window, control.Control, button);
        }

        return controls;
    }

    /// <summary>
    /// Sets text on matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SetControlText(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, string text, bool allWindows = false, bool allControls = false) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            SetControlText(control.Window, control.Control, text, controlOptions?.EnsureForegroundWindow ?? false, controlOptions?.AllowForegroundInputFallback ?? false);
        }

        return controls;
    }

    /// <summary>
    /// Sends keys to matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SendControlKeys(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, IReadOnlyList<VirtualKey> keys, bool allWindows = false, bool allControls = false) {
        if (keys == null || keys.Count == 0) {
            throw new ArgumentException("At least one key is required.", nameof(keys));
        }

        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            SendControlKeys(control.Window, control.Control, keys, controlOptions?.EnsureForegroundWindow ?? false, controlOptions?.AllowForegroundInputFallback ?? false);
        }

        return controls;
    }

    /// <summary>
    /// Clicks the supplied control using either Win32 or UI Automation routing.
    /// </summary>
    public void ClickControl(WindowControlInfo control, MouseButton button = MouseButton.Left) {
        ClickControl(ResolveParentWindow(control), control, button);
    }

    /// <summary>
    /// Sets text on the supplied control using either Win32 or UI Automation routing.
    /// </summary>
    public void SetControlText(WindowControlInfo control, string text, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false) {
        SetControlText(ResolveParentWindow(control), control, text, ensureForegroundWindow, allowForegroundInputFallback);
    }

    /// <summary>
    /// Sends keys to the supplied control using either Win32 or UI Automation routing.
    /// </summary>
    public void SendControlKeys(WindowControlInfo control, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false) {
        SendControlKeys(ResolveParentWindow(control), control, keys, ensureForegroundWindow, allowForegroundInputFallback);
    }

    private void ClickControl(WindowInfo window, WindowControlInfo control, MouseButton button) {
        var uiAutomation = new UiAutomationControlService();
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            if (!uiAutomation.TryInvoke(window, control)) {
                throw new InvalidOperationException("The UI Automation control could not be invoked.");
            }
            return;
        }

        _windowManager.ClickControl(control, button);
    }

    private void SetControlText(WindowInfo window, WindowControlInfo control, string text, bool ensureForegroundWindow, bool allowForegroundInputFallback) {
        var uiAutomation = new UiAutomationControlService();
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            if (uiAutomation.TrySetValue(window, control, text)) {
                return;
            }

            string? validationError = ValidateUiAutomationTextFallback(control, allowForegroundInputFallback);
            if (validationError != null) {
                throw new InvalidOperationException(validationError);
            }

            if (!uiAutomation.TrySetText(window, control, text, ensureForegroundWindow)) {
                throw new InvalidOperationException("The UI Automation control text could not be set even with foreground input fallback enabled.");
            }

            return;
        }

        _windowManager.SetControlText(control, text);
    }

    private void SendControlKeys(WindowInfo window, WindowControlInfo control, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow, bool allowForegroundInputFallback) {
        var uiAutomation = new UiAutomationControlService();
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            string? validationError = ValidateUiAutomationKeyFallback(control, allowForegroundInputFallback);
            if (validationError != null) {
                throw new InvalidOperationException(validationError);
            }

            if (!uiAutomation.TrySendKeys(window, control, keys, ensureForegroundWindow)) {
                throw new InvalidOperationException("The UI Automation control could not receive keys even with foreground input fallback enabled.");
            }

            return;
        }

        _windowManager.SendControlKeys(control, keys.ToArray());
    }

    internal static string? ValidateUiAutomationTextFallback(WindowControlInfo control, bool allowForegroundInputFallback) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Source != WindowControlSource.UiAutomation || control.Handle != IntPtr.Zero) {
            return null;
        }

        if (!control.SupportsForegroundInputFallback) {
            return "The selected UI Automation control does not expose direct value setting and is not keyboard-focusable for foreground input fallback.";
        }

        if (!allowForegroundInputFallback) {
            return "The UI Automation control does not support direct value setting. Enable foreground input fallback only when you intentionally allow focused input for modern app controls.";
        }

        return null;
    }

    internal static string? ValidateUiAutomationKeyFallback(WindowControlInfo control, bool allowForegroundInputFallback) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Source != WindowControlSource.UiAutomation || control.Handle != IntPtr.Zero) {
            return null;
        }

        if (!control.SupportsForegroundInputFallback) {
            return "The selected UI Automation control is not keyboard-focusable and cannot receive foreground fallback key input.";
        }

        if (!allowForegroundInputFallback) {
            return "The selected UI Automation control does not expose a Win32 handle. Enable foreground input fallback only when you intentionally allow focused input for modern app controls.";
        }

        return null;
    }

    /// <summary>
    /// Waits for matching windows to appear.
    /// </summary>
    public DesktopWindowWaitResult WaitForWindows(WindowQueryOptions options, int timeoutMilliseconds, int intervalMilliseconds, bool all = false) {
        if (timeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "timeoutMilliseconds must be zero or greater.");
        }

        if (intervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds), "intervalMilliseconds must be greater than zero.");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            List<WindowInfo> windows = _windowManager.GetWindows(options);
            if (windows.Count > 0) {
                IReadOnlyList<WindowInfo> selected = all ? windows : new[] { windows[0] };
                return new DesktopWindowWaitResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    Windows = selected
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching window.");
    }

    /// <summary>
    /// Waits for matching controls to appear.
    /// </summary>
    public DesktopControlWaitResult WaitForControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows = false, bool allControls = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        if (timeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "timeoutMilliseconds must be zero or greater.");
        }

        if (intervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds), "intervalMilliseconds must be greater than zero.");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        var preferredWindowHandles = new HashSet<IntPtr>();
        long nextWindowRediscoveryAtMilliseconds = 0;
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            IReadOnlyList<WindowControlTargetInfo> controls = Array.Empty<WindowControlTargetInfo>();
            if (preferredWindowHandles.Count > 0) {
                IReadOnlyList<WindowInfo> preferredWindows = GetWindowsByHandle(preferredWindowHandles, allWindows);
                if (preferredWindows.Count > 0) {
                    controls = GetControls(preferredWindows, controlOptions, allControls: true);
                    if (controls.Count > 0) {
                        IReadOnlyList<WindowControlTargetInfo> preferredSelected = allControls ? controls : new[] { controls[0] };
                        return new DesktopControlWaitResult {
                            ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                            Controls = preferredSelected
                        };
                    }

                    if (stopwatch.ElapsedMilliseconds < nextWindowRediscoveryAtMilliseconds) {
                        Thread.Sleep(intervalMilliseconds);
                        continue;
                    }
                } else {
                    preferredWindowHandles.Clear();
                }
            }

            IReadOnlyList<WindowInfo> windows = GetMatchingWindows(windowOptions, allWindows);
            if (windows.Count > 0) {
                RememberWindowHandles(preferredWindowHandles, windows);
                nextWindowRediscoveryAtMilliseconds = stopwatch.ElapsedMilliseconds + PreferredWindowRediscoveryMilliseconds;
                controls = GetControls(windows, controlOptions, allControls: true);
            }

            if (controls.Count > 0) {
                IReadOnlyList<WindowControlTargetInfo> selected = allControls ? controls : new[] { controls[0] };
                return new DesktopControlWaitResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    Controls = selected
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching control.");
    }

    /// <summary>
    /// Launches a desktop process.
    /// </summary>
    public DesktopProcessLaunchInfo LaunchProcess(DesktopProcessStartOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FilePath)) {
            throw new ArgumentException("A process path or command is required.", nameof(options));
        }

        if (options.WaitForInputIdleMilliseconds.HasValue && options.WaitForInputIdleMilliseconds.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(options.WaitForInputIdleMilliseconds), "waitForInputIdleMilliseconds must be zero or greater.");
        }

        if (options.WaitForWindowMilliseconds.HasValue && options.WaitForWindowMilliseconds.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(options.WaitForWindowMilliseconds), "waitForWindowMilliseconds must be zero or greater.");
        }

        if (options.WaitForWindowIntervalMilliseconds.HasValue && options.WaitForWindowIntervalMilliseconds.Value <= 0) {
            throw new ArgumentOutOfRangeException(nameof(options.WaitForWindowIntervalMilliseconds), "waitForWindowIntervalMilliseconds must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory) && !Directory.Exists(options.WorkingDirectory)) {
            throw new DirectoryNotFoundException($"The working directory '{options.WorkingDirectory}' does not exist.");
        }

        var startInfo = new ProcessStartInfo(options.FilePath) {
            UseShellExecute = true
        };
        if (!string.IsNullOrWhiteSpace(options.Arguments)) {
            startInfo.Arguments = options.Arguments;
        }
        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory)) {
            startInfo.WorkingDirectory = options.WorkingDirectory;
        }

        string? primaryProcessNameHint = GetProcessNameHint(options.FilePath);
        HashSet<IntPtr> preLaunchWindowHandles = CaptureWindowHandlesForProcesses(CollectProcessNameHints(primaryProcessNameHint));
        DateTime launchStartedUtc = DateTime.UtcNow;
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process '{options.FilePath}'.");

        if (options.WaitForInputIdleMilliseconds.HasValue && options.WaitForInputIdleMilliseconds.Value > 0) {
            try {
                process.WaitForInputIdle(options.WaitForInputIdleMilliseconds.Value);
            } catch (InvalidOperationException) {
            }
        }

        process.Refresh();
        IReadOnlyList<string> processNameHints = CollectProcessNameHints(primaryProcessNameHint, GetProcessNameHint(process, options.FilePath));
        int waitForWindowMilliseconds = options.WaitForWindowMilliseconds ?? 2000;
        int waitForWindowIntervalMilliseconds = options.WaitForWindowIntervalMilliseconds ?? 200;
        WindowInfo? mainWindow = TryResolveLaunchedWindow(process.Id, processNameHints, launchStartedUtc, preLaunchWindowHandles, options.WindowTitlePattern, options.WindowClassNamePattern, waitForWindowMilliseconds, waitForWindowIntervalMilliseconds);
        if (options.RequireWindow && mainWindow == null) {
            throw new TimeoutException(BuildMissingLaunchedWindowMessage(processNameHints, options.WindowTitlePattern, options.WindowClassNamePattern, waitForWindowMilliseconds));
        }

        return new DesktopProcessLaunchInfo {
            FilePath = options.FilePath,
            Arguments = options.Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? null : Path.GetFullPath(options.WorkingDirectory),
            ProcessId = process.Id,
            ResolvedProcessId = mainWindow == null ? null : (int?)mainWindow.ProcessId,
            HasExited = process.HasExited,
            MainWindow = mainWindow
        };
    }

    /// <summary>
    /// Captures the entire desktop.
    /// </summary>
    public DesktopCapture CaptureDesktop() {
        return new DesktopCapture {
            Kind = "desktop",
            Bitmap = ScreenshotService.CaptureScreen()
        };
    }

    /// <summary>
    /// Captures a monitor.
    /// </summary>
    public DesktopCapture CaptureMonitor(int? monitorIndex = null, string? deviceId = null, string? deviceName = null) {
        Monitor monitor = _monitors.GetMonitors(index: monitorIndex, deviceId: deviceId, deviceName: deviceName).FirstOrDefault()
            ?? throw new InvalidOperationException("No matching monitor was found.");

        return new DesktopCapture {
            Kind = "monitor",
            Bitmap = ScreenshotService.CaptureMonitor(index: monitor.Index, deviceId: monitor.DeviceId, deviceName: monitor.DeviceName),
            MonitorIndex = monitor.Index,
            MonitorDeviceName = monitor.DeviceName
        };
    }

    /// <summary>
    /// Captures a desktop region.
    /// </summary>
    public DesktopCapture CaptureRegion(int left, int top, int width, int height) {
        return new DesktopCapture {
            Kind = "region",
            Bitmap = ScreenshotService.CaptureRegion(left, top, width, height)
        };
    }

    /// <summary>
    /// Captures a single matching window.
    /// </summary>
    public DesktopCapture CaptureWindow(WindowQueryOptions options) {
        WindowInfo window = ResolveSingleWindow(options);
        return new DesktopCapture {
            Kind = "window",
            Bitmap = ScreenshotService.CaptureWindow(window.Handle),
            Window = window,
            Geometry = DescribeWindowGeometry(window)
        };
    }

    /// <summary>
    /// Saves the current layout to the specified path.
    /// </summary>
    public void SaveLayout(string path) {
        _windowManager.SaveLayout(path);
    }

    /// <summary>
    /// Loads a layout from the specified path.
    /// </summary>
    public void LoadLayout(string path, bool validate = false) {
        _windowManager.LoadLayout(path, validate);
    }

    private IReadOnlyList<WindowInfo> ResolveWindows(WindowQueryOptions options, bool all) {
        IReadOnlyList<WindowInfo> windows = GetMatchingWindows(options, all);
        if (windows.Count == 0) {
            throw new InvalidOperationException("No matching windows were found.");
        }
        return windows;
    }

    private WindowInfo ResolveSingleWindow(WindowQueryOptions options) {
        return ResolveWindows(options, all: false)[0];
    }

    private WindowInfo ResolveParentWindow(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.ParentWindowHandle == IntPtr.Zero) {
            throw new InvalidOperationException("The control does not expose parent window metadata.");
        }

        List<WindowInfo> windows = _windowManager.GetWindows(new WindowQueryOptions {
            Handle = control.ParentWindowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        if (windows.Count == 0) {
            throw new InvalidOperationException("The parent window for the selected control could not be resolved.");
        }

        return windows[0];
    }

    private IReadOnlyList<WindowInfo> RefreshWindows(IReadOnlyList<WindowInfo> windows) {
        return windows.Select(RefreshWindow).ToArray();
    }

    private WindowInfo RefreshWindow(WindowInfo window) {
        List<WindowInfo> current = _windowManager.GetWindows(new WindowQueryOptions {
            ProcessId = (int)window.ProcessId,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        return current.FirstOrDefault(candidate => candidate.Handle == window.Handle) ?? window;
    }

    private IReadOnlyList<WindowInfo> GetMatchingWindows(WindowQueryOptions options, bool all) {
        List<WindowInfo> windows = _windowManager.GetWindows(options);
        if (windows.Count == 0) {
            return Array.Empty<WindowInfo>();
        }

        if (all) {
            return windows;
        }

        return new[] { windows[0] };
    }

    private IReadOnlyList<WindowInfo> GetWindowsByHandle(IEnumerable<IntPtr> handles, bool all) {
        var windows = new List<WindowInfo>();
        foreach (IntPtr handle in handles) {
            if (handle == IntPtr.Zero) {
                continue;
            }

            List<WindowInfo> matches = _windowManager.GetWindows(new WindowQueryOptions {
                Handle = handle,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            });
            if (matches.Count == 0) {
                continue;
            }

            windows.Add(matches[0]);
            if (!all) {
                break;
            }
        }

        return windows;
    }

    private IReadOnlyList<WindowControlTargetInfo> GetControls(IReadOnlyList<WindowInfo> windows, WindowControlQueryOptions? controlOptions, bool allControls) {
        if (windows == null || windows.Count == 0) {
            return Array.Empty<WindowControlTargetInfo>();
        }

        var results = new List<WindowControlTargetInfo>();
        foreach (WindowInfo window in windows) {
            List<WindowControlInfo> controls = _windowManager.GetControls(window, controlOptions);
            foreach (WindowControlInfo control in controls) {
                results.Add(new WindowControlTargetInfo {
                    Window = window,
                    Control = control
                });

                if (!allControls) {
                    return results;
                }
            }
        }

        return results;
    }

    private static void RememberWindowHandles(ISet<IntPtr> handles, IEnumerable<WindowInfo> windows) {
        if (handles == null) {
            throw new ArgumentNullException(nameof(handles));
        }

        handles.Clear();
        foreach (WindowInfo window in windows) {
            if (window != null && window.Handle != IntPtr.Zero) {
                handles.Add(window.Handle);
            }
        }
    }

    private IReadOnlyList<WindowControlTargetInfo> RequireControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, bool allWindows, bool allControls) {
        IReadOnlyList<WindowControlTargetInfo> controls = GetControls(windowOptions, controlOptions, allWindows, allControls);
        if (controls.Count == 0) {
            throw new InvalidOperationException("No matching controls were found.");
        }

        return controls;
    }

    private WindowInfo? TryGetPreferredWindowForProcess(int processId, string filePath) {
        List<WindowInfo> windows = _windowManager.GetWindows(new WindowQueryOptions {
            ProcessId = processId,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        WindowInfo? preferred = windows.FirstOrDefault(window => !string.IsNullOrWhiteSpace(window.Title))
            ?? windows.FirstOrDefault();
        if (preferred != null) {
            return preferred;
        }

        string processName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(processName)) {
            return null;
        }

        windows = _windowManager.GetWindows(new WindowQueryOptions {
            ProcessNamePattern = processName,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        return windows.FirstOrDefault(window => !string.IsNullOrWhiteSpace(window.Title))
            ?? windows.FirstOrDefault();
    }

    private WindowInfo? TryResolveLaunchedWindow(int launcherProcessId, IReadOnlyList<string> processNameHints, DateTime launchedAtUtc, ISet<IntPtr> preLaunchWindowHandles, string? windowTitlePattern, string? windowClassNamePattern, int waitForWindowMilliseconds, int waitForWindowIntervalMilliseconds) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        do {
            WindowInfo? candidate = FindBestLaunchedWindowCandidate(launcherProcessId, processNameHints, launchedAtUtc, preLaunchWindowHandles, windowTitlePattern, windowClassNamePattern);
            if (candidate != null) {
                return candidate;
            }

            if (waitForWindowMilliseconds == 0 || stopwatch.ElapsedMilliseconds >= waitForWindowMilliseconds) {
                return null;
            }

            Thread.Sleep(waitForWindowIntervalMilliseconds);
        } while (true);
    }

    private WindowInfo? FindBestLaunchedWindowCandidate(int launcherProcessId, IReadOnlyList<string> processNameHints, DateTime launchedAtUtc, ISet<IntPtr> preLaunchWindowHandles, string? windowTitlePattern, string? windowClassNamePattern) {
        List<LaunchWindowCandidate> candidates = new();

        foreach (WindowInfo window in _windowManager.GetWindows(new WindowQueryOptions {
            ProcessId = launcherProcessId,
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = false,
            IncludeEmptyTitles = false,
            IsVisible = true
        })) {
            candidates.Add(new LaunchWindowCandidate(
                window,
                TryGetProcessStartTimeUtc((int)window.ProcessId, out DateTime startedUtc) ? (DateTime?)startedUtc : null,
                true,
                !preLaunchWindowHandles.Contains(window.Handle)));
        }

        for (int hintIndex = 0; hintIndex < processNameHints.Count; hintIndex++) {
            string resolvedProcessNameHint = processNameHints[hintIndex];
            foreach (WindowInfo window in _windowManager.GetWindows(new WindowQueryOptions {
                ProcessNamePattern = resolvedProcessNameHint,
                IncludeHidden = false,
                IncludeCloaked = false,
                IncludeOwned = false,
                IncludeEmptyTitles = false,
                IsVisible = true
            })) {
                if (candidates.Any(candidate => candidate.Window.Handle == window.Handle)) {
                    continue;
                }

                DateTime? startedUtc = TryGetProcessStartTimeUtc((int)window.ProcessId, out DateTime processStartedUtc) ? processStartedUtc : null;
                bool newHandleAfterLaunch = !preLaunchWindowHandles.Contains(window.Handle);
                if (!newHandleAfterLaunch && startedUtc.HasValue && startedUtc.Value < launchedAtUtc.AddSeconds(-2)) {
                    continue;
                }

                candidates.Add(new LaunchWindowCandidate(window, startedUtc, false, newHandleAfterLaunch, hintIndex));
            }
        }

        return candidates
            .Where(candidate => MatchesLaunchedWindow(candidate.Window, windowTitlePattern, windowClassNamePattern))
            .OrderByDescending(candidate => candidate.ExactProcessMatch)
            .ThenBy(candidate => candidate.HintPriority)
            .ThenByDescending(candidate => candidate.NewHandleAfterLaunch)
            .ThenByDescending(candidate => !string.IsNullOrWhiteSpace(candidate.Window.Title))
            .ThenByDescending(candidate => candidate.ProcessStartedUtc ?? DateTime.MinValue)
            .Select(candidate => candidate.Window)
            .FirstOrDefault();
    }

    private bool MatchesLaunchedWindow(WindowInfo window, string? windowTitlePattern, string? windowClassNamePattern) {
        if (!string.IsNullOrWhiteSpace(windowTitlePattern) && !MatchesPattern(window.Title ?? string.Empty, windowTitlePattern!)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(windowClassNamePattern) && !MatchesPattern(GetWindowClassName(window.Handle), windowClassNamePattern!)) {
            return false;
        }

        return true;
    }

    private HashSet<IntPtr> CaptureWindowHandlesForProcesses(IReadOnlyList<string> processNameHints) {
        if (processNameHints.Count == 0) {
            return new HashSet<IntPtr>();
        }

        HashSet<IntPtr> handles = new();
        foreach (string processNameHint in processNameHints) {
            foreach (WindowInfo window in _windowManager.GetWindows(new WindowQueryOptions {
                ProcessNamePattern = processNameHint,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            })) {
                handles.Add(window.Handle);
            }
        }

        return handles;
    }

    private static string? GetProcessNameHint(string filePath) {
        string executableName = Path.GetFileNameWithoutExtension(filePath.Trim().Trim('"'));
        return string.IsNullOrWhiteSpace(executableName) ? null : executableName;
    }

    private static string? GetProcessNameHint(Process process, string filePath) {
        try {
            if (!process.HasExited && !string.IsNullOrWhiteSpace(process.ProcessName)) {
                return process.ProcessName;
            }
        } catch {
        }

        return GetProcessNameHint(filePath);
    }

    private static IReadOnlyList<string> CollectProcessNameHints(params string?[] values) {
        List<string> hints = new();
        foreach (string? value in values) {
            if (string.IsNullOrWhiteSpace(value)) {
                continue;
            }

            if (hints.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            hints.Add(value!);
        }

        return hints;
    }

    private static string BuildMissingLaunchedWindowMessage(IReadOnlyList<string> processNameHints, string? windowTitlePattern, string? windowClassNamePattern, int waitForWindowMilliseconds) {
        string processText = processNameHints.Count == 0 ? "the launched application" : string.Join(", ", processNameHints);
        if (!string.IsNullOrWhiteSpace(windowTitlePattern) || !string.IsNullOrWhiteSpace(windowClassNamePattern)) {
            return $"Timed out after {waitForWindowMilliseconds}ms waiting for a launched window that matched process '{processText}', title '{windowTitlePattern ?? "*"}', and class '{windowClassNamePattern ?? "*"}'.";
        }

        return $"Timed out after {waitForWindowMilliseconds}ms waiting for a launched window for '{processText}'.";
    }

    private static string GetWindowClassName(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new(256);
        MonitorNativeMethods.GetClassName(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static bool MatchesPattern(string text, string pattern) {
        if (string.IsNullOrEmpty(pattern)) {
            return false;
        }

        if (pattern.Contains('*') || pattern.Contains('?')) {
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }

        return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryGetProcessStartTimeUtc(int processId, out DateTime startTimeUtc) {
        try {
            using Process process = Process.GetProcessById(processId);
            startTimeUtc = process.StartTime.ToUniversalTime();
            return true;
        } catch {
            startTimeUtc = default;
            return false;
        }
    }

    private IReadOnlyList<DesktopResolvedWindowTarget> ResolveWindowTargets(WindowQueryOptions options, string targetName, DesktopWindowTargetDefinition definition, bool all) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        return windows
            .Select(window => ResolveWindowTarget(targetName, definition, DescribeWindowGeometry(window)))
            .ToArray();
    }

    private static DesktopResolvedWindowTarget ResolveWindowTarget(string targetName, DesktopWindowTargetDefinition definition, DesktopWindowGeometry geometry) {
        (int relativeX, int relativeY) = ResolveRelativePoint(geometry, definition.X, definition.Y, definition.XRatio, definition.YRatio, definition.ClientArea);
        int? relativeWidth = ResolveOptionalAxisSize(geometry, definition.Width, definition.WidthRatio, definition.ClientArea, horizontal: true, nameof(definition.Width), nameof(definition.WidthRatio));
        int? relativeHeight = ResolveOptionalAxisSize(geometry, definition.Height, definition.HeightRatio, definition.ClientArea, horizontal: false, nameof(definition.Height), nameof(definition.HeightRatio));
        int screenX = definition.ClientArea ? geometry.ClientLeft + relativeX : geometry.WindowLeft + relativeX;
        int screenY = definition.ClientArea ? geometry.ClientTop + relativeY : geometry.WindowTop + relativeY;

        return new DesktopResolvedWindowTarget {
            Name = targetName,
            Definition = new DesktopWindowTargetDefinition {
                Description = definition.Description,
                X = definition.X,
                Y = definition.Y,
                XRatio = definition.XRatio,
                YRatio = definition.YRatio,
                Width = definition.Width,
                Height = definition.Height,
                WidthRatio = definition.WidthRatio,
                HeightRatio = definition.HeightRatio,
                ClientArea = definition.ClientArea
            },
            Geometry = geometry,
            RelativeX = relativeX,
            RelativeY = relativeY,
            RelativeWidth = relativeWidth,
            RelativeHeight = relativeHeight,
            ScreenX = screenX,
            ScreenY = screenY,
            ScreenWidth = relativeWidth,
            ScreenHeight = relativeHeight
        };
    }

    private static DesktopControlTargetDefinition CloneControlTargetDefinition(DesktopControlTargetDefinition definition) {
        return new DesktopControlTargetDefinition {
            Description = definition.Description,
            ClassNamePattern = definition.ClassNamePattern,
            TextPattern = definition.TextPattern,
            ValuePattern = definition.ValuePattern,
            Id = definition.Id,
            Handle = definition.Handle,
            AutomationIdPattern = definition.AutomationIdPattern,
            ControlTypePattern = definition.ControlTypePattern,
            FrameworkIdPattern = definition.FrameworkIdPattern,
            IsEnabled = definition.IsEnabled,
            IsKeyboardFocusable = definition.IsKeyboardFocusable,
            SupportsBackgroundClick = definition.SupportsBackgroundClick,
            SupportsBackgroundText = definition.SupportsBackgroundText,
            SupportsBackgroundKeys = definition.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = definition.SupportsForegroundInputFallback,
            UseUiAutomation = definition.UseUiAutomation,
            IncludeUiAutomation = definition.IncludeUiAutomation,
            EnsureForegroundWindow = definition.EnsureForegroundWindow
        };
    }

    private static void ValidateWindowTargetDefinition(DesktopWindowTargetDefinition definition) {
        ValidateTargetAxis(definition.X, definition.XRatio, nameof(definition.X), nameof(definition.XRatio));
        ValidateTargetAxis(definition.Y, definition.YRatio, nameof(definition.Y), nameof(definition.YRatio));
        ValidateOptionalTargetSizeAxis(definition.Width, definition.WidthRatio, nameof(definition.Width), nameof(definition.WidthRatio));
        ValidateOptionalTargetSizeAxis(definition.Height, definition.HeightRatio, nameof(definition.Height), nameof(definition.HeightRatio));
    }

    private static void ValidateControlTargetDefinition(DesktopControlTargetDefinition definition) {
        WindowControlQueryOptions query = CreateControlQuery(definition);
        bool hasSelector =
            query.Id.HasValue ||
            query.Handle.HasValue ||
            !IsWildcard(query.ClassNamePattern) ||
            !IsWildcard(query.TextPattern) ||
            !IsWildcard(query.ValuePattern) ||
            !IsWildcard(query.AutomationIdPattern) ||
            !IsWildcard(query.ControlTypePattern) ||
            !IsWildcard(query.FrameworkIdPattern) ||
            query.IsEnabled.HasValue ||
            query.IsKeyboardFocusable.HasValue ||
            query.SupportsBackgroundClick.HasValue ||
            query.SupportsBackgroundText.HasValue ||
            query.SupportsBackgroundKeys.HasValue ||
            query.SupportsForegroundInputFallback.HasValue;
        if (!hasSelector) {
            throw new ArgumentException("A control target must include at least one selector or capability requirement.", nameof(definition));
        }
    }

    private sealed class LaunchWindowCandidate {
        public LaunchWindowCandidate(WindowInfo window, DateTime? processStartedUtc, bool exactProcessMatch, bool newHandleAfterLaunch, int hintPriority = int.MaxValue) {
            Window = window;
            ProcessStartedUtc = processStartedUtc;
            ExactProcessMatch = exactProcessMatch;
            NewHandleAfterLaunch = newHandleAfterLaunch;
            HintPriority = hintPriority;
        }

        public WindowInfo Window { get; }
        public DateTime? ProcessStartedUtc { get; }
        public bool ExactProcessMatch { get; }
        public bool NewHandleAfterLaunch { get; }
        public int HintPriority { get; }
    }

    private static void ValidateTargetAxis(int? coordinate, double? ratio, string coordinateName, string ratioName) {
        bool hasCoordinate = coordinate.HasValue;
        bool hasRatio = ratio.HasValue;
        if (hasCoordinate == hasRatio) {
            throw new ArgumentException($"Provide either {coordinateName} or {ratioName}, but not both.");
        }

        if (hasCoordinate) {
            if (coordinate!.Value < 0) {
                throw new ArgumentOutOfRangeException(coordinateName, $"{coordinateName} must be zero or greater.");
            }

            return;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue < 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be between 0 and 1.");
        }
    }

    private static void ValidateOptionalTargetSizeAxis(int? size, double? ratio, string sizeName, string ratioName) {
        bool hasSize = size.HasValue;
        bool hasRatio = ratio.HasValue;
        if (!hasSize && !hasRatio) {
            return;
        }

        if (hasSize == hasRatio) {
            throw new ArgumentException($"Provide either {sizeName} or {ratioName}, but not both.");
        }

        if (hasSize) {
            if (size!.Value <= 0) {
                throw new ArgumentOutOfRangeException(sizeName, $"{sizeName} must be greater than zero.");
            }

            return;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue <= 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be greater than 0 and less than or equal to 1.");
        }
    }

    private static WindowControlQueryOptions CreateControlQuery(DesktopControlTargetDefinition definition) {
        return new WindowControlQueryOptions {
            ClassNamePattern = string.IsNullOrWhiteSpace(definition.ClassNamePattern) ? "*" : definition.ClassNamePattern,
            TextPattern = string.IsNullOrWhiteSpace(definition.TextPattern) ? "*" : definition.TextPattern,
            ValuePattern = string.IsNullOrWhiteSpace(definition.ValuePattern) ? "*" : definition.ValuePattern,
            Id = definition.Id,
            Handle = string.IsNullOrWhiteSpace(definition.Handle) ? null : DesktopHandleParser.Parse(definition.Handle!),
            AutomationIdPattern = string.IsNullOrWhiteSpace(definition.AutomationIdPattern) ? "*" : definition.AutomationIdPattern,
            ControlTypePattern = string.IsNullOrWhiteSpace(definition.ControlTypePattern) ? "*" : definition.ControlTypePattern,
            FrameworkIdPattern = string.IsNullOrWhiteSpace(definition.FrameworkIdPattern) ? "*" : definition.FrameworkIdPattern,
            IsEnabled = definition.IsEnabled,
            IsKeyboardFocusable = definition.IsKeyboardFocusable,
            SupportsBackgroundClick = definition.SupportsBackgroundClick,
            SupportsBackgroundText = definition.SupportsBackgroundText,
            SupportsBackgroundKeys = definition.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = definition.SupportsForegroundInputFallback,
            UseUiAutomation = definition.UseUiAutomation,
            IncludeUiAutomation = definition.IncludeUiAutomation,
            EnsureForegroundWindow = definition.EnsureForegroundWindow
        };
    }

    private static bool IsWildcard(string? value) {
        return string.IsNullOrWhiteSpace(value) || value == "*";
    }

    private static (int X, int Y) ResolveWindowPoint(WindowInfo window, int? x, int? y, double? xRatio, double? yRatio, bool clientArea) {
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        (int resolvedX, int resolvedY) = ResolveRelativePoint(geometry, x, y, xRatio, yRatio, clientArea);

        if (!clientArea) {
            return (geometry.WindowLeft + resolvedX, geometry.WindowTop + resolvedY);
        }

        return (geometry.ClientLeft + resolvedX, geometry.ClientTop + resolvedY);
    }

    private static DesktopWindowGeometry DescribeWindowGeometry(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        int clientLeft = window.Left;
        int clientTop = window.Top;
        int clientWidth = window.Width;
        int clientHeight = window.Height;

        if (MonitorNativeMethods.GetClientRect(window.Handle, out RECT clientRect)) {
            var clientOrigin = new MonitorNativeMethods.POINT {
                x = 0,
                y = 0
            };
            if (MonitorNativeMethods.ClientToScreen(window.Handle, ref clientOrigin)) {
                clientLeft = clientOrigin.x;
                clientTop = clientOrigin.y;
                clientWidth = Math.Max(0, clientRect.Right - clientRect.Left);
                clientHeight = Math.Max(0, clientRect.Bottom - clientRect.Top);
            }
        }

        return new DesktopWindowGeometry {
            Window = window,
            WindowLeft = window.Left,
            WindowTop = window.Top,
            WindowWidth = window.Width,
            WindowHeight = window.Height,
            ClientLeft = clientLeft,
            ClientTop = clientTop,
            ClientWidth = clientWidth,
            ClientHeight = clientHeight,
            ClientOffsetLeft = clientLeft - window.Left,
            ClientOffsetTop = clientTop - window.Top
        };
    }

    private static (int X, int Y) ResolveRelativePoint(DesktopWindowGeometry geometry, int? x, int? y, double? xRatio, double? yRatio, bool clientArea) {
        int width = clientArea ? geometry.ClientWidth : geometry.WindowWidth;
        int height = clientArea ? geometry.ClientHeight : geometry.WindowHeight;

        return (ResolveAxisCoordinate(x, xRatio, width, nameof(x), nameof(xRatio)), ResolveAxisCoordinate(y, yRatio, height, nameof(y), nameof(yRatio)));
    }

    private static int? ResolveOptionalAxisSize(DesktopWindowGeometry geometry, int? size, double? ratio, bool clientArea, bool horizontal, string sizeName, string ratioName) {
        bool hasSize = size.HasValue;
        bool hasRatio = ratio.HasValue;
        if (!hasSize && !hasRatio) {
            return null;
        }

        int boundsSize = horizontal
            ? clientArea ? geometry.ClientWidth : geometry.WindowWidth
            : clientArea ? geometry.ClientHeight : geometry.WindowHeight;
        if (hasSize) {
            if (size!.Value <= 0) {
                throw new ArgumentOutOfRangeException(sizeName, $"{sizeName} must be greater than zero.");
            }

            return size.Value;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue <= 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be greater than 0 and less than or equal to 1.");
        }

        if (boundsSize <= 0) {
            throw new InvalidOperationException("The target bounds do not expose a usable size.");
        }

        return Math.Max(1, (int)Math.Round(boundsSize * ratioValue, MidpointRounding.AwayFromZero));
    }

    private static int ResolveAxisCoordinate(int? coordinate, double? ratio, int size, string coordinateName, string ratioName) {
        bool hasCoordinate = coordinate.HasValue;
        bool hasRatio = ratio.HasValue;
        if (hasCoordinate == hasRatio) {
            throw new ArgumentException($"Provide either {coordinateName} or {ratioName}, but not both.");
        }

        if (hasCoordinate) {
            if (coordinate!.Value < 0) {
                throw new ArgumentOutOfRangeException(coordinateName, $"{coordinateName} must be zero or greater.");
            }

            return coordinate.Value;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue < 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be between 0 and 1.");
        }

        if (size <= 0) {
            throw new InvalidOperationException("The target bounds do not expose a usable size.");
        }

        return (int)Math.Round((size - 1) * ratioValue, MidpointRounding.AwayFromZero);
    }
}
