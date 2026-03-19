using System;

namespace DesktopManager;

/// <summary>
/// Defines filter options for child control enumeration.
/// </summary>
public sealed class WindowControlQueryOptions {
    /// <summary>
    /// Gets or sets the control class name filter. Supports wildcards and substring matches.
    /// </summary>
    public string ClassNamePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the control text filter. Supports wildcards and substring matches.
    /// </summary>
    public string TextPattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the exact control identifier to match.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Gets or sets the exact control handle to match.
    /// </summary>
    public IntPtr? Handle { get; set; }
}
