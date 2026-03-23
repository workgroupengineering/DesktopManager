namespace DesktopManager.Tests;

internal sealed class HostedSessionRetryHistoryReport {
    public static HostedSessionRetryHistoryReport None { get; } = new HostedSessionRetryHistoryReport {
        CategoryHint = "none",
        Summary = "No external foreground interruptions were recorded across retries.",
        ExternalCount = 0,
        DistinctFingerprintCount = 0
    };

    public string CategoryHint { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int ExternalCount { get; set; }
    public int DistinctFingerprintCount { get; set; }
}
