using System.Linq;
using System.Runtime.InteropServices;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Test class for WindowTitleMatchingTests.
/// </summary>
public class WindowTitleMatchingTests
{
    [TestMethod]
    /// <summary>
    /// Test for GetWindows_TitleMatchingIsCaseInsensitive.
    /// </summary>
    public void GetWindows_TitleMatchingIsCaseInsensitive()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("Test requires Windows");
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows();
        if (windows.Count == 0)
        {
            Assert.Inconclusive("No windows found to test");
        }

        // Find a window with a non-empty title that has mixed case
        var window = windows.FirstOrDefault(w => 
            !string.IsNullOrEmpty(w.Title) && 
            w.Title.Length > 1 &&
            w.Title.Any(char.IsLetter));
            
        if (window == null)
        {
            Assert.Inconclusive("No windows with non-empty titles found to test");
        }

        string titleUpper = window.Title.ToUpperInvariant();
        string titleLower = window.Title.ToLowerInvariant();

        // Only test if the title actually has different cases
        if (titleUpper == titleLower)
        {
            Assert.Inconclusive("Window title has no case differences to test");
        }

        Assert.IsTrue(manager.GetWindows(titleUpper).Any(w => w.Handle == window.Handle),
            $"Failed to find window with uppercase title: '{titleUpper}'");
        Assert.IsTrue(manager.GetWindows(titleLower).Any(w => w.Handle == window.Handle),
            $"Failed to find window with lowercase title: '{titleLower}'");
    }
}
