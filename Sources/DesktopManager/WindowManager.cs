using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DesktopManager;

/// <summary>
/// Provides methods to manage windows, including getting window information and controlling window states.
/// </summary>
public partial class WindowManager {
        private readonly Monitors _monitors;

        /// <summary>
        /// Initializes a new instance of the WindowManager class.
        /// </summary>
        public WindowManager() {
            _monitors = new Monitors();
            SnapOptions = new WindowSnapOptions();
        }

        /// <summary>
        /// Gets all visible windows, optionally filtered by title, process name,
        /// class name, regular expression or process ID.
        /// </summary>
        /// <param name="name">Optional window title filter. Supports wildcards.</param>
        /// <param name="processName">Optional process name filter. Supports wildcards.</param>
        /// <param name="className">Optional window class name filter. Supports wildcards.</param>
        /// <param name="regex">Optional regular expression to match the window title.</param>
        /// <param name="processId">Optional process ID filter.</param>
        /// <returns>A list of WindowInfo objects.</returns>
        public List<WindowInfo> GetWindows(string name = "*", string processName = "*", string className = "*", Regex regex = null, int processId = 0, bool includeHidden = false) {
            var handles = new List<IntPtr>();
            var shellWindowhWnd = MonitorNativeMethods.GetShellWindow();

            if (!MonitorNativeMethods.EnumWindows(
                (handle, lParam) => {
                    if (handle != shellWindowhWnd) {
                        if (includeHidden || MonitorNativeMethods.IsWindowVisible(handle)) {
                            handles.Add(handle);
                        }
                    }
                    return true;
                }, IntPtr.Zero)) {
                throw new InvalidOperationException("Failed to enumerate windows");
            }

            var windows = new List<WindowInfo>();
            foreach (var handle in handles) {
                var title = WindowTextHelper.GetWindowText(handle);
                
                // For process-specific queries, include windows even with empty titles
                // For name-based queries, skip empty titles unless using wildcard "*"
                bool shouldInclude = processId > 0 || 
                                    (name == "*" && processName == "*" && className == "*" && regex == null) ||
                                    !string.IsNullOrEmpty(title);
                
                if (shouldInclude) {

                    // Skip title matching if title is empty and we're doing process-based search
                    if (!string.IsNullOrEmpty(title) || processId <= 0) {
                        bool titleMatches = regex != null ? 
                            (string.IsNullOrEmpty(title) ? false : regex.IsMatch(title)) : 
                            MatchesWildcard(title ?? "", name);
                        if (!titleMatches) {
                            continue;
                        }
                    }

                    uint windowProcessId = 0;
                    MonitorNativeMethods.GetWindowThreadProcessId(handle, out windowProcessId);

                    if (!string.IsNullOrEmpty(processName) && processName != "*") {
                        try {
                            var process = Process.GetProcessById((int)windowProcessId);
                            if (!MatchesWildcard(process.ProcessName, processName)) {
                                continue;
                            }
                        } catch {
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(className) && className != "*") {
                        var classBuilder = new StringBuilder(256);
                        MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
                        if (!MatchesWildcard(classBuilder.ToString(), className)) {
                            continue;
                        }
                    }

                    if (processId > 0 && windowProcessId != processId) {
                        continue;
                    }

                    var windowInfo = new WindowInfo {
                        Title = title,
                        Handle = handle,
                        ProcessId = windowProcessId,
                        IsVisible = MonitorNativeMethods.IsWindowVisible(handle)
                    };

                        // Get window position and state
                        RECT rect = new RECT();
                        if (MonitorNativeMethods.GetWindowRect(handle, out rect)) {
                            windowInfo.Left = rect.Left;
                            windowInfo.Top = rect.Top;
                            windowInfo.Right = rect.Right;
                            windowInfo.Bottom = rect.Bottom;

                            // Get window state using the IntPtr wrapper to work on x86 and x64
                            IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE);
                            long style = stylePtr.ToInt64();
                            if ((style & MonitorNativeMethods.WS_MINIMIZE) != 0) {
                                windowInfo.State = WindowState.Minimize;
                            } else if ((style & MonitorNativeMethods.WS_MAXIMIZE) != 0) {
                                windowInfo.State = WindowState.Maximize;
                            } else {
                                windowInfo.State = WindowState.Normal;
                            }

                            // Find which monitor this window is primarily on
                            var monitors = _monitors.GetMonitors();
                            foreach (var monitor in monitors) {
                                var monitorRect = monitor.GetMonitorBounds();
                                // Check if window center point is within monitor bounds
                                int windowCenterX = (rect.Left + rect.Right) / 2;
                                int windowCenterY = (rect.Top + rect.Bottom) / 2;

                                if (windowCenterX >= monitorRect.Left && windowCenterX < monitorRect.Right &&
                                    windowCenterY >= monitorRect.Top && windowCenterY < monitorRect.Bottom) {
                                    windowInfo.MonitorIndex = monitor.Index;
                                    windowInfo.MonitorDeviceId = monitor.DeviceId;
                                    windowInfo.MonitorDeviceName = monitor.DeviceName;
                                    windowInfo.IsOnPrimaryMonitor = monitor.IsPrimary;
                                    break;
                                }
                            }
                        }

                        windows.Add(windowInfo);
                    }
                }

            return windows;
        }

