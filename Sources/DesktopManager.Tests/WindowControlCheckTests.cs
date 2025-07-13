using System;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Windows.Forms;
#endif

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlCheckTests {
    [TestMethod]
    public void GetAndSetCheckState_Toggles() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
#if NETFRAMEWORK
        using Form form = new();
        using CheckBox box = new() { Text = "Sample" };
        form.Controls.Add(box);
        form.Show();
        box.CreateControl();

        WindowControlInfo info = new() {
            Handle = box.Handle,
            ClassName = "Button",
            Id = MonitorNativeMethods.GetDlgCtrlID(box.Handle),
            Text = box.Text
        };

        Assert.IsFalse(WindowControlService.GetCheckState(info));
        WindowControlService.SetCheckState(info, true);
        Assert.IsTrue(WindowControlService.GetCheckState(info));
        WindowControlService.SetCheckState(info, false);
        Assert.IsFalse(WindowControlService.GetCheckState(info));
        form.Close();
#else
        Assert.Inconclusive("Test only runs on .NET Framework");
#endif
    }
}
