using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for cross-bitness window title retrieval.
/// </summary>
public class WindowTextFallbackTests {
    public TestContext TestContext { get; set; }

    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    [Ignore("Disabled: UI test with Notepad - window enumeration needs fixing")]
    /// <summary>
    /// Ensures SendMessageTimeout fallback works when bitness differs.
    /// </summary>
    public void GetWindowText_Fallback_WorksAcrossBitness() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        if (!Environment.Is64BitProcess) {
            Assert.Inconclusive("Test requires 64-bit process");
        }

        if (TestHelper.ShouldSkipUITests()) {
            Assert.Inconclusive("UI tests skipped in local development. Set RUN_UI_TESTS=true to run.");
        }
        
        using var proc = TestHelper.StartHiddenNotepad();
        if (proc == null) {
            Assert.Inconclusive("Failed to start notepad");
        }

        try {
            var manager = new WindowManager();
            var window = manager.GetWindows(processId: proc.Id).First();
            string expected = window.Title;

            string helperDir = Path.Combine(TestContext.DeploymentDirectory, "WindowTextHelper32");
            string helperPath = Path.Combine(helperDir, "WindowTextHelper32.exe");
            if (!File.Exists(helperPath)) {
                helperDir = Path.Combine(AppContext.BaseDirectory, "WindowTextHelper32");
                helperPath = Path.Combine(helperDir, "WindowTextHelper32.exe");
            }
            if (!File.Exists(helperPath)) {
                Assert.Inconclusive("Helper executable not found");
            }

            using var helper = Process.Start(new ProcessStartInfo(helperPath, window.Handle.ToInt64().ToString()) {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = helperDir
            });
            if (helper == null) {
                Assert.Inconclusive("Failed to start helper");
            }

            string output = helper.StandardOutput.ReadToEnd().Trim();
            helper.WaitForExit();

            Assert.AreEqual(expected, output);
        } finally {
            TestHelper.SafeKillProcess(proc);
        }
    }
}
