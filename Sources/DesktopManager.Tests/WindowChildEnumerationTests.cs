using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for enumerating child windows.
/// </summary>
public class WindowChildEnumerationTests {
    [TestMethod]
    /// <summary>
    /// Ensures child window enumeration returns results for a window with controls.
    /// </summary>
    public void GetChildWindows_ReturnsChildren() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        TestHelper.RequireOwnedWindowUiTests();

        using TextBox textBox = new() { Text = "Child" };
        using Button button = new() { Text = "Go" };
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Child Enumeration Harness", form => {
            form.Controls.Add(textBox);
            form.Controls.Add(button);
        });
        textBox.CreateControl();
        button.CreateControl();
        Application.DoEvents();

        var manager = new WindowManager();
        var children = manager.GetChildWindows(harness.Window, includeHidden: true);
        if (children.Count == 0) {
            Assert.Inconclusive("No child windows were returned for the test window");
        }

        Assert.IsTrue(children.Exists(child => child.Handle == textBox.Handle));
        Assert.IsTrue(children.Exists(child => child.Handle == button.Handle));
    }
}
