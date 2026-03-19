using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace DesktopManager;

/// <summary>
/// Options for launching a desktop process.
/// </summary>
public sealed class DesktopProcessStartOptions {
    /// <summary>
    /// Gets or sets the executable path or shell command.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional argument string.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the optional working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the optional input-idle wait timeout in milliseconds.
    /// </summary>
    public int? WaitForInputIdleMilliseconds { get; set; }
}

/// <summary>
/// Represents a launched desktop process and its preferred main window.
/// </summary>
public sealed class DesktopProcessLaunchInfo {
    /// <summary>
    /// Gets or sets the process file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional argument string.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the optional working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the process identifier.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets whether the process has already exited.
    /// </summary>
    public bool HasExited { get; set; }

    /// <summary>
    /// Gets or sets the preferred main window when one can be resolved.
    /// </summary>
    public WindowInfo? MainWindow { get; set; }
}

/// <summary>
/// Represents a wait operation for one or more windows.
/// </summary>
public sealed class DesktopWindowWaitResult {
    /// <summary>
    /// Gets or sets the elapsed wait time in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> Windows { get; set; } = Array.Empty<WindowInfo>();
}

/// <summary>
/// Represents a wait operation for one or more controls.
/// </summary>
public sealed class DesktopControlWaitResult {
    /// <summary>
    /// Gets or sets the elapsed wait time in milliseconds.
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> Controls { get; set; } = Array.Empty<WindowControlTargetInfo>();
}

/// <summary>
/// Represents a screenshot capture produced by DesktopManager.
/// </summary>
public sealed class DesktopCapture : IDisposable {
    /// <summary>
    /// Gets or sets the capture kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the captured bitmap.
    /// </summary>
    public Bitmap Bitmap { get; set; } = null!;

    /// <summary>
    /// Gets or sets the captured window when applicable.
    /// </summary>
    public WindowInfo? Window { get; set; }

    /// <summary>
    /// Gets or sets the captured monitor index when applicable.
    /// </summary>
    public int? MonitorIndex { get; set; }

    /// <summary>
    /// Gets or sets the captured monitor device name when applicable.
    /// </summary>
    public string? MonitorDeviceName { get; set; }

    /// <summary>
    /// Disposes the underlying bitmap.
    /// </summary>
    public void Dispose() {
        Bitmap?.Dispose();
    }
}
