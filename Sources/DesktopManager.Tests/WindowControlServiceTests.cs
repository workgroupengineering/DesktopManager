using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlServiceTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    [Ignore("Disabled: UI test with Notepad - window enumeration needs fixing")]
    public void ControlClick_CancelButton_ClosesDialog() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        if (TestHelper.ShouldSkipUITests()) {
            Assert.Inconclusive("UI tests skipped in local development. Set RUN_UI_TESTS=true to run.");
        }

        Process? process = null;
        
        try {
            process = TestHelper.StartHiddenNotepad();
            if (process == null) {
                Assert.Inconclusive("Failed to start Notepad");
            }

            var manager = new WindowManager();
            var window = manager.WaitWindow("*Notepad*", 10000);
            KeyboardInputService.PressShortcut(0, VirtualKey.VK_CONTROL, VirtualKey.VK_O);
            var dialog = manager.WaitWindow("*Open*", 5000);

            var enumerator = new ControlEnumerator();
            var controls = enumerator.EnumerateControls(dialog.Handle);
            var cancel = controls.FirstOrDefault(c => c.Text == "Cancel");
            Assert.IsNotNull(cancel, "Cancel control not found");

            WindowControlService.ControlClick(cancel, MouseButton.Left);
            Thread.Sleep(500);
            var openDialogs = manager.GetWindows("*Open*");
            Assert.AreEqual(0, openDialogs.Count, "Dialog was not closed");
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }
}
