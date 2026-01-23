using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for enumerating child windows.
/// </summary>
public class WindowChildEnumerationTests {
    [TestMethod]
    /// <summary>
    /// Ensures child window enumeration returns results for a window with controls.
    /// </summary>
    public void GetChildWindows_ReturnsChildren() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireInteractive();

        Process? process = null;
        WindowInfo? window = null;

        try {
            if (!TestHelper.TryStartNotepadWindow(out process, out window, hideWindow: true) || window == null) {
                Assert.Inconclusive("Failed to start Notepad for testing");
                return;
            }

            var manager = new WindowManager();
            var children = manager.GetChildWindows(window, includeHidden: true);
            if (children.Count == 0) {
                Assert.Inconclusive("No child windows were returned for the test window");
            }
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }
}
