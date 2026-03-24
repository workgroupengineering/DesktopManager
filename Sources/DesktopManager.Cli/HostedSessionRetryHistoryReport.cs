namespace DesktopManager.Cli;

internal sealed class HostedSessionRetryHistoryReport {
    public static HostedSessionRetryHistoryReport None { get; } = new();

    public string CategoryHint { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public int ExternalCount { get; set; }

    public int DistinctFingerprintCount { get; set; }
}
