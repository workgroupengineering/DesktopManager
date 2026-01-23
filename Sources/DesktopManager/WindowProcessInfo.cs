namespace DesktopManager;

/// <summary>
/// Represents process information associated with a window.
/// </summary>
public sealed class WindowProcessInfo {
    /// <summary>
    /// Gets or sets the process ID.
    /// </summary>
    public uint ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the thread ID that owns the window.
    /// </summary>
    public uint ThreadId { get; set; }

    /// <summary>
    /// Gets or sets the process name.
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path to the process executable, if available.
    /// </summary>
    public string? ProcessPath { get; set; }

    /// <summary>
    /// Gets or sets whether the process is elevated, if known.
    /// </summary>
    public bool? IsElevated { get; set; }

    /// <summary>
    /// Gets or sets whether the process is a 32-bit process on a 64-bit OS, if known.
    /// </summary>
    public bool? IsWow64 { get; set; }
}
