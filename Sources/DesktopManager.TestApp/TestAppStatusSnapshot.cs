namespace DesktopManager.TestApp;

internal sealed class TestAppStatusSnapshot {
    public int ProcessId { get; set; }

    public long WindowHandle { get; set; }

    public long EditorHandle { get; set; }

    public long SecondaryWindowHandle { get; set; }

    public string WindowTitle { get; set; } = string.Empty;

    public string ActiveSurface { get; set; } = string.Empty;

    public bool ContainsFocus { get; set; }

    public bool IsForegroundWindow { get; set; }

    public bool SecondaryIsForegroundWindow { get; set; }

    public bool ForegroundHoldActive { get; set; }

    public string ForegroundHoldSurface { get; set; } = string.Empty;

    public int ForegroundHoldRequestCount { get; set; }

    public int ForegroundHoldRecoveryCount { get; set; }

    public long LastObservedForegroundHandle { get; set; }

    public string LastObservedForegroundTitle { get; set; } = string.Empty;

    public string LastObservedForegroundClass { get; set; } = string.Empty;

    public string LastObservedForegroundChangedUtc { get; set; } = string.Empty;

    public string LastCommand { get; set; } = string.Empty;

    public List<string> ForegroundHistory { get; set; } = [];

    public string EditorText { get; set; } = string.Empty;

    public string SecondaryText { get; set; } = string.Empty;

    public string CommandBarText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string DragPayload { get; set; } = string.Empty;

    public string DroppedText { get; set; } = string.Empty;

    public int DragDropCount { get; set; }

    public string DragDropStatus { get; set; } = string.Empty;

    public TestAppControlBounds EditorBounds { get; set; } = new();

    public TestAppControlBounds DragSourceBounds { get; set; } = new();

    public TestAppControlBounds DropTargetBounds { get; set; } = new();
}
