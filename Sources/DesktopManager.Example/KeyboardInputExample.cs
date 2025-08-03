using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopManager.Example;

/// <summary>
/// Demonstrates basic keyboard input using <see cref="KeyboardInputService"/>.
/// </summary>
internal static class KeyboardInputExample {
    /// <summary>Runs the keyboard input example.</summary>
    public static void Run() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("Keyboard input examples require Windows.");
            return;
        }

        Console.WriteLine("Pressing WIN+R to open Run dialog...");
        KeyboardInputService.PressShortcut(0, VirtualKey.VK_LWIN, VirtualKey.VK_R);

        Console.WriteLine("Pressing and releasing F24 using KeyDown/KeyUp...");
        KeyboardInputService.KeyDown(VirtualKey.VK_F24);
        KeyboardInputService.KeyUp(VirtualKey.VK_F24);

        Console.WriteLine("Typing 'HI' into a background Notepad window using SendToControl...");
        using var proc1 = Process.Start("notepad.exe");
        using var proc2 = Process.Start("notepad.exe");
        if (proc1 != null && proc2 != null) {
            proc1.WaitForInputIdle(2000);
            proc2.WaitForInputIdle(2000);

            var manager = new WindowManager();
            var win1 = manager.GetWindowsForProcess(proc1).First();
            var win2 = manager.GetWindowsForProcess(proc2).First();
            MonitorNativeMethods.SetForegroundWindow(win2.Handle);

            var enumerator = new ControlEnumerator();
            var ctrl = enumerator.EnumerateControls(win1.Handle).FirstOrDefault(c => c.ClassName == "Edit");
            if (ctrl != null) {
                KeyboardInputService.SendToControl(ctrl, VirtualKey.VK_H, VirtualKey.VK_I);
                Thread.Sleep(500);
            }
        }
    }
}
