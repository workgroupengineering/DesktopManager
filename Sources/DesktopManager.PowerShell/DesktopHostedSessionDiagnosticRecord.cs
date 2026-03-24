namespace DesktopManager.PowerShell;

/// <summary>Represents a hosted-session diagnostic artifact resolved for PowerShell output.</summary>
public sealed class DesktopHostedSessionDiagnosticRecord {
    /// <summary>
    /// <para type="description">Path to the resolved hosted-session JSON artifact.</para>
    /// </summary>
    public string ArtifactPath { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Path to the preferred companion summary artifact when one exists.</para>
    /// </summary>
    public string SummaryPath { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Resolved summary text, preferring the companion summary file when available.</para>
    /// </summary>
    public string SummaryText { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Artifact reason string recorded by the hosted-session harness.</para>
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Artifact creation time in UTC when available.</para>
    /// </summary>
    public string CreatedUtc { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Retry-history category hint such as browser-electron, mixed, or none.</para>
    /// </summary>
    public string RetryHistoryCategory { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Compact retry-history summary text when available.</para>
    /// </summary>
    public string RetryHistorySummary { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Count of retry attempts that observed an external foreground interruption.</para>
    /// </summary>
    public int RetryHistoryExternalCount { get; set; }

    /// <summary>
    /// <para type="description">Count of distinct external-foreground fingerprints seen during retries.</para>
    /// </summary>
    public int RetryHistoryDistinctFingerprintCount { get; set; }

    /// <summary>
    /// <para type="description">Structured policy report describing the observed focus-steal classification.</para>
    /// </summary>
    public string PolicyReport { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Window title recorded in the diagnostic status payload.</para>
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Status text recorded by the hosted-session harness.</para>
    /// </summary>
    public string StatusText { get; set; } = string.Empty;
}
