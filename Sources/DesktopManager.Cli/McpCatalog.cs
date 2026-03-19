using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static class McpCatalog {
    public static object[] GetTools() {
        return new object[] {
            CreateTool("get_active_window", "Get Active Window", "Return information about the currently focused window.", CreateObjectSchema(), readOnly: true),
            CreateTool("list_windows", "List Windows", "List visible desktop windows with optional filtering.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("window_exists", "Window Exists", "Check whether a matching window currently exists.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("active_window_matches", "Active Window Matches", "Check whether the current foreground window matches the selector.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("wait_for_window", "Wait For Window", "Wait for a matching window to appear.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Return all matching windows instead of the first match."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("list_window_controls", "List Window Controls", "List child controls for one or more matching windows.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows.")
                }), readOnly: true),
            CreateTool("diagnose_window_controls", "Diagnose Window Controls", "Collect discovery diagnostics for matching window controls.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows."),
                    ["sampleLimit"] = CreateIntegerSchema("Maximum number of sample controls to include in each diagnostic result.")
                }), readOnly: true),
            CreateTool("control_exists", "Control Exists", "Check whether a matching control currently exists.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows.")
                }), readOnly: true),
            CreateTool("wait_for_control", "Wait For Control", "Wait for a matching control to appear.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["all"] = CreateBooleanSchema("Return all matching controls instead of the first match."),
                    ["allWindows"] = CreateBooleanSchema("Enumerate controls for all matching windows."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("click_control", "Click Control", "Click a matching child control.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["button"] = CreateStringSchema("Mouse button: left or right."),
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows.")
                }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("set_control_text", "Set Control Text", "Set text on a matching child control.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["text"] = CreateStringSchema("Text to set on the control."),
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows.")
                }, new[] { "text" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("send_control_keys", "Send Control Keys", "Send keys to a matching child control.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["windowClassName"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Window process identifier."),
                    ["windowHandle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["controlClassName"] = CreateStringSchema("Control class filter."),
                    ["controlText"] = CreateStringSchema("Control text filter."),
                    ["controlValue"] = CreateStringSchema("Control value filter."),
                    ["controlId"] = CreateIntegerSchema("Control identifier."),
                    ["controlHandle"] = CreateStringSchema("Control handle in decimal or hexadecimal format."),
                    ["controlAutomationId"] = CreateStringSchema("UI Automation automation identifier filter."),
                    ["controlType"] = CreateStringSchema("UI Automation control type filter."),
                    ["controlFrameworkId"] = CreateStringSchema("UI Automation framework identifier filter."),
                    ["isEnabled"] = CreateBooleanSchema("Filter by whether the control is enabled."),
                    ["isKeyboardFocusable"] = CreateBooleanSchema("Filter by whether the control can receive keyboard focus."),
                    ["uiAutomation"] = CreateBooleanSchema("Use UI Automation for control discovery."),
                    ["includeUiAutomation"] = CreateBooleanSchema("Combine Win32 and UI Automation control results."),
                    ["ensureForegroundWindow"] = CreateBooleanSchema("Bring the target window to the foreground before UI Automation queries."),
                    ["keys"] = new {
                        type = "array",
                        items = new { type = "string" },
                        description = "Virtual key names such as VK_CONTROL or VK_S."
                    },
                    ["all"] = CreateBooleanSchema("Apply to all matching controls."),
                    ["allWindows"] = CreateBooleanSchema("Target controls in all matching windows.")
                }, new[] { "keys" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("move_window", "Move Window", "Move and optionally resize a window by title, process, pid, class, or handle.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["monitor"] = CreateIntegerSchema("Target monitor index."),
                    ["x"] = CreateIntegerSchema("Left coordinate."),
                    ["y"] = CreateIntegerSchema("Top coordinate."),
                    ["width"] = CreateIntegerSchema("Window width."),
                    ["height"] = CreateIntegerSchema("Window height."),
                    ["activate"] = CreateBooleanSchema("Activate the window after moving."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("click_window_point", "Click Window Point", "Click a point relative to a matching window.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["x"] = CreateIntegerSchema("Horizontal coordinate relative to the window bounds."),
                    ["y"] = CreateIntegerSchema("Vertical coordinate relative to the window bounds."),
                    ["button"] = CreateStringSchema("Mouse button: left or right."),
                    ["activate"] = CreateBooleanSchema("Activate the window before clicking."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }, new[] { "x", "y" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("type_window_text", "Type Window Text", "Type or paste text into a matching window.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["text"] = CreateStringSchema("Text to send to the window."),
                    ["paste"] = CreateBooleanSchema("Use clipboard paste instead of typed characters."),
                    ["delayMs"] = CreateIntegerSchema("Delay in milliseconds between typed characters."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }, new[] { "text" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("focus_window", "Focus Window", "Bring a matching window to the foreground.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("minimize_windows", "Minimize Windows", "Minimize one or more matching windows.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("snap_window", "Snap Window", "Snap one or more matching windows to a predefined monitor region.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["position"] = CreateStringSchema("One of left, right, top-left, top-right, bottom-left, bottom-right."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }, new[] { "position" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("list_monitors", "List Monitors", "List connected monitors and their bounds.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["connectedOnly"] = CreateBooleanSchema("Return only connected monitors."),
                    ["primaryOnly"] = CreateBooleanSchema("Return only the primary monitor."),
                    ["index"] = CreateIntegerSchema("Specific monitor index to return.")
                }), readOnly: true),
            CreateTool("screenshot_desktop", "Screenshot Desktop", "Capture the desktop, a monitor, or a region to a PNG file.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["monitor"] = CreateIntegerSchema("Target monitor index."),
                    ["deviceId"] = CreateStringSchema("Target monitor device identifier."),
                    ["deviceName"] = CreateStringSchema("Target monitor device name."),
                    ["left"] = CreateIntegerSchema("Left coordinate for region capture."),
                    ["top"] = CreateIntegerSchema("Top coordinate for region capture."),
                    ["width"] = CreateIntegerSchema("Width for region capture."),
                    ["height"] = CreateIntegerSchema("Height for region capture."),
                    ["outputPath"] = CreateStringSchema("Optional PNG output path.")
                }), readOnly: true),
            CreateTool("screenshot_window", "Screenshot Window", "Capture a matching window to a PNG file.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
                    ["outputPath"] = CreateStringSchema("Optional PNG output path.")
                }), readOnly: true),
            CreateTool("launch_process", "Launch Process", "Start a desktop application or process.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["filePath"] = CreateStringSchema("Executable path or shell command."),
                    ["arguments"] = CreateStringSchema("Optional argument string."),
                    ["workingDirectory"] = CreateStringSchema("Optional working directory."),
                    ["waitForInputIdleMs"] = CreateIntegerSchema("Optional wait for UI input idle in milliseconds.")
                }, new[] { "filePath" }), readOnly: false, destructive: false, idempotent: false),
            CreateTool("list_named_layouts", "List Named Layouts", "List saved named layouts.", CreateObjectSchema(), readOnly: true),
            CreateTool("save_current_layout", "Save Current Layout", "Save the current desktop window layout under a given name.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Layout name.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("apply_named_layout", "Apply Named Layout", "Restore a previously saved named layout.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Layout name."),
                    ["validate"] = CreateBooleanSchema("Validate the layout before applying it.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("list_named_snapshots", "List Named Snapshots", "List saved named snapshots.", CreateObjectSchema(), readOnly: true),
            CreateTool("save_current_snapshot", "Save Current Snapshot", "Save the current desktop snapshot. Snapshots are windows-only for now.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Snapshot name.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("restore_saved_snapshot", "Restore Saved Snapshot", "Restore a previously saved snapshot. Snapshots are windows-only for now.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["name"] = CreateStringSchema("Snapshot name."),
                    ["validate"] = CreateBooleanSchema("Validate the snapshot before applying it.")
                }, new[] { "name" }), readOnly: false, destructive: false, idempotent: true)
        };
    }

    public static object[] GetResources() {
        return new object[] {
            new {
                name = "desktop_monitors",
                title = "Desktop Monitors",
                uri = "desktop://monitors",
                description = "Current connected monitor list as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_windows_visible",
                title = "Visible Windows",
                uri = "desktop://windows/visible",
                description = "Current visible windows as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_active_window",
                title = "Active Window",
                uri = "desktop://windows/active",
                description = "Current active window as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_layouts",
                title = "Named Layouts",
                uri = "desktop://layouts",
                description = "Saved named layouts as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_snapshot_current",
                title = "Current Desktop Snapshot",
                uri = "desktop://snapshot/current",
                description = "Current windows and monitors summary as JSON.",
                mimeType = "application/json"
            }
        };
    }

    public static object[] GetPrompts() {
        return new object[] {
            new {
                name = "prepare_for_coding",
                title = "Prepare For Coding",
                description = "Arrange the desktop for focused coding work.",
                arguments = new object[] {
                    new {
                        name = "layoutName",
                        description = "Preferred named layout to apply before focusing the editor.",
                        required = false
                    }
                }
            },
            new {
                name = "prepare_for_screen_sharing",
                title = "Prepare For Screen Sharing",
                description = "Arrange the desktop for a clean screen sharing session.",
                arguments = new object[] {
                    new {
                        name = "layoutName",
                        description = "Preferred named layout to apply before sharing.",
                        required = false
                    }
                }
            },
            new {
                name = "clean_up_distractions",
                title = "Clean Up Distractions",
                description = "Hide or minimize noisy windows before focused work.",
                arguments = Array.Empty<object>()
            }
        };
    }

    public static bool TryCallTool(string name, JsonElement arguments, out object result, out string? error) {
        try {
            result = name switch {
                "get_active_window" => DesktopOperations.GetActiveWindow(),
                "list_windows" => DesktopOperations.ListWindows(ReadWindowCriteria(arguments, false)),
                "window_exists" => DesktopOperations.WindowExists(ReadWindowCriteria(arguments, true)),
                "active_window_matches" => DesktopOperations.ActiveWindowMatches(ReadWindowCriteria(arguments, true)),
                "wait_for_window" => DesktopOperations.WaitForWindow(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200),
                "move_window" => DesktopOperations.MoveWindow(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "monitor"),
                    ReadInt(arguments, "x"),
                    ReadInt(arguments, "y"),
                    ReadInt(arguments, "width"),
                    ReadInt(arguments, "height"),
                    ReadBool(arguments, "activate")),
                "click_window_point" => DesktopOperations.ClickWindowPoint(
                    ReadWindowCriteria(arguments, true),
                    ReadInt(arguments, "x") ?? throw new CommandLineException("Property 'x' is required."),
                    ReadInt(arguments, "y") ?? throw new CommandLineException("Property 'y' is required."),
                    ReadOptionalString(arguments, "button") ?? "left",
                    ReadBool(arguments, "activate")),
                "focus_window" => DesktopOperations.FocusWindow(ReadWindowCriteria(arguments, true)),
                "minimize_windows" => DesktopOperations.MinimizeWindows(ReadWindowCriteria(arguments, true)),
                "snap_window" => DesktopOperations.SnapWindow(ReadWindowCriteria(arguments, true), ReadRequiredString(arguments, "position")),
                "list_monitors" => DesktopOperations.ListMonitors(ReadNullableBool(arguments, "connectedOnly"), ReadNullableBool(arguments, "primaryOnly"), ReadInt(arguments, "index")),
                "screenshot_desktop" => DesktopOperations.CaptureDesktopScreenshot(
                    ReadInt(arguments, "monitor"),
                    ReadOptionalString(arguments, "deviceId"),
                    ReadOptionalString(arguments, "deviceName"),
                    ReadInt(arguments, "left"),
                    ReadInt(arguments, "top"),
                    ReadInt(arguments, "width"),
                    ReadInt(arguments, "height"),
                    ReadOptionalString(arguments, "outputPath")),
                "screenshot_window" => DesktopOperations.CaptureWindowScreenshot(ReadWindowCriteria(arguments, true), ReadOptionalString(arguments, "outputPath")),
                "launch_process" => DesktopOperations.LaunchProcess(
                    ReadRequiredString(arguments, "filePath"),
                    ReadOptionalString(arguments, "arguments"),
                    ReadOptionalString(arguments, "workingDirectory"),
                    ReadInt(arguments, "waitForInputIdleMs")),
                "list_window_controls" => DesktopOperations.ListControls(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadBool(arguments, "allWindows")),
                "diagnose_window_controls" => DesktopOperations.DiagnoseControls(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadBool(arguments, "allWindows"),
                    ReadInt(arguments, "sampleLimit") ?? 10),
                "control_exists" => DesktopOperations.ControlExists(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadBool(arguments, "allWindows")),
                "wait_for_control" => DesktopOperations.WaitForControl(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadInt(arguments, "timeoutMs") ?? 10000,
                    ReadInt(arguments, "intervalMs") ?? 200,
                    ReadBool(arguments, "allWindows")),
                "click_control" => DesktopOperations.ClickControl(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadOptionalString(arguments, "button") ?? "left",
                    ReadBool(arguments, "allWindows")),
                "set_control_text" => DesktopOperations.SetControlText(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadRequiredString(arguments, "text"),
                    ReadBool(arguments, "allWindows")),
                "send_control_keys" => DesktopOperations.SendControlKeys(
                    ReadWindowCriteria(arguments, true, "windowTitle", "processName", "windowClassName", "processId", "windowHandle"),
                    ReadControlCriteria(arguments),
                    ReadStringList(arguments, "keys"),
                    ReadBool(arguments, "allWindows")),
                "type_window_text" => DesktopOperations.TypeWindowText(
                    ReadWindowCriteria(arguments, true),
                    ReadRequiredString(arguments, "text"),
                    ReadBool(arguments, "paste"),
                    ReadInt(arguments, "delayMs") ?? 0),
                "list_named_layouts" => DesktopOperations.ListLayouts(),
                "save_current_layout" => DesktopOperations.SaveLayout(ReadRequiredString(arguments, "name")),
                "apply_named_layout" => DesktopOperations.ApplyLayout(ReadRequiredString(arguments, "name"), ReadBool(arguments, "validate")),
                "list_named_snapshots" => DesktopOperations.ListSnapshots(),
                "save_current_snapshot" => DesktopOperations.SaveSnapshot(ReadRequiredString(arguments, "name")),
                "restore_saved_snapshot" => DesktopOperations.RestoreSnapshot(ReadRequiredString(arguments, "name"), ReadBool(arguments, "validate")),
                _ => throw new CommandLineException($"Unknown tool '{name}'.")
            };
            error = null;
            return true;
        } catch (CommandLineException ex) {
            result = new { error = ex.Message };
            error = ex.Message;
            return false;
        }
    }

    public static object ReadResource(string uri) {
        return uri switch {
            "desktop://monitors" => DesktopOperations.ListMonitors(connectedOnly: true),
            "desktop://windows/visible" => DesktopOperations.ListWindows(new WindowSelectionCriteria()),
            "desktop://windows/active" => DesktopOperations.GetActiveWindow(),
            "desktop://layouts" => DesktopOperations.ListLayouts(),
            "desktop://snapshot/current" => DesktopOperations.GetCurrentSnapshotSummary(),
            _ => throw new CommandLineException($"Unknown resource '{uri}'.")
        };
    }

    public static object GetPrompt(string name, JsonElement arguments) {
        string? layoutName = ReadOptionalString(arguments, "layoutName");
        return name switch {
            "prepare_for_coding" => BuildPrompt("Prepare the desktop for focused coding work.", layoutName, "Start by listing named layouts. If the requested layout exists, apply it. Then inspect visible windows and focus the main editor or terminal window. If the layout is missing, explain the gap and suggest the nearest saved layout."),
            "prepare_for_screen_sharing" => BuildPrompt("Prepare the desktop for a clean screen sharing session.", layoutName, "Start by listing named layouts. If the requested layout exists, apply it. Then inspect visible windows, minimize obviously distracting windows, and focus the application that should be shared."),
            "clean_up_distractions" => BuildPrompt("Clean up distracting windows before focused work.", null, "Inspect visible windows first. Minimize obvious distractions such as chat, mail, or utility windows when appropriate, but avoid closing anything. Explain what changed."),
            _ => throw new CommandLineException($"Unknown prompt '{name}'.")
        };
    }

    private static object BuildPrompt(string summary, string? layoutName, string instructions) {
        string layoutText = string.IsNullOrWhiteSpace(layoutName) ? "No preferred layout was provided." : $"Preferred layout: {layoutName}.";
        return new {
            description = summary,
            messages = new[] {
                new {
                    role = "user",
                    content = new {
                        type = "text",
                        text = $"{summary} {layoutText} {instructions}"
                    }
                }
            }
        };
    }

    private static WindowSelectionCriteria ReadWindowCriteria(JsonElement element, bool includeEmptyDefault) {
        return ReadWindowCriteria(element, includeEmptyDefault, "windowTitle", "processName", "className", "processId", "handle");
    }

    private static WindowSelectionCriteria ReadWindowCriteria(JsonElement element, bool includeEmptyDefault, string titleProperty, string processNameProperty, string classNameProperty, string processIdProperty, string handleProperty) {
        return new WindowSelectionCriteria {
            TitlePattern = ReadOptionalString(element, titleProperty) ?? "*",
            ProcessNamePattern = ReadOptionalString(element, processNameProperty) ?? "*",
            ClassNamePattern = ReadOptionalString(element, classNameProperty) ?? "*",
            ProcessId = ReadInt(element, processIdProperty),
            Handle = ReadOptionalString(element, handleProperty),
            Active = ReadBool(element, "activeWindow"),
            IncludeHidden = ReadBool(element, "includeHidden"),
            IncludeCloaked = !ReadBool(element, "excludeCloaked"),
            IncludeOwned = !ReadBool(element, "excludeOwned"),
            IncludeEmptyTitles = ReadNullableBool(element, "includeEmpty") ?? includeEmptyDefault,
            All = ReadBool(element, "all")
        };
    }

    private static ControlSelectionCriteria ReadControlCriteria(JsonElement element) {
        return new ControlSelectionCriteria {
            ClassNamePattern = ReadOptionalString(element, "controlClassName") ?? "*",
            TextPattern = ReadOptionalString(element, "controlText") ?? "*",
            ValuePattern = ReadOptionalString(element, "controlValue") ?? "*",
            Id = ReadInt(element, "controlId"),
            Handle = ReadOptionalString(element, "controlHandle"),
            AutomationIdPattern = ReadOptionalString(element, "controlAutomationId") ?? "*",
            ControlTypePattern = ReadOptionalString(element, "controlType") ?? "*",
            FrameworkIdPattern = ReadOptionalString(element, "controlFrameworkId") ?? "*",
            IsEnabled = ReadNullableBool(element, "isEnabled"),
            IsKeyboardFocusable = ReadNullableBool(element, "isKeyboardFocusable"),
            EnsureForegroundWindow = ReadBool(element, "ensureForegroundWindow"),
            UiAutomation = ReadBool(element, "uiAutomation"),
            IncludeUiAutomation = ReadBool(element, "includeUiAutomation"),
            All = ReadBool(element, "all")
        };
    }

    private static object CreateTool(string name, string title, string description, object inputSchema, bool readOnly, bool destructive = false, bool idempotent = false) {
        return new {
            name,
            title,
            description,
            inputSchema,
            annotations = new {
                title,
                readOnlyHint = readOnly,
                destructiveHint = destructive,
                idempotentHint = idempotent,
                openWorldHint = false
            }
        };
    }

    private static object CreateWindowSelectorSchema(bool includeAll, bool includeEmpty) {
        var properties = new Dictionary<string, object> {
            ["windowTitle"] = CreateStringSchema("Window title filter."),
            ["processName"] = CreateStringSchema("Process name filter."),
            ["className"] = CreateStringSchema("Window class filter."),
            ["processId"] = CreateIntegerSchema("Process identifier."),
            ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
            ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
            ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
            ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
            ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows.")
        };

        if (includeEmpty) {
            properties["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles.");
        }

        if (includeAll) {
            properties["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.");
        }

        return CreateObjectSchema(properties);
    }

    private static object CreateObjectSchema(Dictionary<string, object>? properties = null, string[]? required = null) {
        return new {
            type = "object",
            properties = properties ?? new Dictionary<string, object>(),
            required = required ?? Array.Empty<string>()
        };
    }

    private static object CreateStringSchema(string description) {
        return new {
            type = "string",
            description
        };
    }

    private static object CreateIntegerSchema(string description) {
        return new {
            type = "integer",
            description
        };
    }

    private static object CreateBooleanSchema(string description) {
        return new {
            type = "boolean",
            description
        };
    }

    private static string ReadRequiredString(JsonElement element, string propertyName) {
        return ReadOptionalString(element, propertyName) ?? throw new CommandLineException($"Property '{propertyName}' is required.");
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName) {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static IReadOnlyList<string> ReadStringList(JsonElement element, string propertyName) {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return Array.Empty<string>();
        }

        if (property.ValueKind == JsonValueKind.Array) {
            List<string> values = new();
            foreach (JsonElement item in property.EnumerateArray()) {
                if (item.ValueKind == JsonValueKind.Null) {
                    continue;
                }

                values.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString());
            }

            return values;
        }

        string? single = ReadOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(single) ? Array.Empty<string>() : new[] { single };
    }

    private static int? ReadInt(JsonElement element, string propertyName) {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int numericValue)) {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out int textValue)) {
            return textValue;
        }

        throw new CommandLineException($"Property '{propertyName}' expects an integer value.");
    }

    private static bool ReadBool(JsonElement element, string propertyName) {
        return ReadNullableBool(element, propertyName) ?? false;
    }

    private static bool? ReadNullableBool(JsonElement element, string propertyName) {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True) {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False) {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Property '{propertyName}' expects a boolean value.");
    }
}
