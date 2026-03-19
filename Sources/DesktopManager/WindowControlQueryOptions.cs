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

    /// <summary>
    /// Gets or sets the UI Automation automation identifier filter. Supports wildcards and substring matches.
    /// </summary>
    public string AutomationIdPattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the UI Automation control type filter. Supports wildcards and substring matches.
    /// </summary>
    public string ControlTypePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets the UI Automation framework identifier filter. Supports wildcards and substring matches.
    /// </summary>
    public string FrameworkIdPattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets a value indicating whether UI Automation should be used for control discovery.
    /// </summary>
    public bool UseUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether classic Win32 and UI Automation results should be combined.
    /// </summary>
    public bool IncludeUiAutomation { get; set; }

    /// <summary>
    /// Determines whether the current query requires UI Automation metadata.
    /// </summary>
    /// <returns><c>true</c> when UI Automation should be used; otherwise <c>false</c>.</returns>
    public bool RequiresUiAutomation() {
        return UseUiAutomation ||
            IncludeUiAutomation ||
            !IsWildcard(AutomationIdPattern) ||
            !IsWildcard(ControlTypePattern) ||
            !IsWildcard(FrameworkIdPattern);
    }

    private static bool IsWildcard(string? value) {
        return string.IsNullOrWhiteSpace(value) || value == "*";
    }
}
