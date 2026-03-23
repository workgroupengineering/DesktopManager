namespace DesktopManager.Tests;

internal sealed class HostedSessionDiagnosticArtifact {
    public int FormatVersion { get; set; } = 1;

    public string Reason { get; set; } = string.Empty;

    public string CreatedUtc { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string PolicyReport { get; set; } = string.Empty;

    public HostedSessionRetryHistoryReport RetryHistoryReport { get; set; } = HostedSessionRetryHistoryReport.None;

    public DesktopManagerTestAppStatus Status { get; set; } = new();
}
