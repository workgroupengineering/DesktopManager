#if NET8_0_OR_GREATER
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI screenshot text output.
/// </summary>
public class ScreenshotCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures screenshot text output includes window, client geometry, and monitor details when available.
    /// </summary>
    public void WriteScreenshotResult_WritesRichScreenshotSummary() {
        var result = new global::DesktopManager.Cli.ScreenshotResult {
            Kind = "window-target",
            Path = @"C:\Temp\editor-target.png",
            Width = 320,
            Height = 180,
            MonitorIndex = 1,
            Window = new global::DesktopManager.Cli.WindowResult {
                Title = "DesktopManager Test App"
            },
            Geometry = new global::DesktopManager.Cli.WindowGeometryResult {
                ClientWidth = 784,
                ClientHeight = 560,
                ClientOffsetLeft = 8,
                ClientOffsetTop = 32
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ScreenshotCommands.WriteScreenshotResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "screenshot: window-target");
        StringAssert.Contains(output, "- Path: C:\\Temp\\editor-target.png");
        StringAssert.Contains(output, "- Size: 320x180");
        StringAssert.Contains(output, "- Window: DesktopManager Test App");
        StringAssert.Contains(output, "- Client: 784x560 at offset 8,32");
        StringAssert.Contains(output, "- Monitor: 1");
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot text output omits optional sections when only the basic capture result is available.
    /// </summary>
    public void WriteScreenshotResult_OmitsOptionalSectionsWhenUnset() {
        var result = new global::DesktopManager.Cli.ScreenshotResult {
            Kind = "desktop",
            Path = @"C:\Temp\desktop.png",
            Width = 1920,
            Height = 1080
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ScreenshotCommands.WriteScreenshotResult(result, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "screenshot: desktop");
        StringAssert.Contains(output, "- Path: C:\\Temp\\desktop.png");
        StringAssert.Contains(output, "- Size: 1920x1080");
        Assert.IsFalse(output.Contains("- Window:"));
        Assert.IsFalse(output.Contains("- Client:"));
        Assert.IsFalse(output.Contains("- Monitor:"));
    }
}
#endif
