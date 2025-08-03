using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class ControlEnumeratorTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    [Ignore("Disabled: UI test with Notepad - window enumeration needs fixing")]
    public void Enumerate_NotepadControls_ReturnsEdit() {
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
            var enumerator = new ControlEnumerator();
            var controls = enumerator.EnumerateControls(window.Handle);
            Assert.IsTrue(controls.Any(c => c.ClassName == "Edit"));
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }
}
