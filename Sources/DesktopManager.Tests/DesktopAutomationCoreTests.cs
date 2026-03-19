using System;
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared desktop automation helpers.
/// </summary>
public class DesktopAutomationCoreTests {
    [TestMethod]
    /// <summary>
    /// Ensures hexadecimal handles parse correctly.
    /// </summary>
    public void DesktopHandleParser_ParseHexValue_ReturnsHandle() {
        IntPtr handle = DesktopHandleParser.Parse("0x1A2B");

        Assert.AreEqual(new IntPtr(0x1A2B), handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures decimal handles parse correctly.
    /// </summary>
    public void DesktopHandleParser_ParseDecimalValue_ReturnsHandle() {
        IntPtr handle = DesktopHandleParser.Parse("6699");

        Assert.AreEqual(new IntPtr(6699), handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid handles are rejected.
    /// </summary>
    public void DesktopHandleParser_ParseInvalidValue_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => DesktopHandleParser.Parse("not-a-handle"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot paths are normalized and get a png extension.
    /// </summary>
    public void DesktopStateStore_ResolveCapturePath_AppendsPngExtension() {
        string testRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string requestedPath = Path.Combine(testRoot, "capture-output");

        try {
            string resolvedPath = DesktopStateStore.ResolveCapturePath("desktop", requestedPath);
            string expectedPath = Path.GetFullPath(requestedPath + ".png");

            Assert.AreEqual(expectedPath, resolvedPath, true);
            Assert.AreEqual(".png", Path.GetExtension(resolvedPath), true);
            Assert.IsTrue(Directory.Exists(testRoot));
        } finally {
            if (Directory.Exists(testRoot)) {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }
}
