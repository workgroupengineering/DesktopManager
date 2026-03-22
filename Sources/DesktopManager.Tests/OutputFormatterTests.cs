#if NET8_0_OR_GREATER
using System.IO;
using System.Collections.Generic;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared CLI output formatting helpers.
/// </summary>
public class OutputFormatterTests {
    [TestMethod]
    /// <summary>
    /// Ensures JSON output is written to the provided writer.
    /// </summary>
    public void WriteJson_WritesSerializedJsonToWriter() {
        using var writer = new StringWriter();

        global::DesktopManager.Cli.OutputFormatter.WriteJson(writer, new {
            Name = "DesktopManager",
            Count = 2
        });

        string output = writer.ToString();

        StringAssert.Contains(output, "\"Name\": \"DesktopManager\"");
        StringAssert.Contains(output, "\"Count\": 2");
    }

    [TestMethod]
    /// <summary>
    /// Ensures table output includes headers, separator, and aligned rows.
    /// </summary>
    public void WriteTable_WritesHeadersSeparatorAndRows() {
        using var writer = new StringWriter();

        global::DesktopManager.Cli.OutputFormatter.WriteTable(
            writer,
            new[] { "Id", "Name" },
            new List<IReadOnlyList<string>> {
                new[] { "1", "Alpha" },
                new[] { "22", "Beta" }
            });

        string output = writer.ToString();

        StringAssert.Contains(output, "Id  Name ");
        StringAssert.Contains(output, "--  -----");
        StringAssert.Contains(output, "1   Alpha");
        StringAssert.Contains(output, "22  Beta ");
    }
}
#endif
