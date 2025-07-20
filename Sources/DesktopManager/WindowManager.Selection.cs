using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager {
    private readonly HashSet<IntPtr> _selectedWindows = new();

    /// <summary>
    /// Inverts the selection state of the specified windows and returns the new selection.
    /// </summary>
    /// <param name="handles">Window handles to toggle.</param>
    /// <returns>Array of handles currently selected.</returns>
    public int[] InvertWindowSelection(int[] handles) {
        if (handles == null) {
            throw new ArgumentNullException(nameof(handles));
        }

        foreach (var h in handles) {
            var ptr = new IntPtr(h);
            if (!_selectedWindows.Add(ptr)) {
                _selectedWindows.Remove(ptr);
            }
        }

        return _selectedWindows.Select(h => (int)h).ToArray();
    }
}
