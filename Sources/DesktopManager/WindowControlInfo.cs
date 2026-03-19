using System;

namespace DesktopManager;

/// <summary>
/// Represents a child window control.
/// </summary>
public class WindowControlInfo {
    /// <summary>Handle of the control.</summary>
    public IntPtr Handle { get; internal set; }
    /// <summary>Class name of the control.</summary>
    public string ClassName { get; internal set; } = string.Empty;
    /// <summary>Control identifier.</summary>
    public int Id { get; internal set; }
    /// <summary>Displayed text of the control.</summary>
    public string Text { get; internal set; } = string.Empty;
    /// <summary>Origin of the control metadata.</summary>
    public WindowControlSource Source { get; internal set; } = WindowControlSource.Win32;
    /// <summary>UI Automation automation identifier when available.</summary>
    public string AutomationId { get; internal set; } = string.Empty;
    /// <summary>UI Automation control type when available.</summary>
    public string ControlType { get; internal set; } = string.Empty;
    /// <summary>UI Automation framework identifier when available.</summary>
    public string FrameworkId { get; internal set; } = string.Empty;
    /// <summary>Whether the control can receive keyboard focus when available.</summary>
    public bool? IsKeyboardFocusable { get; internal set; }
    /// <summary>Whether the control is enabled when available.</summary>
    public bool? IsEnabled { get; internal set; }
}
