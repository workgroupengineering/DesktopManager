using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace DesktopManager.Cli;

internal static class DesktopOperations {
    public static IReadOnlyList<WindowResult> ListWindows(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => new DesktopAutomationService().GetWindows(CreateWindowQuery(criteria)).Select(MapWindow).ToArray());
    }

    public static WindowResult GetActiveWindow() {
        return ExecuteCore(() => {
            WindowInfo? window = new DesktopAutomationService().GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            if (window == null) {
                throw new InvalidOperationException("The active window could not be resolved.");
            }

            return MapWindow(window);
        });
    }

    public static WindowChangeResult MoveWindow(WindowSelectionCriteria criteria, int? monitorIndex, int? x, int? y, int? width, int? height, bool activate) {
        return ExecuteCore(() => BuildWindowChangeResult(
            "move",
            new DesktopAutomationService().MoveWindows(CreateWindowQuery(criteria), monitorIndex, x, y, width, height, activate, criteria.All)));
    }

    public static WindowChangeResult FocusWindow(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => BuildWindowChangeResult(
            "focus",
            new DesktopAutomationService().FocusWindows(CreateWindowQuery(criteria), criteria.All)));
    }

    public static WindowChangeResult MinimizeWindows(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => BuildWindowChangeResult(
            "minimize",
            new DesktopAutomationService().MinimizeWindows(CreateWindowQuery(criteria), criteria.All)));
    }

    public static WindowChangeResult SnapWindow(WindowSelectionCriteria criteria, string position) {
        if (!TryParseSnapPosition(position, out SnapPosition snapPosition)) {
            throw new CommandLineException($"Unsupported snap position '{position}'.");
        }

        return ExecuteCore(() => BuildWindowChangeResult(
            "snap",
            new DesktopAutomationService().SnapWindows(CreateWindowQuery(criteria), snapPosition, criteria.All)));
    }

    public static WindowChangeResult TypeWindowText(WindowSelectionCriteria criteria, string text, bool paste, int delayMilliseconds) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteCore(() => BuildWindowChangeResult(
            paste ? "paste-text" : "type-text",
            new DesktopAutomationService().TypeWindowText(CreateWindowQuery(criteria), text, paste, delayMilliseconds, criteria.All)));
    }

    public static IReadOnlyList<MonitorResult> ListMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null) {
        return ExecuteCore(() => new DesktopAutomationService().GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index)
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
            .ToArray());
    }

    public static IReadOnlyList<ControlResult> ListControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, allControls: true)
            .Select(MapControl)
            .ToArray());
    }

    public static ControlAssertionResult ControlExists(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows) {
        return ExecuteCore(() => {
            IReadOnlyList<ControlResult> controls = new DesktopAutomationService()
                .GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, allControls: true)
                .Select(MapControl)
                .ToArray();
            return new ControlAssertionResult {
                Assertion = "control-exists",
                Matched = controls.Count > 0,
                Count = controls.Count,
                Controls = controls
            };
        });
    }

    public static IReadOnlyList<ControlDiagnosticResult> DiagnoseControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows, int sampleLimit) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlDiagnostics(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, sampleLimit)
            .Select(MapControlDiagnostics)
            .ToArray());
    }

    public static ControlActionResult ClickControl(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string button, bool allWindows) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteCore(() => BuildControlActionResult(
            "click-control",
            new DesktopAutomationService().ClickControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), mouseButton, allWindows, controlCriteria.All)));
    }

    public static ControlActionResult SetControlText(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string text, bool allWindows) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteCore(() => BuildControlActionResult(
            "set-control-text",
            new DesktopAutomationService().SetControlText(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), text, allWindows, controlCriteria.All)));
    }

    public static ControlActionResult SendControlKeys(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, IReadOnlyList<string> keys, bool allWindows) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteCore(() => BuildControlActionResult(
            "send-control-keys",
            new DesktopAutomationService().SendControlKeys(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), virtualKeys, allWindows, controlCriteria.All)));
    }

    public static WaitForControlResult WaitForControl(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows) {
        return ExecuteCore(() => {
            DesktopControlWaitResult result = new DesktopAutomationService().WaitForControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), timeoutMilliseconds, intervalMilliseconds, allWindows, controlCriteria.All);
            return new WaitForControlResult {
                ElapsedMilliseconds = result.ElapsedMilliseconds,
                Count = result.Controls.Count,
                Controls = result.Controls.Select(MapControl).ToArray()
            };
        });
    }

    public static NamedStateResult SaveLayout(string name) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetLayoutPath(name);
            new DesktopAutomationService().SaveLayout(path);
            return new NamedStateResult {
                Action = "save",
                Name = name,
                Path = path
            };
        });
    }

    public static NamedStateResult ApplyLayout(string name, bool validate) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetLayoutPath(name);
            if (!File.Exists(path)) {
                throw new InvalidOperationException($"Named layout '{name}' was not found.");
            }

            new DesktopAutomationService().LoadLayout(path, validate);
            return new NamedStateResult {
                Action = "apply",
                Name = name,
                Path = path
            };
        });
    }

    public static IReadOnlyList<string> ListLayouts() {
        return ExecuteCore(() => DesktopStateStore.ListNames("layouts"));
    }

    public static NamedStateResult SaveSnapshot(string name) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetSnapshotPath(name);
            new DesktopAutomationService().SaveLayout(path);
            return new NamedStateResult {
                Action = "save",
                Name = name,
                Path = path,
                Scope = "windows-only"
            };
        });
    }

    public static NamedStateResult RestoreSnapshot(string name, bool validate) {
        return ExecuteCore(() => {
            string path = DesktopStateStore.GetSnapshotPath(name);
            if (!File.Exists(path)) {
                throw new InvalidOperationException($"Named snapshot '{name}' was not found.");
            }

            new DesktopAutomationService().LoadLayout(path, validate);
            return new NamedStateResult {
                Action = "restore",
                Name = name,
                Path = path,
                Scope = "windows-only"
            };
        });
    }

    public static IReadOnlyList<string> ListSnapshots() {
        return ExecuteCore(() => DesktopStateStore.ListNames("snapshots"));
    }

    public static object GetCurrentSnapshotSummary() {
        return new {
            ActiveWindow = SafeGetActiveWindow(),
            Monitors = ListMonitors(connectedOnly: true),
            Windows = ListWindows(new WindowSelectionCriteria())
        };
    }

    public static ScreenshotResult CaptureDesktopScreenshot(int? monitorIndex, string? deviceId, string? deviceName, int? left, int? top, int? width, int? height, string? outputPath) {
        return ExecuteCore(() => {
            bool hasRegionSelector = left.HasValue || top.HasValue || width.HasValue || height.HasValue;
            if (hasRegionSelector && (!left.HasValue || !top.HasValue || !width.HasValue || !height.HasValue)) {
                throw new ArgumentException("Region capture requires --left, --top, --width, and --height.");
            }

            using DesktopCapture capture = hasRegionSelector
                ? new DesktopAutomationService().CaptureRegion(left!.Value, top!.Value, width!.Value, height!.Value)
                : monitorIndex.HasValue || !string.IsNullOrWhiteSpace(deviceId) || !string.IsNullOrWhiteSpace(deviceName)
                    ? new DesktopAutomationService().CaptureMonitor(monitorIndex, deviceId, deviceName)
                    : new DesktopAutomationService().CaptureDesktop();

            string prefix = capture.Kind == "monitor" && capture.MonitorIndex.HasValue ? $"monitor-{capture.MonitorIndex.Value}" : capture.Kind;
            return SaveScreenshot(capture, prefix, outputPath);
        });
    }

    public static ScreenshotResult CaptureWindowScreenshot(WindowSelectionCriteria criteria, string? outputPath) {
        return ExecuteCore(() => {
            using DesktopCapture capture = new DesktopAutomationService().CaptureWindow(CreateWindowQuery(criteria));
            string prefix = capture.Window == null ? "window" : $"window-{capture.Window.ProcessId}";
            return SaveScreenshot(capture, prefix, outputPath);
        });
    }

    public static ProcessLaunchResult LaunchProcess(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds) {
        return ExecuteCore(() => {
            DesktopProcessLaunchInfo result = new DesktopAutomationService().LaunchProcess(new DesktopProcessStartOptions {
                FilePath = filePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                WaitForInputIdleMilliseconds = waitForInputIdleMilliseconds
            });

            return new ProcessLaunchResult {
                FilePath = result.FilePath,
                Arguments = result.Arguments,
                WorkingDirectory = result.WorkingDirectory,
                ProcessId = result.ProcessId,
                HasExited = result.HasExited,
                MainWindow = result.MainWindow == null ? null : MapWindow(result.MainWindow)
            };
        });
    }

    public static WaitForWindowResult WaitForWindow(WindowSelectionCriteria criteria, int timeoutMilliseconds, int intervalMilliseconds) {
        return ExecuteCore(() => {
            DesktopWindowWaitResult result = new DesktopAutomationService().WaitForWindows(CreateWindowQuery(criteria), timeoutMilliseconds, intervalMilliseconds, criteria.All);
            return new WaitForWindowResult {
                ElapsedMilliseconds = result.ElapsedMilliseconds,
                Count = result.Windows.Count,
                Windows = result.Windows.Select(MapWindow).ToArray()
            };
        });
    }

    public static WindowAssertionResult WindowExists(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => {
            WindowQueryOptions query = CreateWindowQuery(criteria);
            DesktopAutomationService automation = new DesktopAutomationService();
            IReadOnlyList<WindowResult> windows = automation.GetWindows(query).Select(MapWindow).ToArray();
            return new WindowAssertionResult {
                Assertion = "window-exists",
                Matched = automation.WindowExists(query),
                Count = windows.Count,
                Windows = windows
            };
        });
    }

    public static WindowAssertionResult ActiveWindowMatches(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => {
            WindowQueryOptions query = CreateWindowQuery(criteria);
            DesktopAutomationService automation = new DesktopAutomationService();
            WindowInfo? activeWindow = automation.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            bool matched = automation.ActiveWindowMatches(query);
            IReadOnlyList<WindowResult> windows = matched
                ? automation.GetWindows(new WindowQueryOptions {
                    TitlePattern = query.TitlePattern,
                    ProcessNamePattern = query.ProcessNamePattern,
                    ClassNamePattern = query.ClassNamePattern,
                    TitleRegex = query.TitleRegex,
                    ProcessId = query.ProcessId,
                    Handle = query.Handle,
                    ActiveWindow = true,
                    IncludeEmptyTitles = query.IncludeEmptyTitles ?? true,
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IsVisible = query.IsVisible,
                    State = query.State,
                    IsTopMost = query.IsTopMost,
                    ZOrderMin = query.ZOrderMin,
                    ZOrderMax = query.ZOrderMax
                }).Select(MapWindow).ToArray()
                : Array.Empty<WindowResult>();

            return new WindowAssertionResult {
                Assertion = "active-window-matches",
                Matched = matched,
                Count = windows.Count,
                Windows = windows,
                ActiveWindow = activeWindow == null ? null : MapWindow(activeWindow)
            };
        });
    }

    private static WindowResult? SafeGetActiveWindow() {
        try {
            return GetActiveWindow();
        } catch {
            return null;
        }
    }

    private static WindowChangeResult BuildWindowChangeResult(string action, IReadOnlyList<WindowInfo> windows) {
        return new WindowChangeResult {
            Action = action,
            Count = windows.Count,
            Windows = windows.Select(MapWindow).ToArray()
        };
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
        return MapControl(target.Window, target.Control);
    }

    private static ControlResult MapControl(WindowInfo window, WindowControlInfo control) {
        return new ControlResult {
            Handle = $"0x{control.Handle.ToInt64():X}",
            ClassName = control.ClassName,
            Id = control.Id,
            Text = !string.IsNullOrWhiteSpace(control.Text) ? control.Text : control.Handle != IntPtr.Zero ? WindowTextHelper.GetWindowText(control.Handle) : string.Empty,
            Value = !string.IsNullOrWhiteSpace(control.Value) ? control.Value : !string.IsNullOrWhiteSpace(control.Text) ? control.Text : control.Handle != IntPtr.Zero ? WindowTextHelper.GetWindowText(control.Handle) : string.Empty,
            Source = control.Source.ToString(),
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsEnabled = control.IsEnabled,
            ParentWindow = MapWindow(window)
        };
    }

    private static ControlDiagnosticResult MapControlDiagnostics(DesktopControlDiscoveryDiagnostics diagnostics) {
        return new ControlDiagnosticResult {
            Window = MapWindow(diagnostics.Window),
            RequiresUiAutomation = diagnostics.RequiresUiAutomation,
            UseUiAutomation = diagnostics.UseUiAutomation,
            IncludeUiAutomation = diagnostics.IncludeUiAutomation,
            EnsureForegroundWindow = diagnostics.EnsureForegroundWindow,
            UiAutomationAvailable = diagnostics.UiAutomationAvailable,
            PreparationAttempted = diagnostics.PreparationAttempted,
            PreparationSucceeded = diagnostics.PreparationSucceeded,
            EffectiveSource = diagnostics.EffectiveSource,
            Win32ControlCount = diagnostics.Win32ControlCount,
            UiAutomationControlCount = diagnostics.UiAutomationControlCount,
            EffectiveControlCount = diagnostics.EffectiveControlCount,
            MatchedControlCount = diagnostics.MatchedControlCount,
            SampleControls = diagnostics.SampleControls.Select(control => MapControl(diagnostics.Window, control)).ToArray()
        };
    }

    private static ControlActionResult BuildControlActionResult(string action, IReadOnlyList<WindowControlTargetInfo> controls) {
        return new ControlActionResult {
            Action = action,
            Count = controls.Count,
            Controls = controls.Select(MapControl).ToArray()
        };
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

    private static ScreenshotResult SaveScreenshot(DesktopCapture capture, string prefix, string? outputPath) {
        string path = DesktopStateStore.ResolveCapturePath(prefix, outputPath);
        capture.Bitmap.Save(path, ImageFormat.Png);
        return new ScreenshotResult {
            Kind = capture.Kind,
            Path = path,
            Width = capture.Bitmap.Width,
            Height = capture.Bitmap.Height,
            MonitorIndex = capture.MonitorIndex,
            MonitorDeviceName = capture.MonitorDeviceName,
            Window = capture.Window == null ? null : MapWindow(capture.Window)
        };
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
            Handle = string.IsNullOrWhiteSpace(criteria.Handle) ? null : DesktopHandleParser.Parse(criteria.Handle),
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
            Handle = string.IsNullOrWhiteSpace(criteria.Handle) ? null : DesktopHandleParser.Parse(criteria.Handle),
            AutomationIdPattern = criteria.AutomationIdPattern,
            ControlTypePattern = criteria.ControlTypePattern,
            FrameworkIdPattern = criteria.FrameworkIdPattern,
            ValuePattern = criteria.ValuePattern,
            IsEnabled = criteria.IsEnabled,
            IsKeyboardFocusable = criteria.IsKeyboardFocusable,
            EnsureForegroundWindow = criteria.EnsureForegroundWindow,
            UseUiAutomation = criteria.UiAutomation,
            IncludeUiAutomation = criteria.IncludeUiAutomation
        };
    }

    private static T ExecuteCore<T>(Func<T> operation) {
        try {
            return operation();
        } catch (ArgumentOutOfRangeException ex) {
            throw new CommandLineException(ex.Message);
        } catch (ArgumentException ex) {
            throw new CommandLineException(ex.Message);
        } catch (DirectoryNotFoundException ex) {
            throw new CommandLineException(ex.Message);
        } catch (FileNotFoundException ex) {
            throw new CommandLineException(ex.Message);
        } catch (InvalidOperationException ex) {
            throw new CommandLineException(ex.Message);
        } catch (TimeoutException ex) {
            throw new CommandLineException(ex.Message);
        }
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
