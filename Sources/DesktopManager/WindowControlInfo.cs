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
}
