namespace DesktopManager;

/// <summary>
/// Modifiers for registering global hotkeys.
/// </summary>
[System.Flags]
public enum HotkeyModifiers {
    /// <summary>No modifier.</summary>
    None = 0x0000,
    /// <summary>Alt key.</summary>
    Alt = 0x0001,
    /// <summary>Control key.</summary>
    Control = 0x0002,
    /// <summary>Shift key.</summary>
    Shift = 0x0004,
    /// <summary>Windows key.</summary>
    Win = 0x0008,
    /// <summary>Do not generate repeats.</summary>
    NoRepeat = 0x4000
}
