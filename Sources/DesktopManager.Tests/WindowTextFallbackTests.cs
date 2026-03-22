using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for cross-bitness window title retrieval.
/// </summary>
public class WindowTextFallbackTests {
    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
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

        TestHelper.RequireOwnedWindowUiTests();
        string title = $"Window Text Fallback Harness {Guid.NewGuid():N}";
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create(title);

        string expected = title;

        string? deploymentDir = TestContext?.DeploymentDirectory;
        if (string.IsNullOrEmpty(deploymentDir)) {
            deploymentDir = AppContext.BaseDirectory;
        }

        string helperDir = Path.Combine(deploymentDir, "WindowTextHelper32");
        string helperPath = Path.Combine(helperDir, "WindowTextHelper32.exe");
        if (!File.Exists(helperPath)) {
            helperDir = Path.Combine(AppContext.BaseDirectory, "WindowTextHelper32");
            helperPath = Path.Combine(helperDir, "WindowTextHelper32.exe");
        }
        if (!File.Exists(helperPath)) {
            Assert.Inconclusive("Helper executable not found");
        }

        using var helper = Process.Start(new ProcessStartInfo(helperPath, harness.Window.Handle.ToInt64().ToString()) {
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
    }
}
