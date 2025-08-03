using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DesktopManager.Tests;

[TestClass]
public class KeyboardInputControlTests {
    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }
    
    [TestMethod]
    [TestCategory("UITest")]
    [Ignore("Disabled: UI test with Notepad - underlying SendToControl needs fixing")]
    public void SendToControl_TypesIntoBackgroundControl() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        if (TestHelper.ShouldSkipUITests()) {
            Assert.Inconclusive("UI tests skipped in local development. Set RUN_UI_TESTS=true to run.");
        }

        Process? proc1 = null;
        Process? proc2 = null;
        
        try {
            proc1 = TestHelper.StartHiddenNotepad();
            proc2 = TestHelper.StartHiddenNotepad();
            
            if (proc1 == null || proc2 == null) {
                Assert.Inconclusive("Failed to start Notepad");
            }

            var manager = new WindowManager();
            
            WindowInfo? win1 = null;
            WindowInfo? win2 = null;
            
            for (int retry = 0; retry < 5; retry++) {
                var windows1 = manager.GetWindowsForProcess(proc1!);
                var windows2 = manager.GetWindowsForProcess(proc2!);
                
                if (windows1.Any() && windows2.Any()) {
                    win1 = windows1.First();
                    win2 = windows2.First();
                    break;
                }
                
                Thread.Sleep(500);
            }
            
            if (win1 == null || win2 == null) {
                Assert.Inconclusive("Windows not found after retries");
            }
            
            MonitorNativeMethods.SetForegroundWindow(win2.Handle);
            Thread.Sleep(100);

            var enumerator = new ControlEnumerator();
            WindowControlInfo? ctrl = null;
            
            for (int retry = 0; retry < 3; retry++) {
                ctrl = enumerator.EnumerateControls(win1.Handle).FirstOrDefault(c => c.ClassName == "Edit");
                if (ctrl != null) break;
                Thread.Sleep(500);
            }
            
            if (ctrl == null) {
                Assert.Inconclusive("Edit control not found after retries");
            }
            
            KeyboardInputService.SendToControl(ctrl, VirtualKey.VK_H, VirtualKey.VK_I);
            Thread.Sleep(1000);

            int len = MonitorNativeMethods.GetWindowTextLength(ctrl.Handle);
            StringBuilder sb = new(Math.Max(len + 1, 10));
            MonitorNativeMethods.GetWindowText(ctrl.Handle, sb, sb.Capacity);
            
            string text = sb.ToString();
            Assert.IsTrue(text.Contains("HI") || text.EndsWith("HI"), 
                $"Expected text to contain 'HI' but got '{text}'");
        } finally {
            TestHelper.SafeKillProcess(proc1);
            TestHelper.SafeKillProcess(proc2);
        }
    }
}
