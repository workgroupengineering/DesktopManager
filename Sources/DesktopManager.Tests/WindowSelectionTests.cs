using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>Tests for window selection inversion.</summary>
public class WindowSelectionTests {
    [TestMethod]
    /// <summary>InvertWindowSelection toggles selected windows.</summary>
    public void InvertWindowSelection_TogglesState() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count < 2) {
            Assert.Inconclusive("Need at least two windows");
        }

        var h1 = (int)windows[0].Handle;
        var h2 = (int)windows[1].Handle;

        var result = manager.InvertWindowSelection(new[] { h1, h2 });
        CollectionAssert.AreEquivalent(new[] { h1, h2 }, result);

        result = manager.InvertWindowSelection(new[] { h1 });
        CollectionAssert.AreEquivalent(new[] { h2 }, result);
    }
}
