namespace DesktopManager;

/// <summary>
/// Represents a control together with its parent window.
/// </summary>
public sealed class WindowControlTargetInfo {
    /// <summary>
    /// Gets or sets the parent window.
    /// </summary>
    public WindowInfo Window { get; set; } = null!;

    /// <summary>
    /// Gets or sets the matched control.
    /// </summary>
    public WindowControlInfo Control { get; set; } = null!;
}
