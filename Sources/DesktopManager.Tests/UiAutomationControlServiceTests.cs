using System;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for UI Automation helper behavior.
/// </summary>
public class UiAutomationControlServiceTests {
    [TestMethod]
    /// <summary>
    /// Ensures Chromium render widget roots are prioritized for fallback probing.
    /// </summary>
    public void GetFallbackRootHandles_ChromeRenderWidget_IsPrioritized() {
        IReadOnlyList<IntPtr> handles = UiAutomationControlService.GetFallbackRootHandles(new IntPtr(100), new[] {
            new WindowControlInfo {
                Handle = new IntPtr(200),
                ClassName = "Intermediate D3D Window"
            },
            new WindowControlInfo {
                Handle = new IntPtr(300),
                ClassName = "Chrome_RenderWidgetHostHWND"
            }
        });

        CollectionAssert.AreEqual(new[] { new IntPtr(300), new IntPtr(200) }, handles.ToArray());
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid or duplicate handles are excluded from fallback probing.
    /// </summary>
    public void GetFallbackRootHandles_ZeroDuplicateAndParentHandles_AreExcluded() {
        IReadOnlyList<IntPtr> handles = UiAutomationControlService.GetFallbackRootHandles(new IntPtr(100), new[] {
            new WindowControlInfo {
                Handle = IntPtr.Zero,
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(100),
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(200),
                ClassName = "Chrome_RenderWidgetHostHWND"
            },
            new WindowControlInfo {
                Handle = new IntPtr(200),
                ClassName = "Chrome_RenderWidgetHostHWND"
            }
        });

        CollectionAssert.AreEqual(new[] { new IntPtr(200) }, handles.ToArray());
    }
}
