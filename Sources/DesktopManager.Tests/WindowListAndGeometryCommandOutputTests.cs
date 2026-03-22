#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window list and geometry text output.
/// </summary>
public class WindowListAndGeometryCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures window list output renders the expected table headers and row values.
    /// </summary>
    public void WriteWindowListResults_WritesWindowTable() {
        var windows = new List<global::DesktopManager.Cli.WindowResult> {
            new() {
                ProcessId = 321,
                Handle = "0x1234",
                MonitorIndex = 1,
                IsVisible = true,
                State = "Normal",
                Title = "DesktopManager Test App"
            },
            new() {
                ProcessId = 654,
                Handle = "0xABCD",
                MonitorIndex = 2,
                IsVisible = false,
                State = "Minimized",
                Title = "DesktopManager Command Surface"
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWindowListResults(windows, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "PID");
        StringAssert.Contains(output, "Handle");
        StringAssert.Contains(output, "Mon");
        StringAssert.Contains(output, "Visible");
        StringAssert.Contains(output, "State");
        StringAssert.Contains(output, "Title");
        StringAssert.Contains(output, "321");
        StringAssert.Contains(output, "1234");
        StringAssert.Contains(output, "1");
        StringAssert.Contains(output, "Yes");
        StringAssert.Contains(output, "Normal");
        StringAssert.Contains(output, "DesktopManager Test App");
        StringAssert.Contains(output, "654");
        StringAssert.Contains(output, "ABCD");
        StringAssert.Contains(output, "No");
        StringAssert.Contains(output, "Minimized");
        StringAssert.Contains(output, "DesktopManager Command Surface");
    }

    [TestMethod]
    /// <summary>
    /// Ensures geometry output renders window bounds, client bounds, and client offsets.
    /// </summary>
    public void WriteWindowGeometryResults_WritesGeometrySummary() {
        var geometries = new List<global::DesktopManager.Cli.WindowGeometryResult> {
            new() {
                Window = new global::DesktopManager.Cli.WindowResult {
                    Title = "DesktopManager Test App",
                    Handle = "0x1234"
                },
                WindowLeft = 10,
                WindowTop = 20,
                WindowWidth = 800,
                WindowHeight = 600,
                ClientLeft = 18,
                ClientTop = 52,
                ClientWidth = 784,
                ClientHeight = 560,
                ClientOffsetLeft = 8,
                ClientOffsetTop = 32
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteWindowGeometryResults(geometries, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "window: DesktopManager Test App (0x1234)");
        StringAssert.Contains(output, "- Window: 10,20 800x600");
        StringAssert.Contains(output, "- Client: 18,52 784x560");
        StringAssert.Contains(output, "- ClientOffset: 8,32");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the empty window message stays explicit for list and geometry callers.
    /// </summary>
    public void WriteNoMatchingWindows_WritesExpectedMessage() {
        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.WindowCommands.WriteNoMatchingWindows(writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "No matching windows found.");
    }
}
#endif
