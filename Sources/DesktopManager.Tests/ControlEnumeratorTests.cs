using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
public class ControlEnumeratorTests {
    [TestMethod]
    public void Enumerate_NotepadControls_ReturnsEdit() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        var process = Process.Start("notepad.exe");
        if (process == null) {
            Assert.Inconclusive("Failed to start Notepad");
        }

        try {
            var manager = new WindowManager();
            var window = manager.WaitWindow("*Notepad*", 10000);
            var enumerator = new ControlEnumerator();
            var controls = enumerator.EnumerateControls(window.Handle);
            Assert.IsTrue(controls.Any(c => c.ClassName == "Edit"));
        } finally {
            if (process != null && !process.HasExited) {
                process.Kill();
            }
        }
    }
}
