using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class ControlEnumeratorTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    public void Enumerate_WinFormsControls_ReturnsEdit() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = "Control Enumerator Form", ShowInTaskbar = false };
        using TextBox textBox = new() { Text = "DesktopManager" };
        form.Controls.Add(textBox);
        form.Show();
        textBox.CreateControl();
        Application.DoEvents();

        var enumerator = new ControlEnumerator();
        var controls = enumerator.EnumerateControls(form.Handle);

        WindowControlInfo? textBoxControl = controls.FirstOrDefault(c => c.Handle == textBox.Handle);
        Assert.IsNotNull(textBoxControl);
        StringAssert.Contains(textBoxControl.ClassName, "EDIT", StringComparison.OrdinalIgnoreCase);
    }
}
