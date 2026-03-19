using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static class McpCatalog {
    public static object[] GetTools() {
        return new object[] {
            CreateTool("get_active_window", "Get Active Window", "Return information about the currently focused window.", CreateObjectSchema(), readOnly: true),
            CreateTool("list_windows", "List Windows", "List visible desktop windows with optional filtering.", CreateWindowSelectorSchema(includeAll: false, includeEmpty: true), readOnly: true),
            CreateTool("wait_for_window", "Wait For Window", "Wait for a matching window to appear.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
                    ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
                    ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows."),
                    ["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles."),
                    ["all"] = CreateBooleanSchema("Return all matching windows instead of the first match."),
                    ["timeoutMs"] = CreateIntegerSchema("Maximum time to wait in milliseconds."),
                    ["intervalMs"] = CreateIntegerSchema("Polling interval in milliseconds.")
                }), readOnly: true),
            CreateTool("move_window", "Move Window", "Move and optionally resize a window by title, process, pid, class, or handle.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
                    ["monitor"] = CreateIntegerSchema("Target monitor index."),
                    ["x"] = CreateIntegerSchema("Left coordinate."),
                    ["y"] = CreateIntegerSchema("Top coordinate."),
                    ["width"] = CreateIntegerSchema("Window width."),
                    ["height"] = CreateIntegerSchema("Window height."),
                    ["activate"] = CreateBooleanSchema("Activate the window after moving."),
                    ["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.")
                }), readOnly: false, destructive: false, idempotent: true),
            CreateTool("focus_window", "Focus Window", "Bring a matching window to the foreground.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("minimize_windows", "Minimize Windows", "Minimize one or more matching windows.", CreateWindowSelectorSchema(includeAll: true, includeEmpty: false), readOnly: false, destructive: false, idempotent: true),
            CreateTool("snap_window", "Snap Window", "Snap one or more matching windows to a predefined monitor region.", CreateObjectSchema(
                new Dictionary<string, object> {
                    ["windowTitle"] = CreateStringSchema("Window title filter."),
                    ["processName"] = CreateStringSchema("Process name filter."),
                    ["className"] = CreateStringSchema("Window class filter."),
                    ["processId"] = CreateIntegerSchema("Process identifier."),
                    ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
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
        return new WindowSelectionCriteria {
            TitlePattern = ReadOptionalString(element, "windowTitle") ?? "*",
            ProcessNamePattern = ReadOptionalString(element, "processName") ?? "*",
            ClassNamePattern = ReadOptionalString(element, "className") ?? "*",
            ProcessId = ReadInt(element, "processId"),
            Handle = ReadOptionalString(element, "handle"),
            IncludeHidden = ReadBool(element, "includeHidden"),
            IncludeCloaked = !ReadBool(element, "excludeCloaked"),
            IncludeOwned = !ReadBool(element, "excludeOwned"),
            IncludeEmptyTitles = ReadNullableBool(element, "includeEmpty") ?? includeEmptyDefault,
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
