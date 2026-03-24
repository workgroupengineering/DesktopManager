namespace DesktopManager.Cli;

internal sealed class HostedSessionDiagnosticResult {
    public string ArtifactPath { get; set; } = string.Empty;

    public string SummaryPath { get; set; } = string.Empty;

    public string SummaryText { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string CreatedUtc { get; set; } = string.Empty;

    public string RetryHistoryCategory { get; set; } = string.Empty;

    public string RetryHistorySummary { get; set; } = string.Empty;

    public int RetryHistoryExternalCount { get; set; }

    public int RetryHistoryDistinctFingerprintCount { get; set; }

    public string PolicyReport { get; set; } = string.Empty;

    public string WindowTitle { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;
}
