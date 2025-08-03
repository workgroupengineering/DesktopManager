using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Manages global hotkey registrations.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HotkeyService : IDisposable {
    private static readonly Lazy<HotkeyService> _instance = new(() => new HotkeyService());

    /// <summary>Gets the shared instance.</summary>
    public static HotkeyService Instance => _instance.Value;

    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId;
    private IntPtr _hwnd;
    private Thread? _thread;
    private MonitorNativeMethods.WndProc? _wndProc;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Queue<Action> _actions = new();
    private const uint WM_RUN = MonitorNativeMethods.WM_APP + 1;

    private HotkeyService() {
        _thread = new Thread(MessageLoop) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait();
    }

    private void Invoke(Action action) {
        if (_thread == null) {
            throw new ObjectDisposedException(nameof(HotkeyService));
        }

        if (Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId) {
            action();
            return;
        }

        using var done = new ManualResetEventSlim(false);
        Exception? ex = null;
        lock (_actions) {
            _actions.Enqueue(() => {
                try {
                    action();
                } catch (Exception e) {
                    ex = e;
                } finally {
                    done.Set();
                }
            });
        }
        MonitorNativeMethods.PostMessage(_hwnd, WM_RUN, IntPtr.Zero, IntPtr.Zero);
        done.Wait();
        if (ex != null) {
            throw;
        }
    }

    /// <summary>Window handle used for hotkey messages.</summary>
    internal IntPtr WindowHandle => _hwnd;

    /// <summary>
    /// Registers a hotkey and associates a callback.
    /// </summary>
    /// <param name="modifiers">Modifier keys.</param>
    /// <param name="key">Virtual key.</param>
    /// <param name="callback">Callback invoked when the hotkey fires.</param>
    /// <returns>Identifier of the registration.</returns>
    public int RegisterHotkey(HotkeyModifiers modifiers, VirtualKey key, Action callback) {
        if (callback == null) {
            throw new ArgumentNullException(nameof(callback));
        }

        int id = 0;
        Invoke(() => {
            id = ++_nextId;
            _callbacks[id] = callback;

            if (!MonitorNativeMethods.RegisterHotKey(_hwnd, id, (uint)modifiers, (uint)key)) {
                _callbacks.Remove(id);
                int error = Marshal.GetLastWin32Error();
                var ex = new System.ComponentModel.Win32Exception(error);
                throw new DesktopManagerException("RegisterHotKey", ex);
            }
        });

        return id;
    }

    /// <summary>
    /// Unregisters a previously registered hotkey.
    /// </summary>
    /// <param name="id">Identifier returned from <see cref="RegisterHotkey"/>.</param>
    public void UnregisterHotkey(int id) {
        Invoke(() => {
            MonitorNativeMethods.UnregisterHotKey(_hwnd, id);
            _callbacks.Remove(id);
        });
    }

    private void MessageLoop() {
        _wndProc = WndProc;
        _hwnd = MonitorNativeMethods.CreateWindowExW(0, "Message", string.Empty, 0, 0, 0, 0, 0,
            MonitorNativeMethods.HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        MonitorNativeMethods.SetWindowLongPtr(_hwnd, MonitorNativeMethods.GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProc));
        _ready.Set();

        MonitorNativeMethods.MSG msg;
        while (MonitorNativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0) != 0) {
            MonitorNativeMethods.TranslateMessage(ref msg);
            MonitorNativeMethods.DispatchMessage(ref msg);
        }

        if (_hwnd != IntPtr.Zero) {
            MonitorNativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
        if (msg == MonitorNativeMethods.WM_HOTKEY) {
            int id = wParam.ToInt32();
            Action? callback;
            lock (_callbacks) {
                _callbacks.TryGetValue(id, out callback);
            }
            callback?.Invoke();
            return IntPtr.Zero;
        }

        if (msg == WM_RUN) {
            while (true) {
                Action? next = null;
                lock (_actions) {
                    if (_actions.Count > 0) {
                        next = _actions.Dequeue();
                    }
                }
                if (next == null) {
                    break;
                }
                next();
            }
            return IntPtr.Zero;
        }

        return MonitorNativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_hwnd != IntPtr.Zero) {
            MonitorNativeMethods.PostMessage(_hwnd, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread?.Join();
        }
        _ready.Dispose();
    }
}
