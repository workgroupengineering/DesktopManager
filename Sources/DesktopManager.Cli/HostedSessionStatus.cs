using System.Collections.Generic;

namespace DesktopManager.Cli;

internal sealed class HostedSessionStatus {
    public string WindowTitle { get; set; } = string.Empty;

    public string LastObservedForegroundTitle { get; set; } = string.Empty;

    public string LastObservedForegroundClass { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public List<string> ForegroundHistory { get; set; } = [];
}
