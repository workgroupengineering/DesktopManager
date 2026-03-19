using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides methods for pasting or typing text into windows.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowInputService {
    /// <summary>
    /// Pastes the specified text into the window using the clipboard.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to paste.</param>
    public static void PasteText(WindowInfo window, string text) {
        PasteText(window, text, null);
    }

    /// <summary>
    /// Pastes the specified text into the window using the clipboard and options.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to paste.</param>
    /// <param name="options">Input options.</param>
    public static void PasteText(WindowInfo window, string text, WindowInputOptions? options) {
        ValidateWindow(window);
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = options ?? new WindowInputOptions();
        NormalizeOptions(settings);

        IntPtr previousForeground = IntPtr.Zero;
        if (settings.ActivateWindow || settings.RestoreFocus) {
            previousForeground = MonitorNativeMethods.GetForegroundWindow();
        }

        string? clipboardBackup = null;
        bool restoreClipboard = false;
        if (settings.PreserveClipboard) {
            restoreClipboard = ClipboardHelper.TryGetText(out clipboardBackup, settings.ClipboardRetryCount, settings.ClipboardRetryDelayMilliseconds);
        }

        ClipboardHelper.SetText(text, settings.ClipboardRetryCount, settings.ClipboardRetryDelayMilliseconds);

        if (settings.ActivateWindow) {
            TryActivateWindow(window.Handle, settings.ActivationRetryCount, settings.ActivationRetryDelayMilliseconds);
        }

        SendPaste(window.Handle, settings.InputRetryCount, settings.ActivationRetryDelayMilliseconds);
        EnsureTextApplied(window, text);

        if (settings.RestoreFocus && previousForeground != IntPtr.Zero && previousForeground != window.Handle) {
            MonitorNativeMethods.SetForegroundWindow(previousForeground);
        }

        if (settings.PreserveClipboard && restoreClipboard) {
            ClipboardHelper.SetText(clipboardBackup ?? string.Empty, settings.ClipboardRetryCount, settings.ClipboardRetryDelayMilliseconds);
        }
    }

    /// <summary>
    /// Types the specified text into the window using simulated keyboard input.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="delay">Optional delay in milliseconds between characters.</param>
    public static void TypeText(WindowInfo window, string text, int delay = 0) {
        var options = new WindowInputOptions {
            KeyDelayMilliseconds = delay
        };

        TypeText(window, text, options);
    }

    /// <summary>
    /// Types the specified text into the window using options.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="options">Input options.</param>
    public static void TypeText(WindowInfo window, string text, WindowInputOptions? options) {
        ValidateWindow(window);
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = options ?? new WindowInputOptions();
        NormalizeOptions(settings);

        IntPtr previousForeground = IntPtr.Zero;
        if (settings.ActivateWindow || settings.RestoreFocus) {
            previousForeground = MonitorNativeMethods.GetForegroundWindow();
        }

        if (settings.ActivateWindow) {
            TryActivateWindow(window.Handle, settings.ActivationRetryCount, settings.ActivationRetryDelayMilliseconds);
        }

        if (settings.UseSendInput) {
            SendInputText(text, settings);
        } else {
            SendMessageText(window.Handle, text, settings.KeyDelayMilliseconds);
        }

        EnsureTextApplied(window, text);

        if (settings.RestoreFocus && previousForeground != IntPtr.Zero && previousForeground != window.Handle) {
            MonitorNativeMethods.SetForegroundWindow(previousForeground);
        }
    }

    private static void ValidateWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }
        if (window.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(window));
        }
    }

    private static void NormalizeOptions(WindowInputOptions options) {
        if (options.ClipboardRetryCount < 1) {
            options.ClipboardRetryCount = 1;
        }
        if (options.ClipboardRetryDelayMilliseconds < 0) {
            options.ClipboardRetryDelayMilliseconds = 0;
        }
        if (options.ActivationRetryCount < 1) {
            options.ActivationRetryCount = 1;
        }
        if (options.ActivationRetryDelayMilliseconds < 0) {
            options.ActivationRetryDelayMilliseconds = 0;
        }
        if (options.InputRetryCount < 1) {
            options.InputRetryCount = 1;
        }
        if (options.KeyDelayMilliseconds < 0) {
            options.KeyDelayMilliseconds = 0;
        }
    }

    private static void TryActivateWindow(IntPtr handle, int retryCount, int retryDelayMilliseconds) {
        for (int attempt = 0; attempt < retryCount; attempt++) {
            if (MonitorNativeMethods.SetForegroundWindow(handle)) {
                return;
            }

            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }
    }

    private static void SendPaste(IntPtr handle, int retryCount, int retryDelayMilliseconds) {
        for (int attempt = 0; attempt < retryCount; attempt++) {
            MonitorNativeMethods.SendMessage(handle, MonitorNativeMethods.WM_PASTE, 0, 0);
            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }
    }

    private static void SendMessageText(IntPtr handle, string text, int delayMilliseconds) {
        foreach (char c in text) {
            MonitorNativeMethods.SendMessage(handle, MonitorNativeMethods.WM_CHAR, (uint)c, 0);
            if (delayMilliseconds > 0) {
                Thread.Sleep(delayMilliseconds);
            }
        }
    }

    private static void SendInputText(string text, WindowInputOptions options) {
        foreach (char c in text) {
            MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];

            inputs[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = 0,
                Scan = c,
                Flags = MonitorNativeMethods.KEYEVENTF_UNICODE,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            inputs[1].Type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[1].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
                Vk = 0,
                Scan = c,
                Flags = MonitorNativeMethods.KEYEVENTF_UNICODE | MonitorNativeMethods.KEYEVENTF_KEYUP,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            for (int attempt = 0; attempt < options.InputRetryCount; attempt++) {
                uint sent = MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
                if (sent == inputs.Length) {
                    break;
                }
                if (attempt < options.InputRetryCount - 1 && options.ActivationRetryDelayMilliseconds > 0) {
                    Thread.Sleep(options.ActivationRetryDelayMilliseconds);
                }
            }

            if (options.KeyDelayMilliseconds > 0) {
                Thread.Sleep(options.KeyDelayMilliseconds);
            }
        }
    }

    private static void EnsureTextApplied(WindowInfo window, string text) {
        WindowControlInfo? editable = FindPreferredEditableControl(window.Handle);
        if (editable == null) {
            return;
        }

        string current = WindowTextHelper.GetWindowText(editable.Handle);
        if (string.Equals(current, text, StringComparison.Ordinal)) {
            return;
        }

        MonitorNativeMethods.SendMessage(editable.Handle, MonitorNativeMethods.WM_SETTEXT, IntPtr.Zero, text);
    }

    private static WindowControlInfo? FindPreferredEditableControl(IntPtr windowHandle) {
        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> controls = enumerator.EnumerateControls(windowHandle);

        return controls.Find(control => control.ClassName.Equals("RichEditD2DPT", StringComparison.OrdinalIgnoreCase))
            ?? controls.Find(control => control.ClassName.Equals("NotepadTextBox", StringComparison.OrdinalIgnoreCase))
            ?? controls.Find(control => control.ClassName.IndexOf("RichEdit", StringComparison.OrdinalIgnoreCase) >= 0)
            ?? controls.Find(control => control.ClassName.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

