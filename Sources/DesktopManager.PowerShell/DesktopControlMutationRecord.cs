using System.Collections.Generic;

namespace DesktopManager.PowerShell;

/// <summary>Represents the outcome of a PowerShell control mutation.</summary>
public sealed class DesktopControlMutationRecord {
    /// <summary>Gets or sets the action name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the mutation call completed without throwing.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets whether post-mutation verification was requested.</summary>
    public bool VerificationPerformed { get; set; }

    /// <summary>Gets or sets whether the post-mutation verification succeeded.</summary>
    public bool? Verified { get; set; }

    /// <summary>Gets or sets the verification mode.</summary>
    public string VerificationMode { get; set; } = string.Empty;

    /// <summary>Gets or sets the verification summary.</summary>
    public string VerificationSummary { get; set; } = string.Empty;

    /// <summary>Gets or sets the originally targeted control.</summary>
    public WindowControlInfo RequestedControl { get; set; } = new();

    /// <summary>Gets or sets the observed control after the mutation, when available.</summary>
    public WindowControlInfo ObservedControl { get; set; }

    /// <summary>Gets or sets the observed parent window after the mutation, when available.</summary>
    public WindowInfo ParentWindow { get; set; }

    /// <summary>Gets or sets the active foreground window observed during verification, when available.</summary>
    public WindowInfo ActiveWindow { get; set; }

    /// <summary>Gets or sets verification notes.</summary>
    public IReadOnlyList<string> VerificationNotes { get; set; } = new List<string>();
}
