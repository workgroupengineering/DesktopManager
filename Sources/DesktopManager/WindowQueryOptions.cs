using System.Text.RegularExpressions;

namespace DesktopManager;

/// <summary>
/// Defines filter options for window enumeration.
/// </summary>
public sealed class WindowQueryOptions {
    /// <summary>
    /// Gets or sets the window title filter. Supports wildcards and substring matches.
    /// </summary>
    public string TitlePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the process name filter. Supports wildcards and substring matches.
    /// </summary>
    public string ProcessNamePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the class name filter. Supports wildcards and substring matches.
    /// </summary>
    public string ClassNamePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the regular expression to match the window title.
    /// </summary>
    public Regex? TitleRegex { get; set; }

    /// <summary>
    /// Gets or sets the process ID filter.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets whether hidden windows should be included.
    /// </summary>
    public bool IncludeHidden { get; set; }

    /// <summary>
    /// Gets or sets whether DWM-cloaked windows should be included.
    /// </summary>
    public bool IncludeCloaked { get; set; } = true;

    /// <summary>
    /// Gets or sets whether owned windows should be included.
    /// </summary>
    public bool IncludeOwned { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the window must be visible.
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the required window state.
    /// </summary>
    public WindowState? State { get; set; }

    /// <summary>
    /// Gets or sets whether the window must be topmost.
    /// </summary>
    public bool? IsTopMost { get; set; }

    /// <summary>
    /// Gets or sets the minimum Z-order index (0 is top-most).
    /// </summary>
    public int? ZOrderMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum Z-order index (0 is top-most).
    /// </summary>
    public int? ZOrderMax { get; set; }
}
