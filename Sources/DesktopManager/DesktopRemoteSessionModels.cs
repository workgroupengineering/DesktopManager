namespace DesktopManager;

/// <summary>
/// Identifies the kind of remote desktop target a session is attached to.
/// </summary>
public enum DesktopRemoteSessionTargetKind
{
    /// <summary>
    /// The session is attached to the combined desktop.
    /// </summary>
    Desktop,

    /// <summary>
    /// The session is attached to a single monitor.
    /// </summary>
    Monitor,

    /// <summary>
    /// The session is attached to a single window.
    /// </summary>
    Window,

    /// <summary>
    /// The session is attached to a specific control.
    /// </summary>
    Control
}

/// <summary>
/// Describes the source viewport used by a remote desktop session.
/// </summary>
public sealed class DesktopRemoteSessionViewport
{
    /// <summary>
    /// Gets or sets the left coordinate in desktop space.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the top coordinate in desktop space.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the width in desktop pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height in desktop pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the horizontal DPI of the source viewport.
    /// </summary>
    public int DpiX { get; set; } = 96;

    /// <summary>
    /// Gets or sets the vertical DPI of the source viewport.
    /// </summary>
    public int DpiY { get; set; } = 96;

    /// <summary>
    /// Gets or sets the X-axis scale factor applied to the source viewport.
    /// </summary>
    public double ScaleX { get; set; } = 1;

    /// <summary>
    /// Gets or sets the Y-axis scale factor applied to the source viewport.
    /// </summary>
    public double ScaleY { get; set; } = 1;
}

/// <summary>
/// Describes the resolved target for a remote desktop session.
/// </summary>
public sealed class DesktopRemoteSessionTarget
{
    /// <summary>
    /// Gets or sets the target kind.
    /// </summary>
    public DesktopRemoteSessionTargetKind TargetKind { get; set; }

    /// <summary>
    /// Gets or sets the optional monitor identifier.
    /// </summary>
    public string? MonitorId { get; set; }

    /// <summary>
    /// Gets or sets the optional window identifier.
    /// </summary>
    public string? WindowId { get; set; }

    /// <summary>
    /// Gets or sets the optional control identifier.
    /// </summary>
    public string? ControlId { get; set; }

    /// <summary>
    /// Gets or sets the optional human-readable target name.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Represents a remote desktop session that can be used for view, input, and verification work.
/// </summary>
public sealed class DesktopRemoteSessionInfo
{
    /// <summary>
    /// Gets or sets the unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the resolved session target.
    /// </summary>
    public DesktopRemoteSessionTarget Target { get; set; } = new();

    /// <summary>
    /// Gets or sets the source viewport used by the session.
    /// </summary>
    public DesktopRemoteSessionViewport SourceViewport { get; set; } = new();

    /// <summary>
    /// Gets or sets the encoded frame width.
    /// </summary>
    public int FrameWidth { get; set; }

    /// <summary>
    /// Gets or sets the encoded frame height.
    /// </summary>
    public int FrameHeight { get; set; }

    /// <summary>
    /// Gets or sets whether cursor telemetry is included with session frames.
    /// </summary>
    public bool CursorIncluded { get; set; } = true;

    /// <summary>
    /// Gets or sets the active window identifier when it is known.
    /// </summary>
    public string? ActiveWindowId { get; set; }

    /// <summary>
    /// Gets or sets the session creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
