namespace DesktopManager.Tests;

internal sealed class HostedSessionExternalForegroundReport {
    public static HostedSessionExternalForegroundReport None { get; } = new HostedSessionExternalForegroundReport {
        Category = "none",
        AbortThreshold = 3,
        Summary = "No external foreground interruption recorded.",
        Fingerprint = string.Empty,
        Entry = string.Empty
    };

    public string Category { get; set; } = string.Empty;
    public int AbortThreshold { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string Entry { get; set; } = string.Empty;

    public bool HasExternalForeground {
        get {
            return !string.IsNullOrWhiteSpace(Entry);
        }
    }

    public string ToPolicyReport() {
        return
            "category='" + Category + "', " +
            "abortThreshold=" + AbortThreshold + ", " +
            "fingerprint='" + Fingerprint + "'";
    }
}
