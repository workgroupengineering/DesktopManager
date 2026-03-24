namespace DesktopManager.PowerShell;

internal sealed class DesktopHostedSessionRetryHistoryReport {
    public static DesktopHostedSessionRetryHistoryReport None { get; } = new();

    public string CategoryHint { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public int ExternalCount { get; set; }

    public int DistinctFingerprintCount { get; set; }
}
