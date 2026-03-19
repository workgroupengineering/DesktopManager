using System;
using System.Globalization;

namespace DesktopManager;

/// <summary>
/// Parses decimal or hexadecimal window and control handles.
/// </summary>
public static class DesktopHandleParser {
    /// <summary>
    /// Parses a handle value in decimal or hexadecimal notation.
    /// </summary>
    /// <param name="value">Handle value.</param>
    /// <returns>The parsed handle.</returns>
    public static IntPtr Parse(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("A handle value is required.", nameof(value));
        }

        bool isHex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            value.IndexOfAny(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f' }) >= 0;
        if (isHex) {
            string normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value.Substring(2) : value;
            if (long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue)) {
                return new IntPtr(hexValue);
            }
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long decimalValue)) {
            return new IntPtr(decimalValue);
        }

        throw new ArgumentException("The provided handle is not a valid decimal or hexadecimal window handle.", nameof(value));
    }
}
