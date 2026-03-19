namespace DesktopManager;

/// <summary>
/// Identifies how a window control was discovered.
/// </summary>
public enum WindowControlSource {
    /// <summary>
    /// The control was discovered through classic child-window enumeration.
    /// </summary>
    Win32,

    /// <summary>
    /// The control was discovered through UI Automation.
    /// </summary>
    UiAutomation
}
