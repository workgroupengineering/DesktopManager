using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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

        TestHelper.RequireOwnedWindowUiTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Process Info Harness");

        var manager = new WindowManager();
        var info = manager.GetWindowProcessInfo(harness.Window);
        using var currentProcess = Process.GetCurrentProcess();

        Assert.AreEqual((uint)currentProcess.Id, info.ProcessId);
        Assert.IsTrue(info.ThreadId > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(info.ProcessName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(info.ProcessPath));
        Assert.IsTrue(info.IsElevated.HasValue);
        Assert.IsTrue(info.IsWow64.HasValue);

        var threadId = manager.GetWindowThreadId(harness.Window);
        Assert.AreEqual(info.ThreadId, threadId);

        var modulePath = manager.GetWindowModulePath(harness.Window);
        Assert.AreEqual(info.ProcessPath, modulePath);
    }
}
