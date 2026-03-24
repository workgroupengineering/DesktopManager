namespace DesktopManager.PowerShell;

internal sealed class DesktopHostedSessionDiagnosticArtifact {
    public int FormatVersion { get; set; } = 1;

    public string Reason { get; set; } = string.Empty;

    public string CreatedUtc { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string PolicyReport { get; set; } = string.Empty;

    public DesktopHostedSessionRetryHistoryReport RetryHistoryReport { get; set; } = DesktopHostedSessionRetryHistoryReport.None;

    public DesktopHostedSessionStatus Status { get; set; } = new();
}
