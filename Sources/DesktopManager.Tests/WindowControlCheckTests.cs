using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlCheckTests {
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const int BsAutoCheckBox = 0x00000003;

    [TestMethod]
    public void GetAndSetCheckState_Toggles() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Check State Harness");
        IntPtr checkBoxHandle = MonitorNativeMethods.CreateWindowExW(
            0,
            "Button",
            "Sample",
            WsChild | WsVisible | BsAutoCheckBox,
            10,
            10,
            120,
            24,
            harness.Form.Handle,
            new IntPtr(1001),
            IntPtr.Zero,
            IntPtr.Zero);
        if (checkBoxHandle == IntPtr.Zero) {
            Assert.Inconclusive("Failed to create a native checkbox control for testing.");
        }

        Application.DoEvents();
        Thread.Sleep(100);

        try {
            WindowControlInfo info = new() {
                ParentWindowHandle = harness.Form.Handle,
                Handle = checkBoxHandle,
                ClassName = "Button",
                Id = MonitorNativeMethods.GetDlgCtrlID(checkBoxHandle),
                Text = "Sample"
            };

            Assert.IsFalse(WindowControlService.GetCheckState(info));
            WindowControlService.SetCheckState(info, true);
            Application.DoEvents();
            Thread.Sleep(100);
            Assert.IsTrue(WindowControlService.GetCheckState(info));
            WindowControlService.SetCheckState(info, false);
            Application.DoEvents();
            Thread.Sleep(100);
            Assert.IsFalse(WindowControlService.GetCheckState(info));
        } finally {
            MonitorNativeMethods.DestroyWindow(checkBoxHandle);
        }
    }
}
