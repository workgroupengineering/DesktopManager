#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI window query mapping and geometry result shaping.
/// </summary>
public class WindowQueryAndGeometryMappingTests {
    [TestMethod]
    /// <summary>
    /// Ensures default window criteria keep empty-title filtering enabled when no explicit selector is supplied.
    /// </summary>
    public void CreateWindowQuery_DefaultCriteria_ExcludeEmptyTitlesByDefault() {
        var criteria = new global::DesktopManager.Cli.WindowSelectionCriteria {
            TitlePattern = "*",
            ProcessNamePattern = "*",
            ClassNamePattern = "*",
            IncludeHidden = false,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = false,
            Active = false
        };

        WindowQueryOptions query = global::DesktopManager.Cli.DesktopOperations.CreateWindowQuery(criteria);

        Assert.AreEqual("*", query.TitlePattern);
        Assert.AreEqual("*", query.ProcessNamePattern);
        Assert.AreEqual("*", query.ClassNamePattern);
        Assert.AreEqual(false, query.IncludeHidden);
        Assert.AreEqual(true, query.IncludeCloaked);
        Assert.AreEqual(true, query.IncludeOwned);
        Assert.AreEqual(false, query.IncludeEmptyTitles);
        Assert.AreEqual(0, query.ProcessId);
        Assert.IsNull(query.Handle);
        Assert.IsFalse(query.ActiveWindow);
    }

    [TestMethod]
    /// <summary>
    /// Ensures explicit window selectors preserve null empty-title filtering so exact matches can include untitled windows.
    /// </summary>
    public void CreateWindowQuery_ExplicitSelector_PreservesNullableEmptyTitleFilter() {
        var criteria = new global::DesktopManager.Cli.WindowSelectionCriteria {
            TitlePattern = "DesktopManager*",
            ProcessNamePattern = "*",
            ClassNamePattern = "*",
            IncludeHidden = true,
            IncludeCloaked = false,
            IncludeOwned = false,
            IncludeEmptyTitles = false,
            ProcessId = 321,
            Handle = "0x1234",
            Active = true
        };

        WindowQueryOptions query = global::DesktopManager.Cli.DesktopOperations.CreateWindowQuery(criteria);

        Assert.AreEqual("DesktopManager*", query.TitlePattern);
        Assert.AreEqual(true, query.IncludeHidden);
        Assert.AreEqual(false, query.IncludeCloaked);
        Assert.AreEqual(false, query.IncludeOwned);
        Assert.IsNull(query.IncludeEmptyTitles);
        Assert.AreEqual(321, query.ProcessId);
        Assert.AreEqual(new IntPtr(0x1234), query.Handle);
        Assert.IsTrue(query.ActiveWindow);
    }

    [TestMethod]
    /// <summary>
    /// Ensures CLI geometry mapping preserves both top-level window metadata and client-area measurements.
    /// </summary>
    public void MapWindowGeometry_MapsWindowAndClientBounds() {
        var geometry = new DesktopWindowGeometry {
            Window = new WindowInfo {
                Title = "DesktopManager Test App",
                Handle = new IntPtr(0x5678),
                ProcessId = 444,
                ThreadId = 555,
                IsVisible = true,
                IsTopMost = true,
                State = WindowState.Normal,
                Left = 100,
                Top = 200,
                Right = 900,
                Bottom = 700,
                MonitorIndex = 2,
                MonitorDeviceName = @"\\.\\DISPLAY2"
            },
            WindowLeft = 100,
            WindowTop = 200,
            WindowWidth = 800,
            WindowHeight = 500,
            ClientLeft = 108,
            ClientTop = 232,
            ClientWidth = 784,
            ClientHeight = 460,
            ClientOffsetLeft = 8,
            ClientOffsetTop = 32
        };

        global::DesktopManager.Cli.WindowGeometryResult result = global::DesktopManager.Cli.DesktopOperations.MapWindowGeometry(geometry);

        Assert.AreEqual("DesktopManager Test App", result.Window.Title);
        Assert.AreEqual("0x5678", result.Window.Handle);
        Assert.AreEqual((uint)444, result.Window.ProcessId);
        Assert.AreEqual((uint)555, result.Window.ThreadId);
        Assert.IsTrue(result.Window.IsVisible);
        Assert.IsTrue(result.Window.IsTopMost);
        Assert.AreEqual("Normal", result.Window.State);
        Assert.AreEqual(100, result.Window.Left);
        Assert.AreEqual(200, result.Window.Top);
        Assert.AreEqual(800, result.Window.Width);
        Assert.AreEqual(500, result.Window.Height);
        Assert.AreEqual(2, result.Window.MonitorIndex);
        Assert.AreEqual(@"\\.\\DISPLAY2", result.Window.MonitorDeviceName);
        Assert.AreEqual(100, result.WindowLeft);
        Assert.AreEqual(200, result.WindowTop);
        Assert.AreEqual(800, result.WindowWidth);
        Assert.AreEqual(500, result.WindowHeight);
        Assert.AreEqual(108, result.ClientLeft);
        Assert.AreEqual(232, result.ClientTop);
        Assert.AreEqual(784, result.ClientWidth);
        Assert.AreEqual(460, result.ClientHeight);
        Assert.AreEqual(8, result.ClientOffsetLeft);
        Assert.AreEqual(32, result.ClientOffsetTop);
    }
}
#endif
