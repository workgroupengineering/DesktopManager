using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Versioning;
#if NETFRAMEWORK
using System.Windows.Forms;
#endif

namespace DesktopManager;

/// <summary>
/// Provides methods for capturing screenshots of the desktop.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ScreenshotService {
    /// <summary>
    /// Captures a screenshot of the entire virtual screen.
    /// </summary>
    /// <returns>A <see cref="Bitmap"/> containing the screenshot.</returns>
    public static Bitmap CaptureScreen() {
        // Use the system-reported virtual screen bounds instead of calculating from individual monitors
        // This ensures we capture exactly what Windows considers the virtual screen
#if NETFRAMEWORK
        var bounds = SystemInformation.VirtualScreen;
#else
        var bounds = GetVirtualScreenBounds();
#endif
        
        return CaptureRegion(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Captures a screenshot of the specified monitor.
    /// </summary>
    /// <param name="index">Monitor index starting at 0.</param>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="deviceName">Monitor device name.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureMonitor(int? index = null, string deviceId = null, string deviceName = null) {
        Monitors monitors = new();
        var monitor = monitors.GetMonitors(index: index, deviceId: deviceId, deviceName: deviceName).FirstOrDefault();
        if (monitor == null) {
            string requested = !string.IsNullOrEmpty(deviceId)
                ? $"DeviceId '{deviceId}'"
                : !string.IsNullOrEmpty(deviceName)
                    ? $"DeviceName '{deviceName}'"
                    : "the specified criteria";
            throw new ArgumentException($"Monitor not found for {requested}");
        }

        var rect = monitor.GetMonitorBounds();
        return CaptureRegion(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Captures a screenshot of an arbitrary region of the desktop.
    /// </summary>
    /// <param name="region">Rectangle describing region to capture.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureRegion(Rectangle region) {
        return CaptureRegion(region.Left, region.Top, region.Width, region.Height);
    }

    /// <summary>
    /// Captures a screenshot of an arbitrary region.
    /// </summary>
    /// <param name="left">Left coordinate.</param>
    /// <param name="top">Top coordinate.</param>
    /// <param name="width">Width of the region.</param>
    /// <param name="height">Height of the region.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureRegion(int left, int top, int width, int height) {
        if (width <= 0 || height <= 0) {
            throw new ArgumentException("Width and height must be greater than zero");
        }

        Rectangle bounds;
#if NETFRAMEWORK
        bounds = SystemInformation.VirtualScreen;
#else
        bounds = GetVirtualScreenBounds();
#endif
        // Check if the requested region is within the virtual screen bounds
        int requestedRight = left + width;
        int requestedBottom = top + height;
        int boundsRight = bounds.Left + bounds.Width;
        int boundsBottom = bounds.Top + bounds.Height;
        
        // First try to capture as-is if it's within bounds
        bool isWithinBounds = left >= bounds.Left && top >= bounds.Top && 
                             requestedRight <= boundsRight && requestedBottom <= boundsBottom;
        
        if (!isWithinBounds) {
            // For monitor capture, try to intersect with virtual screen bounds to handle coordinate system mismatches
            int adjustedLeft = Math.Max(left, bounds.Left);
            int adjustedTop = Math.Max(top, bounds.Top);
            int adjustedRight = Math.Min(requestedRight, boundsRight);
            int adjustedBottom = Math.Min(requestedBottom, boundsBottom);
            
            // If there's still a valid intersection, use it
            if (adjustedLeft < adjustedRight && adjustedTop < adjustedBottom) {
                left = adjustedLeft;
                top = adjustedTop;
                width = adjustedRight - adjustedLeft;
                height = adjustedBottom - adjustedTop;
            } else {
                throw new ArgumentOutOfRangeException(nameof(left), 
                    $"Region ({left}, {top}, {width}x{height}) is outside the bounds of the virtual screen ({bounds.Left}, {bounds.Top}, {bounds.Width}x{bounds.Height})");
            }
        }

        Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        return bitmap;
    }

    /// <summary>
    /// Captures a screenshot of a window.
    /// </summary>
    /// <param name="hwnd">Window handle.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureWindow(IntPtr hwnd) {
        if (hwnd == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(hwnd));
        }

        if (!MonitorNativeMethods.GetWindowRect(hwnd, out RECT rect)) {
            throw new InvalidOperationException("Failed to get window bounds");
        }

        return CaptureRegion(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Captures a screenshot of a window control.
    /// </summary>
    /// <param name="hwnd">Control handle.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureControl(IntPtr hwnd) {
        return CaptureWindow(hwnd);
    }

#if !NETFRAMEWORK
    private static Rectangle GetVirtualScreenBounds() {
        int left = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_XVIRTUALSCREEN);
        int top = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_YVIRTUALSCREEN);
        int width = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CXVIRTUALSCREEN);
        int height = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CYVIRTUALSCREEN);
        return new Rectangle(left, top, width, height);
    }
#endif
}
