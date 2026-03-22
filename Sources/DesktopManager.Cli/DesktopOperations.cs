using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace DesktopManager.Cli;

internal static partial class DesktopOperations {
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

    public static IReadOnlyList<WindowGeometryResult> GetWindowGeometry(WindowSelectionCriteria criteria) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetWindowGeometry(CreateWindowQuery(criteria), criteria.All)
            .Select(MapWindowGeometry)
            .ToArray());
    }

    public static WindowChangeResult MoveWindow(WindowSelectionCriteria criteria, int? monitorIndex, int? x, int? y, int? width, int? height, bool activate, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "move",
            criteria,
            activate ? "window-management-activate" : "window-management",
            artifactOptions,
            automation => automation.MoveWindows(CreateWindowQuery(criteria), monitorIndex, x, y, width, height, activate, criteria.All));
    }

    public static WindowChangeResult FocusWindow(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "focus",
            criteria,
            "window-management-activate",
            artifactOptions,
            automation => automation.FocusWindows(CreateWindowQuery(criteria), criteria.All));
    }

    public static WindowChangeResult ClickWindowPoint(WindowSelectionCriteria criteria, int? x, int? y, double? xRatio, double? yRatio, string button, bool activate, bool clientArea, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "click-point",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ClickWindowPoint(CreateWindowQuery(criteria), x, y, xRatio, yRatio, mouseButton, activate, clientArea, criteria.All));
    }

    public static WindowChangeResult ClickWindowTarget(WindowSelectionCriteria criteria, string targetName, string button, bool activate, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "click-target",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ClickWindowTarget(CreateWindowQuery(criteria), targetName, mouseButton, activate, criteria.All),
            targetName,
            "window-target");
    }

    public static WindowChangeResult DragWindowPoints(WindowSelectionCriteria criteria, int? startX, int? startY, double? startXRatio, double? startYRatio, int? endX, int? endY, double? endXRatio, double? endYRatio, string button, int stepDelayMilliseconds, bool activate, bool clientArea, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "drag-point",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.DragWindowPoints(CreateWindowQuery(criteria), startX, startY, startXRatio, startYRatio, endX, endY, endXRatio, endYRatio, mouseButton, stepDelayMilliseconds, activate, clientArea, criteria.All));
    }

    public static WindowChangeResult DragWindowTargets(WindowSelectionCriteria criteria, string startTargetName, string endTargetName, string button, int stepDelayMilliseconds, bool activate, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteWindowMutation(
            "drag-target",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.DragWindowTargets(CreateWindowQuery(criteria), startTargetName, endTargetName, mouseButton, stepDelayMilliseconds, activate, criteria.All),
            $"{startTargetName}->{endTargetName}",
            "window-target-pair");
    }

    public static WindowChangeResult ScrollWindowPoint(WindowSelectionCriteria criteria, int? x, int? y, double? xRatio, double? yRatio, int delta, bool activate, bool clientArea, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "scroll-point",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ScrollWindowPoint(CreateWindowQuery(criteria), x, y, xRatio, yRatio, delta, activate, clientArea, criteria.All));
    }

    public static WindowChangeResult ScrollWindowTarget(WindowSelectionCriteria criteria, string targetName, int delta, bool activate, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "scroll-target",
            criteria,
            "foreground-mouse-input",
            artifactOptions,
            automation => automation.ScrollWindowTarget(CreateWindowQuery(criteria), targetName, delta, activate, criteria.All),
            targetName,
            "window-target");
    }

    public static WindowChangeResult MinimizeWindows(WindowSelectionCriteria criteria, MutationArtifactOptions? artifactOptions = null) {
        return ExecuteWindowMutation(
            "minimize",
            criteria,
            "window-management",
            artifactOptions,
            automation => automation.MinimizeWindows(CreateWindowQuery(criteria), criteria.All));
    }

    public static WindowChangeResult SnapWindow(WindowSelectionCriteria criteria, string position, MutationArtifactOptions? artifactOptions = null) {
        if (!TryParseSnapPosition(position, out SnapPosition snapPosition)) {
            throw new CommandLineException($"Unsupported snap position '{position}'.");
        }

        return ExecuteWindowMutation(
            "snap",
            criteria,
            "window-management",
            artifactOptions,
            automation => automation.SnapWindows(CreateWindowQuery(criteria), snapPosition, criteria.All));
    }

    public static WindowChangeResult TypeWindowText(WindowSelectionCriteria criteria, string text, bool paste, int delayMilliseconds, MutationArtifactOptions? artifactOptions = null) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteWindowMutation(
            paste ? "paste-text" : "type-text",
            criteria,
            paste ? "window-text-paste" : "window-text-input",
            artifactOptions,
            automation => automation.TypeWindowText(CreateWindowQuery(criteria), text, paste, delayMilliseconds, criteria.All));
    }

    public static WindowChangeResult SendWindowKeys(WindowSelectionCriteria criteria, IReadOnlyList<string> keys, bool activate, MutationArtifactOptions? artifactOptions = null) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteWindowMutation(
            "send-window-keys",
            criteria,
            "window-key-input",
            artifactOptions,
            automation => automation.SendWindowKeys(CreateWindowQuery(criteria), virtualKeys, activate, criteria.All));
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

    public static IReadOnlyList<ControlResult> ListControlTargets(WindowSelectionCriteria windowCriteria, string targetName, bool allWindows, bool allControls) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls)
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

    public static ControlAssertionResult ControlTargetExists(WindowSelectionCriteria windowCriteria, string targetName, bool allWindows, bool allControls) {
        return ExecuteCore(() => {
            IReadOnlyList<ControlResult> controls = new DesktopAutomationService()
                .GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls)
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

    public static IReadOnlyList<ControlDiagnosticResult> DiagnoseControls(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, bool allWindows, int sampleLimit, bool includeActionProbe) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlDiagnostics(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, sampleLimit, includeActionProbe)
            .Select(MapControlDiagnostics)
            .ToArray());
    }

    public static IReadOnlyList<ControlDiagnosticResult> DiagnoseControlTargets(WindowSelectionCriteria windowCriteria, string targetName, bool allWindows, int sampleLimit, bool includeActionProbe) {
        return ExecuteCore(() => new DesktopAutomationService()
            .GetControlTargetDiagnostics(CreateWindowQuery(windowCriteria), targetName, allWindows, sampleLimit, includeActionProbe)
            .Select(MapControlDiagnostics)
            .ToArray());
    }

    public static ControlActionResult ClickControl(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string button, bool allWindows, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteControlMutation(
            "click-control",
            windowCriteria,
            allWindows,
            controlCriteria.All,
            artifactOptions,
            automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All),
            resolvedControls => DetermineControlClickSafetyMode(resolvedControls),
            automation => automation.ClickControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), mouseButton, allWindows, controlCriteria.All));
    }

    public static ControlActionResult ClickControlTarget(WindowSelectionCriteria windowCriteria, string targetName, string button, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions = null) {
        MouseButton mouseButton = ParseMouseButton(button);
        return ExecuteControlMutation(
            "click-control",
            windowCriteria,
            allWindows,
            allControls,
            artifactOptions,
            automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls),
            resolvedControls => DetermineControlClickSafetyMode(resolvedControls),
            automation => automation.ClickControlTarget(CreateWindowQuery(windowCriteria), targetName, mouseButton, allWindows, allControls),
            targetName,
            "control-target");
    }

    public static ControlActionResult SetControlText(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, string text, bool allWindows, MutationArtifactOptions? artifactOptions = null) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteControlMutation(
            "set-control-text",
            windowCriteria,
            allWindows,
            controlCriteria.All,
            artifactOptions,
            automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All),
            resolvedControls => DetermineControlTextSafetyMode(resolvedControls, controlCriteria.AllowForegroundInputFallback),
            automation => automation.SetControlText(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), text, allWindows, controlCriteria.All));
    }

    public static ControlActionResult SetControlTargetText(WindowSelectionCriteria windowCriteria, string targetName, string text, bool ensureForegroundWindow, bool allowForegroundInputFallback, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions = null) {
        if (text == null) {
            throw new CommandLineException("Text is required.");
        }

        return ExecuteControlMutation(
            "set-control-text",
            windowCriteria,
            allWindows,
            allControls,
            artifactOptions,
            automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls),
            resolvedControls => DetermineControlTextSafetyMode(resolvedControls, allowForegroundInputFallback),
            automation => automation.SetControlTargetText(CreateWindowQuery(windowCriteria), targetName, text, ensureForegroundWindow, allowForegroundInputFallback, allWindows, allControls),
            targetName,
            "control-target");
    }

    public static ControlActionResult SendControlKeys(WindowSelectionCriteria windowCriteria, ControlSelectionCriteria controlCriteria, IReadOnlyList<string> keys, bool allWindows, MutationArtifactOptions? artifactOptions = null) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteControlMutation(
            "send-control-keys",
            windowCriteria,
            allWindows,
            controlCriteria.All,
            artifactOptions,
            automation => automation.GetControls(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), allWindows, controlCriteria.All),
            resolvedControls => DetermineControlKeySafetyMode(resolvedControls, controlCriteria.AllowForegroundInputFallback),
            automation => automation.SendControlKeys(CreateWindowQuery(windowCriteria), CreateControlQuery(controlCriteria), virtualKeys, allWindows, controlCriteria.All));
    }

    public static ControlActionResult SendControlTargetKeys(WindowSelectionCriteria windowCriteria, string targetName, IReadOnlyList<string> keys, bool ensureForegroundWindow, bool allowForegroundInputFallback, bool allWindows, bool allControls, MutationArtifactOptions? artifactOptions = null) {
        VirtualKey[] virtualKeys = ParseVirtualKeys(keys);
        return ExecuteControlMutation(
            "send-control-keys",
            windowCriteria,
            allWindows,
            allControls,
            artifactOptions,
            automation => automation.GetControlTargets(CreateWindowQuery(windowCriteria), targetName, allWindows, allControls),
            resolvedControls => DetermineControlKeySafetyMode(resolvedControls, allowForegroundInputFallback),
            automation => automation.SendControlTargetKeys(CreateWindowQuery(windowCriteria), targetName, virtualKeys, ensureForegroundWindow, allowForegroundInputFallback, allWindows, allControls),
            targetName,
            "control-target");
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

    public static WaitForControlResult WaitForControlTarget(WindowSelectionCriteria windowCriteria, string targetName, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows, bool allControls) {
        return ExecuteCore(() => {
            DesktopControlWaitResult result = new DesktopAutomationService().WaitForControlTarget(CreateWindowQuery(windowCriteria), targetName, timeoutMilliseconds, intervalMilliseconds, allWindows, allControls);
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

    public static WindowTargetResult SaveWindowTarget(string name, string? description, int? x, int? y, double? xRatio, double? yRatio, int? width, int? height, double? widthRatio, double? heightRatio, bool clientArea) {
        return ExecuteCore(() => {
            DesktopWindowTargetDefinition definition = new DesktopWindowTargetDefinition {
                Description = description,
                X = x,
                Y = y,
                XRatio = xRatio,
                YRatio = yRatio,
                Width = width,
                Height = height,
                WidthRatio = widthRatio,
                HeightRatio = heightRatio,
                ClientArea = clientArea
            };

            DesktopWindowTargetDefinition saved = new DesktopAutomationService().SaveWindowTarget(name, definition);
            return BuildWindowTargetResult(name, DesktopStateStore.GetTargetPath(name), saved);
        });
    }

    public static IReadOnlyList<string> ListWindowTargets() {
        return ExecuteCore(() => new DesktopAutomationService().ListWindowTargets());
    }

    public static WindowTargetResult GetWindowTarget(string name) {
        return ExecuteCore(() => BuildWindowTargetResult(
            name,
            DesktopStateStore.GetTargetPath(name),
            new DesktopAutomationService().GetWindowTarget(name)));
    }

    public static IReadOnlyList<ResolvedWindowTargetResult> ResolveWindowTargets(WindowSelectionCriteria criteria, string name) {
        return ExecuteCore(() => new DesktopAutomationService()
            .ResolveWindowTargets(CreateWindowQuery(criteria), name, criteria.All)
            .Select(MapResolvedWindowTarget)
            .ToArray());
    }

    public static ControlTargetResult SaveControlTarget(string name, ControlSelectionCriteria criteria, string? description) {
        return ExecuteCore(() => {
            DesktopControlTargetDefinition definition = new DesktopControlTargetDefinition {
                Description = description,
                ClassNamePattern = criteria.ClassNamePattern,
                TextPattern = criteria.TextPattern,
                ValuePattern = criteria.ValuePattern,
                Id = criteria.Id,
                Handle = criteria.Handle,
                AutomationIdPattern = criteria.AutomationIdPattern,
                ControlTypePattern = criteria.ControlTypePattern,
                FrameworkIdPattern = criteria.FrameworkIdPattern,
                IsEnabled = criteria.IsEnabled,
                IsKeyboardFocusable = criteria.IsKeyboardFocusable,
                SupportsBackgroundClick = criteria.SupportsBackgroundClick,
                SupportsBackgroundText = criteria.SupportsBackgroundText,
                SupportsBackgroundKeys = criteria.SupportsBackgroundKeys,
                SupportsForegroundInputFallback = criteria.SupportsForegroundInputFallback,
                UseUiAutomation = criteria.UiAutomation,
                IncludeUiAutomation = criteria.IncludeUiAutomation,
                EnsureForegroundWindow = criteria.EnsureForegroundWindow
            };

            DesktopControlTargetDefinition saved = new DesktopAutomationService().SaveControlTarget(name, definition);
            return BuildControlTargetResult(name, DesktopStateStore.GetControlTargetPath(name), saved);
        });
    }

    public static IReadOnlyList<string> ListControlTargets() {
        return ExecuteCore(() => new DesktopAutomationService().ListControlTargets());
    }

    public static ControlTargetResult GetControlTarget(string name) {
        return ExecuteCore(() => BuildControlTargetResult(
            name,
            DesktopStateStore.GetControlTargetPath(name),
            new DesktopAutomationService().GetControlTarget(name)));
    }

    public static IReadOnlyList<ResolvedControlTargetResult> ResolveControlTargets(WindowSelectionCriteria windowCriteria, string name, bool allControls = false) {
        return ExecuteCore(() => new DesktopAutomationService()
            .ResolveControlTargets(CreateWindowQuery(windowCriteria), name, windowCriteria.All, allControls)
            .Select(MapResolvedControlTarget)
            .ToArray());
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

    public static ScreenshotResult CaptureWindowTargetScreenshot(WindowSelectionCriteria criteria, string targetName, string? outputPath) {
        return ExecuteCore(() => {
            using DesktopCapture capture = new DesktopAutomationService().CaptureWindowTarget(CreateWindowQuery(criteria), targetName);
            string prefix = capture.Window == null ? $"target-{targetName}" : $"target-{targetName}-{capture.Window.ProcessId}";
            return SaveScreenshot(capture, prefix, outputPath);
        });
    }

    public static ProcessLaunchResult LaunchProcess(string filePath, string? arguments, string? workingDirectory, int? waitForInputIdleMilliseconds, int? waitForWindowMilliseconds, int? waitForWindowIntervalMilliseconds, string? windowTitlePattern, string? windowClassNamePattern, bool requireWindow) {
        return ExecuteCore(() => {
            DesktopProcessLaunchInfo result = new DesktopAutomationService().LaunchProcess(new DesktopProcessStartOptions {
                FilePath = filePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                WaitForInputIdleMilliseconds = waitForInputIdleMilliseconds,
                WaitForWindowMilliseconds = waitForWindowMilliseconds,
                WaitForWindowIntervalMilliseconds = waitForWindowIntervalMilliseconds,
                WindowTitlePattern = windowTitlePattern,
                WindowClassNamePattern = windowClassNamePattern,
                RequireWindow = requireWindow
            });

            return BuildProcessLaunchResult(result);
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

    private static WindowChangeResult BuildWindowChangeResult(string action, IReadOnlyList<WindowInfo> windows, int elapsedMilliseconds, string safetyMode, string? targetName, string? targetKind, IReadOnlyList<ScreenshotResult> beforeScreenshots, IReadOnlyList<ScreenshotResult> afterScreenshots, IReadOnlyList<string> artifactWarnings) {
        return new WindowChangeResult {
            Action = action,
            Success = true,
            Count = windows.Count,
            ElapsedMilliseconds = elapsedMilliseconds,
            SafetyMode = safetyMode,
            TargetName = targetName,
            TargetKind = targetKind,
            BeforeScreenshots = beforeScreenshots,
            AfterScreenshots = afterScreenshots,
            ArtifactWarnings = artifactWarnings,
            Windows = windows.Select(MapWindow).ToArray()
        };
    }

    internal static ProcessLaunchResult BuildProcessLaunchResult(DesktopProcessLaunchInfo result) {
        return new ProcessLaunchResult {
            FilePath = result.FilePath,
            Arguments = result.Arguments,
            WorkingDirectory = result.WorkingDirectory,
            ProcessId = result.ProcessId,
            ResolvedProcessId = result.ResolvedProcessId,
            HasExited = result.HasExited,
            MainWindow = result.MainWindow == null ? null : MapWindow(result.MainWindow)
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

    internal static WindowGeometryResult MapWindowGeometry(DesktopWindowGeometry geometry) {
        return new WindowGeometryResult {
            Window = MapWindow(geometry.Window),
            WindowLeft = geometry.WindowLeft,
            WindowTop = geometry.WindowTop,
            WindowWidth = geometry.WindowWidth,
            WindowHeight = geometry.WindowHeight,
            ClientLeft = geometry.ClientLeft,
            ClientTop = geometry.ClientTop,
            ClientWidth = geometry.ClientWidth,
            ClientHeight = geometry.ClientHeight,
            ClientOffsetLeft = geometry.ClientOffsetLeft,
            ClientOffsetTop = geometry.ClientOffsetTop
        };
    }

    private static WindowTargetResult BuildWindowTargetResult(string name, string path, DesktopWindowTargetDefinition target) {
        return new WindowTargetResult {
            Name = name,
            Path = path,
            Target = MapWindowTargetDefinition(target)
        };
    }

    private static ControlTargetResult BuildControlTargetResult(string name, string path, DesktopControlTargetDefinition target) {
        return new ControlTargetResult {
            Name = name,
            Path = path,
            Target = MapControlTargetDefinition(target)
        };
    }

    private static WindowTargetDefinitionResult MapWindowTargetDefinition(DesktopWindowTargetDefinition target) {
        return new WindowTargetDefinitionResult {
            Description = target.Description,
            X = target.X,
            Y = target.Y,
            XRatio = target.XRatio,
            YRatio = target.YRatio,
            Width = target.Width,
            Height = target.Height,
            WidthRatio = target.WidthRatio,
            HeightRatio = target.HeightRatio,
            ClientArea = target.ClientArea
        };
    }

    private static ControlTargetDefinitionResult MapControlTargetDefinition(DesktopControlTargetDefinition target) {
        return new ControlTargetDefinitionResult {
            Description = target.Description,
            ClassNamePattern = target.ClassNamePattern,
            TextPattern = target.TextPattern,
            ValuePattern = target.ValuePattern,
            Id = target.Id,
            Handle = target.Handle,
            AutomationIdPattern = target.AutomationIdPattern,
            ControlTypePattern = target.ControlTypePattern,
            FrameworkIdPattern = target.FrameworkIdPattern,
            IsEnabled = target.IsEnabled,
            IsKeyboardFocusable = target.IsKeyboardFocusable,
            SupportsBackgroundClick = target.SupportsBackgroundClick,
            SupportsBackgroundText = target.SupportsBackgroundText,
            SupportsBackgroundKeys = target.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = target.SupportsForegroundInputFallback,
            UseUiAutomation = target.UseUiAutomation,
            IncludeUiAutomation = target.IncludeUiAutomation,
            EnsureForegroundWindow = target.EnsureForegroundWindow
        };
    }

    internal static ResolvedWindowTargetResult MapResolvedWindowTarget(DesktopResolvedWindowTarget target) {
        return new ResolvedWindowTargetResult {
            Name = target.Name,
            Target = MapWindowTargetDefinition(target.Definition),
            Window = MapWindow(target.Geometry.Window),
            Geometry = MapWindowGeometry(target.Geometry),
            RelativeX = target.RelativeX,
            RelativeY = target.RelativeY,
            RelativeWidth = target.RelativeWidth,
            RelativeHeight = target.RelativeHeight,
            ScreenX = target.ScreenX,
            ScreenY = target.ScreenY,
            ScreenWidth = target.ScreenWidth,
            ScreenHeight = target.ScreenHeight
        };
    }

    internal static ResolvedControlTargetResult MapResolvedControlTarget(DesktopResolvedControlTarget target) {
        return new ResolvedControlTargetResult {
            Name = target.Name,
            Target = MapControlTargetDefinition(target.Definition),
            Window = MapWindow(target.Window),
            Control = MapControl(target.Window, target.Control)
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
            IsOffscreen = control.IsOffscreen,
            SupportsBackgroundClick = control.SupportsBackgroundClick,
            SupportsBackgroundText = control.SupportsBackgroundText,
            SupportsBackgroundKeys = control.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = control.SupportsForegroundInputFallback,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height,
            ParentWindow = MapWindow(window)
        };
    }

    internal static ControlDiagnosticResult MapControlDiagnostics(DesktopControlDiscoveryDiagnostics diagnostics) {
        return new ControlDiagnosticResult {
            Window = MapWindow(diagnostics.Window),
            RequiresUiAutomation = diagnostics.RequiresUiAutomation,
            UseUiAutomation = diagnostics.UseUiAutomation,
            IncludeUiAutomation = diagnostics.IncludeUiAutomation,
            EnsureForegroundWindow = diagnostics.EnsureForegroundWindow,
            UiAutomationAvailable = diagnostics.UiAutomationAvailable,
            ElapsedMilliseconds = diagnostics.ElapsedMilliseconds,
            PreparationAttempted = diagnostics.PreparationAttempted,
            PreparationSucceeded = diagnostics.PreparationSucceeded,
            UiAutomationFallbackRootCount = diagnostics.UiAutomationFallbackRootCount,
            UsedUiAutomationFallbackRoots = diagnostics.UsedUiAutomationFallbackRoots,
            UsedCachedUiAutomationControls = diagnostics.UsedCachedUiAutomationControls,
            UsedPreferredUiAutomationRoot = diagnostics.UsedPreferredUiAutomationRoot,
            PreferredUiAutomationRootHandle = diagnostics.PreferredUiAutomationRootHandle == IntPtr.Zero ? string.Empty : $"0x{diagnostics.PreferredUiAutomationRootHandle.ToInt64():X}",
            EffectiveSource = diagnostics.EffectiveSource,
            Win32ControlCount = diagnostics.Win32ControlCount,
            UiAutomationControlCount = diagnostics.UiAutomationControlCount,
            EffectiveControlCount = diagnostics.EffectiveControlCount,
            MatchedControlCount = diagnostics.MatchedControlCount,
            SampleControls = diagnostics.SampleControls.Select(control => MapControl(diagnostics.Window, control)).ToArray(),
            UiAutomationRoots = diagnostics.UiAutomationRoots.Select(root => new UiAutomationRootDiagnosticResult {
                Order = root.Order,
                Handle = $"0x{root.Handle.ToInt64():X}",
                ClassName = root.ClassName,
                IsPrimaryRoot = root.IsPrimaryRoot,
                IsPreferredRoot = root.IsPreferredRoot,
                UsedCachedControls = root.UsedCachedControls,
                IncludeRoot = root.IncludeRoot,
                ElementResolved = root.ElementResolved,
                ControlCount = root.ControlCount,
                Error = root.Error,
                SampleControls = root.SampleControls.Select(control => MapControl(diagnostics.Window, control)).ToArray()
            }).ToArray(),
            UiAutomationActionProbe = diagnostics.UiAutomationActionProbe == null ? null : new UiAutomationActionDiagnosticResult {
                Attempted = diagnostics.UiAutomationActionProbe.Attempted,
                Resolved = diagnostics.UiAutomationActionProbe.Resolved,
                UsedCachedActionMatch = diagnostics.UiAutomationActionProbe.UsedCachedActionMatch,
                UsedPreferredRoot = diagnostics.UiAutomationActionProbe.UsedPreferredRoot,
                RootHandle = diagnostics.UiAutomationActionProbe.RootHandle == IntPtr.Zero ? string.Empty : $"0x{diagnostics.UiAutomationActionProbe.RootHandle.ToInt64():X}",
                Score = diagnostics.UiAutomationActionProbe.Score,
                SearchMode = diagnostics.UiAutomationActionProbe.SearchMode,
                ElapsedMilliseconds = diagnostics.UiAutomationActionProbe.ElapsedMilliseconds
            }
        };
    }

    private static ControlActionResult BuildControlActionResult(string action, IReadOnlyList<WindowControlTargetInfo> controls, int elapsedMilliseconds, string safetyMode, string? targetName, string? targetKind, IReadOnlyList<ScreenshotResult> beforeScreenshots, IReadOnlyList<ScreenshotResult> afterScreenshots, IReadOnlyList<string> artifactWarnings) {
        return new ControlActionResult {
            Action = action,
            Success = true,
            Count = controls.Count,
            ElapsedMilliseconds = elapsedMilliseconds,
            SafetyMode = safetyMode,
            TargetName = targetName,
            TargetKind = targetKind,
            BeforeScreenshots = beforeScreenshots,
            AfterScreenshots = afterScreenshots,
            ArtifactWarnings = artifactWarnings,
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
            Window = capture.Window == null ? null : MapWindow(capture.Window),
            Geometry = capture.Geometry == null ? null : MapWindowGeometry(capture.Geometry)
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

    internal static WindowQueryOptions CreateWindowQuery(WindowSelectionCriteria criteria) {
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

    internal static WindowControlQueryOptions CreateControlQuery(ControlSelectionCriteria criteria) {
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
            SupportsBackgroundClick = criteria.SupportsBackgroundClick,
            SupportsBackgroundText = criteria.SupportsBackgroundText,
            SupportsBackgroundKeys = criteria.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = criteria.SupportsForegroundInputFallback,
            EnsureForegroundWindow = criteria.EnsureForegroundWindow,
            AllowForegroundInputFallback = criteria.AllowForegroundInputFallback,
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
        } catch (InvalidDataException ex) {
            throw new CommandLineException(ex.Message);
        } catch (FileNotFoundException ex) {
            throw new CommandLineException(ex.Message);
        } catch (Win32Exception ex) {
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
