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
    /// Gets or sets the control value filter. Supports wildcards and substring matches.
    /// </summary>
    public string ValuePattern { get; set; } = "*";

    /// <summary>
    /// Gets or sets a value indicating whether matching controls must be enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching controls must be keyboard focusable.
    /// </summary>
    public bool? IsKeyboardFocusable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching controls must support background-safe click or invoke actions.
    /// </summary>
    public bool? SupportsBackgroundClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching controls must support background-safe text updates.
    /// </summary>
    public bool? SupportsBackgroundText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching controls must support background-safe key delivery.
    /// </summary>
    public bool? SupportsBackgroundKeys { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching controls must support explicit foreground input fallback.
    /// </summary>
    public bool? SupportsForegroundInputFallback { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether UI Automation should be used for control discovery.
    /// </summary>
    public bool UseUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether classic Win32 and UI Automation results should be combined.
    /// </summary>
    public bool IncludeUiAutomation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the target window should be brought to the foreground before UI Automation queries.
    /// </summary>
    public bool EnsureForegroundWindow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shared foreground-based input fallback is allowed for zero-handle UI Automation controls.
    /// </summary>
    public bool AllowForegroundInputFallback { get; set; }

    /// <summary>
    /// Determines whether the current query requires UI Automation metadata.
    /// </summary>
    /// <returns><c>true</c> when UI Automation should be used; otherwise <c>false</c>.</returns>
    public bool RequiresUiAutomation() {
        return UseUiAutomation ||
            IncludeUiAutomation ||
            !IsWildcard(AutomationIdPattern) ||
            !IsWildcard(ControlTypePattern) ||
            !IsWildcard(FrameworkIdPattern) ||
            !IsWildcard(ValuePattern) ||
            IsEnabled.HasValue ||
            IsKeyboardFocusable.HasValue ||
            SupportsBackgroundClick.HasValue ||
            SupportsBackgroundText.HasValue ||
            SupportsBackgroundKeys.HasValue ||
            SupportsForegroundInputFallback.HasValue;
    }

    private static bool IsWildcard(string? value) {
        return string.IsNullOrWhiteSpace(value) || value == "*";
    }
}
