using System.Collections.Generic;

namespace DesktopManager.Cli;

internal sealed class WindowSelectionCriteria {
    public string TitlePattern { get; set; } = "*";
    public string ProcessNamePattern { get; set; } = "*";
    public string ClassNamePattern { get; set; } = "*";
    public int? ProcessId { get; set; }
    public string? Handle { get; set; }
    public bool IncludeHidden { get; set; }
    public bool IncludeCloaked { get; set; } = true;
    public bool IncludeOwned { get; set; } = true;
    public bool IncludeEmptyTitles { get; set; }
    public bool All { get; set; }
}

internal sealed class WindowResult {
    public string Title { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public uint ThreadId { get; set; }
    public bool IsVisible { get; set; }
    public bool IsTopMost { get; set; }
    public string? State { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int MonitorIndex { get; set; }
    public string MonitorDeviceName { get; set; } = string.Empty;
}

internal sealed class WindowChangeResult {
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
    public IReadOnlyList<WindowResult> Windows { get; set; } = new List<WindowResult>();
}

internal sealed class MonitorResult {
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceString { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsPrimary { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }
}

internal sealed class NamedStateResult {
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Scope { get; set; }
}
