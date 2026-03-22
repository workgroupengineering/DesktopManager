#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI control list text output.
/// </summary>
public class ControlListCommandOutputTests {
    [TestMethod]
    /// <summary>
    /// Ensures control list output renders the expected table headers and row values.
    /// </summary>
    public void WriteControlListResults_WritesControlTable() {
        var controls = new List<global::DesktopManager.Cli.ControlResult> {
            new() {
                Id = 101,
                Handle = "0x1234",
                Source = "Win32",
                ControlType = "Edit",
                Left = 10,
                Top = 20,
                Width = 300,
                Height = 24,
                IsEnabled = true,
                IsKeyboardFocusable = true,
                IsOffscreen = false,
                SupportsBackgroundClick = true,
                SupportsBackgroundText = true,
                SupportsBackgroundKeys = false,
                SupportsForegroundInputFallback = false,
                AutomationId = "EditorTextBox",
                ClassName = "Edit",
                Text = "DesktopManager",
                Value = "DesktopManager",
                ParentWindow = new global::DesktopManager.Cli.WindowResult {
                    ProcessId = 321,
                    Title = "DesktopManager Test App"
                }
            },
            new() {
                Id = 0,
                Handle = "0x0",
                Source = "UiAutomation",
                ControlType = "Button",
                Left = 400,
                Top = 40,
                Width = 90,
                Height = 30,
                IsEnabled = true,
                IsKeyboardFocusable = true,
                IsOffscreen = false,
                SupportsBackgroundClick = false,
                SupportsBackgroundText = false,
                SupportsBackgroundKeys = false,
                SupportsForegroundInputFallback = true,
                AutomationId = "SaveButton",
                ClassName = "Button",
                Text = "Save",
                Value = string.Empty,
                ParentWindow = new global::DesktopManager.Cli.WindowResult {
                    ProcessId = 654,
                    Title = "DesktopManager Command Surface"
                }
            }
        };

        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteControlListResults(controls, writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "PID");
        StringAssert.Contains(output, "Id");
        StringAssert.Contains(output, "Handle");
        StringAssert.Contains(output, "Source");
        StringAssert.Contains(output, "Type");
        StringAssert.Contains(output, "BgClick");
        StringAssert.Contains(output, "BgText");
        StringAssert.Contains(output, "BgKeys");
        StringAssert.Contains(output, "FgFallback");
        StringAssert.Contains(output, "AutomationId");
        StringAssert.Contains(output, "Class");
        StringAssert.Contains(output, "Text");
        StringAssert.Contains(output, "Value");
        StringAssert.Contains(output, "Window");
        StringAssert.Contains(output, "321");
        StringAssert.Contains(output, "101");
        StringAssert.Contains(output, "1234");
        StringAssert.Contains(output, "Win32");
        StringAssert.Contains(output, "Edit");
        StringAssert.Contains(output, "EditorTextBox");
        StringAssert.Contains(output, "DesktopManager Test App");
        StringAssert.Contains(output, "654");
        StringAssert.Contains(output, "0");
        StringAssert.Contains(output, "UiAutomation");
        StringAssert.Contains(output, "Button");
        StringAssert.Contains(output, "SaveButton");
        StringAssert.Contains(output, "DesktopManager Command Surface");
    }

    [TestMethod]
    /// <summary>
    /// Ensures the empty control-list message stays explicit.
    /// </summary>
    public void WriteNoMatchingControls_WritesExpectedMessage() {
        using var writer = new StringWriter();

        int exitCode = global::DesktopManager.Cli.ControlCommands.WriteNoMatchingControls(writer);
        string output = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "No matching controls found.");
    }
}
#endif
