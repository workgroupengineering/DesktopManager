using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for window process utility APIs.
/// </summary>
public class WindowProcessInfoTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures window process info returns stable details.
    /// </summary>
    public void GetWindowProcessInfo_ReturnsDetails() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireInteractive();

        if (!TestHelper.TryStartNotepadWindow(out var process, out var window, hideWindow: true) ||
            process == null ||
            window == null) {
            Assert.Inconclusive("Failed to start Notepad window");
        }

        try {
            var manager = new WindowManager();
            var info = manager.GetWindowProcessInfo(window);

            Assert.AreEqual((uint)process.Id, info.ProcessId);
            Assert.IsTrue(info.ThreadId > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(info.ProcessName));
            Assert.IsTrue(info.ProcessPath != null &&
                          info.ProcessPath.EndsWith("notepad.exe", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(info.IsElevated.HasValue);
            Assert.IsTrue(info.IsWow64.HasValue);

            var threadId = manager.GetWindowThreadId(window);
            Assert.AreEqual(info.ThreadId, threadId);

            var modulePath = manager.GetWindowModulePath(window);
            Assert.AreEqual(info.ProcessPath, modulePath);
        } finally {
            TestHelper.SafeKillProcess(process);
        }
    }
}
