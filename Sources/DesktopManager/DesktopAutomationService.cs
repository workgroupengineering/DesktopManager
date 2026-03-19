using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides higher-level desktop automation orchestration on top of WindowManager and related services.
/// </summary>
public sealed class DesktopAutomationService {
    private readonly WindowManager _windowManager;
    private readonly Monitors _monitors;

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
    /// Gets matching controls for one or more windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> GetControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false, bool allControls = true) {
        IReadOnlyList<WindowControlTargetInfo> controls = _windowManager.GetControls(windowOptions, controlOptions, allWindows);
        if (controls.Count == 0) {
            return controls;
        }

        if (allControls) {
            return controls;
        }

        return new[] { controls[0] };
    }

    /// <summary>
    /// Clicks matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> ClickControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, MouseButton button, bool allWindows = false, bool allControls = false) {
        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            _windowManager.ClickControl(control.Control, button);
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
            MonitorNativeMethods.SendMessage(control.Control.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text);
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
            KeyboardInputService.SendToControl(control.Control, keys.ToArray());
        }

        return controls;
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

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process '{options.FilePath}'.");

        if (options.WaitForInputIdleMilliseconds.HasValue && options.WaitForInputIdleMilliseconds.Value > 0) {
            try {
                process.WaitForInputIdle(options.WaitForInputIdleMilliseconds.Value);
            } catch (InvalidOperationException) {
            }
        }

        process.Refresh();
        Thread.Sleep(200);

        return new DesktopProcessLaunchInfo {
            FilePath = options.FilePath,
            Arguments = options.Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? null : Path.GetFullPath(options.WorkingDirectory),
            ProcessId = process.Id,
            HasExited = process.HasExited,
            MainWindow = TryGetPreferredWindowForProcess(process.Id, options.FilePath)
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
            Window = window
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
        List<WindowInfo> windows = _windowManager.GetWindows(options);
        if (windows.Count == 0) {
            throw new InvalidOperationException("No matching windows were found.");
        }

        if (all) {
            return windows;
        }

        return new[] { windows[0] };
    }

    private WindowInfo ResolveSingleWindow(WindowQueryOptions options) {
        return ResolveWindows(options, all: false)[0];
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
}
