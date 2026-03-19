using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class OutputFormatter {
    public static void WriteJson(object value) {
        Console.WriteLine(JsonUtilities.Serialize(value));
    }

    public static void WriteTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows) {
        int[] widths = new int[headers.Count];
        for (int index = 0; index < headers.Count; index++) {
            widths[index] = headers[index].Length;
        }

        foreach (IReadOnlyList<string> row in rows) {
            for (int index = 0; index < headers.Count && index < row.Count; index++) {
                widths[index] = Math.Max(widths[index], row[index].Length);
            }
        }

        Console.WriteLine(FormatRow(headers, widths));
        Console.WriteLine(FormatSeparator(widths));
        foreach (IReadOnlyList<string> row in rows) {
            Console.WriteLine(FormatRow(row, widths));
        }
    }

    private static string FormatRow(IReadOnlyList<string> values, IReadOnlyList<int> widths) {
        return string.Join("  ", values.Select((value, index) => value.PadRight(widths[index], ' ')));
    }

    private static string FormatSeparator(IReadOnlyList<int> widths) {
        return string.Join("  ", widths.Select(width => new string('-', width)));
    }
}
