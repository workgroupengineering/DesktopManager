using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides methods for simulating keyboard input.
/// </summary>
[SupportedOSPlatform("windows")]
public static class KeyboardInputService {
    /// <summary>
    /// Presses a single key by sending a down and up event.
    /// </summary>
    /// <param name="key">Key to press.</param>
    public static void PressKey(VirtualKey key) {
        KeyDown(key);
        KeyUp(key);
    }

    /// <summary>
    /// Sends a key down event for the specified key.
    /// </summary>
    /// <param name="key">Key to press down.</param>
    public static void KeyDown(VirtualKey key) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = 0,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput(1, [input], Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Sends a key up event for the specified key.
    /// </summary>
    /// <param name="key">Key to release.</param>
    public static void KeyUp(VirtualKey key) {
        MonitorNativeMethods.INPUT input = new();
        input.Type = MonitorNativeMethods.INPUT_KEYBOARD;
        input.Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = (ushort)key,
            Scan = 0,
            Flags = MonitorNativeMethods.KEYEVENTF_KEYUP,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };
        MonitorNativeMethods.SendInput(1, [input], Marshal.SizeOf<MonitorNativeMethods.INPUT>());
    }

    /// <summary>
    /// Presses a shortcut combination of keys.
    /// </summary>
    /// <param name="delay">Delay in milliseconds between each key event.</param>
    /// <param name="keys">Keys to press in order.</param>
    public static void PressShortcut(int delay, params VirtualKey[] keys) {
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        foreach (VirtualKey key in keys) {
            KeyDown(key);
            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }

        for (int i = keys.Length - 1; i >= 0; i--) {
            KeyUp(keys[i]);
            if (delay > 0) {
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Sends one or more keys to the current foreground target using SendInput.
    /// </summary>
    /// <param name="keys">Keys to send.</param>
    public static void SendToForeground(params VirtualKey[] keys) {
        if (keys == null || keys.Length == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        var heldModifiers = new List<VirtualKey>();
        for (int index = 0; index < keys.Length; index++) {
            VirtualKey key = keys[index];
            bool hasTrailingKey = index < keys.Length - 1;
            if (IsModifierKey(key) && hasTrailingKey) {
                KeyDown(key);
                heldModifiers.Add(key);
                continue;
            }

            PressKey(key);
            ReleaseHeldModifiers(heldModifiers);
        }

        ReleaseHeldModifiers(heldModifiers);
    }

    /// <summary>
    /// Sends Unicode text to the current foreground target using SendInput.
    /// </summary>
    /// <param name="text">Text to send.</param>
    /// <param name="delayMilliseconds">Optional delay between characters.</param>
    public static void SendTextToForeground(string text, int delayMilliseconds = 0) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        if (delayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds), "delayMilliseconds must be zero or greater.");
        }

        foreach (char character in text) {
            MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];

            inputs[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = 0,
                Scan = character,
                Flags = MonitorNativeMethods.KEYEVENTF_UNICODE,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            inputs[1].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[1].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = 0,
                Scan = character,
                Flags = MonitorNativeMethods.KEYEVENTF_UNICODE | MonitorNativeMethods.KEYEVENTF_KEYUP,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
            if (delayMilliseconds > 0) {
                Thread.Sleep(delayMilliseconds);
            }
        }
    }

    /// <summary>
    /// Sends key presses directly to a background control.
    /// </summary>
    /// <param name="control">Target control.</param>
    /// <param name="keys">Keys to send.</param>
    public static void SendToControl(WindowControlInfo control, params VirtualKey[] keys) {
        WindowControlService.SendKeys(control, keys);
    }

    private static bool IsModifierKey(VirtualKey key) {
        return key == VirtualKey.VK_CONTROL ||
            key == VirtualKey.VK_LCONTROL ||
            key == VirtualKey.VK_RCONTROL ||
            key == VirtualKey.VK_SHIFT ||
            key == VirtualKey.VK_LSHIFT ||
            key == VirtualKey.VK_RSHIFT ||
            key == VirtualKey.VK_MENU ||
            key == VirtualKey.VK_LMENU ||
            key == VirtualKey.VK_RMENU ||
            key == VirtualKey.VK_LWIN ||
            key == VirtualKey.VK_RWIN;
    }

    private static void ReleaseHeldModifiers(List<VirtualKey> heldModifiers) {
        for (int index = heldModifiers.Count - 1; index >= 0; index--) {
            KeyUp(heldModifiers[index]);
        }

        heldModifiers.Clear();
    }
}