        /// <summary>
        /// Finds windows by process name and title pattern.
        /// </summary>
        /// <param name="processName">Name of the process (e.g., "notepad").</param>
        /// <param name="titlePattern">Optional title pattern to match.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <returns>List of matching windows.</returns>
        public List<WindowInfo> FindWindowsByProcess(string processName, string titlePattern = "*", bool includeHidden = false) {
            if (string.IsNullOrEmpty(processName)) {
                throw new ArgumentNullException(nameof(processName));
            }
            
            // Get all windows matching the title pattern
            var windows = GetWindows(name: titlePattern, includeHidden: includeHidden);
            
            // Filter by process name
            var result = new List<WindowInfo>();
            foreach (var window in windows) {
                try {
                    using var proc = Process.GetProcessById((int)window.ProcessId);
                    if (string.Equals(proc.ProcessName, processName, StringComparison.OrdinalIgnoreCase)) {
                        result.Add(window);
                    }
                } catch {
                    // Process might have exited, skip it
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets windows belonging to the specified process.
        /// </summary>
        /// <param name="process">Process whose windows to retrieve.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <returns>List of windows owned by the process.</returns>
        public List<WindowInfo> GetWindowsForProcess(Process process, bool includeHidden = false) {
            if (process == null) {
                throw new ArgumentNullException(nameof(process));
            }

            // First try direct process ID match
            var windows = GetWindows(processId: process.Id, includeHidden: includeHidden);
            
            // If no windows found and process has a MainWindowHandle, try to find it
            if (windows.Count == 0 && process.MainWindowHandle != IntPtr.Zero) {
                var allWindows = GetWindows(includeHidden: includeHidden);
                var mainWindow = allWindows.FirstOrDefault(w => w.Handle == process.MainWindowHandle);
                if (mainWindow != null) {
                    windows.Add(mainWindow);
                }
            }
            
            // For modern Windows apps, also check child processes
            if (windows.Count == 0) {
                try {
                    // Get all processes with the same name
                    var relatedProcesses = Process.GetProcessesByName(process.ProcessName);
                    foreach (var related in relatedProcesses) {
                        if (related.Id != process.Id) {
                            var relatedWindows = GetWindows(processId: related.Id, includeHidden: includeHidden);
                            windows.AddRange(relatedWindows);
                        }
                        related.Dispose();
                    }
                } catch {
                    // Ignore errors when checking related processes
                }
            }
            
            return windows;
        }

        /// <summary>
        /// Gets the position of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <returns>The window position.</returns>
        public WindowPosition GetWindowPosition(WindowInfo windowInfo) {
            RECT rect = new RECT();
            if (MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out rect)) {
                // Re-evaluate the current state directly from the window to avoid stale data
                IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_STYLE);
                long style = stylePtr.ToInt64();
                var state = WindowState.Normal;
                if ((style & MonitorNativeMethods.WS_MINIMIZE) != 0) {
                    state = WindowState.Minimize;
                } else if ((style & MonitorNativeMethods.WS_MAXIMIZE) != 0) {
                    state = WindowState.Maximize;
                }

                return new WindowPosition {
                    Title = windowInfo.Title,
                    Handle = windowInfo.Handle,
                    ProcessId = windowInfo.ProcessId,
                    Left = rect.Left,
                    Top = rect.Top,
                    Right = rect.Right,
                    Bottom = rect.Bottom,
                    State = state
                };
            }
            throw new InvalidOperationException("Failed to get window position");
        }

        private bool MatchesWildcard(string text, string pattern) {
            if (string.IsNullOrEmpty(pattern)) {
                return false;
            }

            // If the pattern contains wildcard characters, convert it to a regex
            if (pattern.Contains('*') || pattern.Contains('?')) {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
            }

            // For plain text patterns without wildcards, check if it occurs anywhere
            return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
