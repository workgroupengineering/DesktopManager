using System;
using System.IO;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for shared desktop automation helpers.
/// </summary>
public class DesktopAutomationCoreTests {
    [TestMethod]
    /// <summary>
    /// Ensures hexadecimal handles parse correctly.
    /// </summary>
    public void DesktopHandleParser_ParseHexValue_ReturnsHandle() {
        IntPtr handle = DesktopHandleParser.Parse("0x1A2B");

        Assert.AreEqual(new IntPtr(0x1A2B), handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures decimal handles parse correctly.
    /// </summary>
    public void DesktopHandleParser_ParseDecimalValue_ReturnsHandle() {
        IntPtr handle = DesktopHandleParser.Parse("6699");

        Assert.AreEqual(new IntPtr(6699), handle);
    }

    [TestMethod]
    /// <summary>
    /// Ensures invalid handles are rejected.
    /// </summary>
    public void DesktopHandleParser_ParseInvalidValue_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => DesktopHandleParser.Parse("not-a-handle"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot paths are normalized and get a png extension.
    /// </summary>
    public void DesktopStateStore_ResolveCapturePath_AppendsPngExtension() {
        string testRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string requestedPath = Path.Combine(testRoot, "capture-output");

        try {
            string resolvedPath = DesktopStateStore.ResolveCapturePath("desktop", requestedPath);
            string expectedPath = Path.GetFullPath(requestedPath + ".png");

            Assert.AreEqual(expectedPath, resolvedPath, true);
            Assert.AreEqual(".png", Path.GetExtension(resolvedPath), true);
            Assert.IsTrue(Directory.Exists(testRoot));
        } finally {
            if (Directory.Exists(testRoot)) {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures screenshot paths normalize non-PNG extensions to PNG.
    /// </summary>
    public void DesktopStateStore_ResolveCapturePath_ReplacesNonPngExtension() {
        string testRoot = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", Guid.NewGuid().ToString("N"));
        string requestedPath = Path.Combine(testRoot, "capture-output.jpg");

        try {
            string resolvedPath = DesktopStateStore.ResolveCapturePath("desktop", requestedPath);
            string expectedPath = Path.GetFullPath(Path.Combine(testRoot, "capture-output.png"));

            Assert.AreEqual(expectedPath, resolvedPath, true);
            Assert.AreEqual(".png", Path.GetExtension(resolvedPath), true);
            Assert.IsTrue(Directory.Exists(testRoot));
        } finally {
            if (Directory.Exists(testRoot)) {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures target paths use json storage.
    /// </summary>
    public void DesktopStateStore_GetTargetPath_UsesJsonExtension() {
        string path = DesktopStateStore.GetTargetPath("editor-center");

        Assert.AreEqual(".json", Path.GetExtension(path), true);
        StringAssert.Contains(path, Path.DirectorySeparatorChar + "targets" + Path.DirectorySeparatorChar);
    }

    [TestMethod]
    /// <summary>
    /// Ensures reserved Windows device names are rejected for saved state.
    /// </summary>
    public void DesktopStateStore_GetTargetPath_ReservedDeviceName_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => DesktopStateStore.GetTargetPath("CON"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures trailing spaces and dots are normalized away from saved state names.
    /// </summary>
    public void DesktopStateStore_GetTargetPath_TrailingDotsAndSpaces_AreTrimmed() {
        string path = DesktopStateStore.GetTargetPath("editor-center. ");

        Assert.AreEqual("editor-center.json", Path.GetFileName(path), true);
    }

    [TestMethod]
    /// <summary>
    /// Ensures saved window targets round-trip through the shared core.
    /// </summary>
    public void DesktopAutomationService_SaveWindowTarget_RoundTripsDefinition() {
        string targetName = "DesktopAutomationCoreTests-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetTargetPath(targetName);
        var automation = new DesktopAutomationService();

        try {
            DesktopWindowTargetDefinition saved = automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
                Description = "Editor center",
                XRatio = 0.5,
                YRatio = 0.5,
                WidthRatio = 0.8,
                HeightRatio = 0.6,
                ClientArea = true
            });

            DesktopWindowTargetDefinition loaded = automation.GetWindowTarget(targetName);

            Assert.AreEqual("Editor center", saved.Description);
            Assert.AreEqual(saved.Description, loaded.Description);
            Assert.AreEqual(saved.XRatio, loaded.XRatio);
            Assert.AreEqual(saved.YRatio, loaded.YRatio);
            Assert.AreEqual(saved.WidthRatio, loaded.WidthRatio);
            Assert.AreEqual(saved.HeightRatio, loaded.HeightRatio);
            Assert.AreEqual(saved.ClientArea, loaded.ClientArea);
            Assert.IsTrue(File.Exists(path));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures saved control targets round-trip through the shared core.
    /// </summary>
    public void DesktopAutomationService_SaveControlTarget_RoundTripsDefinition() {
        string targetName = "DesktopAutomationCoreTests-Control-" + Guid.NewGuid().ToString("N");
        string path = DesktopStateStore.GetControlTargetPath(targetName);
        var automation = new DesktopAutomationService();

        try {
            DesktopControlTargetDefinition saved = automation.SaveControlTarget(targetName, new DesktopControlTargetDefinition {
                Description = "Address bar",
                ControlTypePattern = "Edit",
                SupportsBackgroundText = true,
                UseUiAutomation = true
            });

            DesktopControlTargetDefinition loaded = automation.GetControlTarget(targetName);

            Assert.AreEqual("Address bar", saved.Description);
            Assert.AreEqual(saved.Description, loaded.Description);
            Assert.AreEqual(saved.ControlTypePattern, loaded.ControlTypePattern);
            Assert.AreEqual(saved.SupportsBackgroundText, loaded.SupportsBackgroundText);
            Assert.AreEqual(saved.UseUiAutomation, loaded.UseUiAutomation);
            Assert.IsTrue(File.Exists(path));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    /// <summary>
    /// Ensures control queries reject a missing window selector.
    /// </summary>
    public void DesktopAutomationService_ControlExists_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ControlExists(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control queries reject a missing window selector.
    /// </summary>
    public void DesktopAutomationService_GetControls_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.GetControls(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for controls rejects a missing window selector.
    /// </summary>
    public void DesktopAutomationService_WaitForControls_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.WaitForControls(null!, null, 1000, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for controls rejects negative timeouts.
    /// </summary>
    public void DesktopAutomationService_WaitForControls_NegativeTimeout_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.WaitForControls(new WindowQueryOptions { TitlePattern = "*" }, null, -1, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for controls rejects non-positive polling intervals.
    /// </summary>
    public void DesktopAutomationService_WaitForControls_ZeroInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.WaitForControls(new WindowQueryOptions { TitlePattern = "*" }, null, 1000, 0));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control diagnostics reject a missing window selector.
    /// </summary>
    public void DesktopAutomationService_GetControlDiagnostics_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.GetControlDiagnostics(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control diagnostics reject negative sample sizes.
    /// </summary>
    public void DesktopAutomationService_GetControlDiagnostics_NegativeSampleLimit_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.GetControlDiagnostics(new WindowQueryOptions { TitlePattern = "*" }, sampleLimit: -1));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window-relative clicks reject a negative X coordinate.
    /// </summary>
    public void DesktopAutomationService_ClickWindowPoint_NegativeX_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ClickWindowPoint(new WindowQueryOptions { TitlePattern = "*" }, -1, 0, MouseButton.Left, activate: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window-relative clicks reject a negative Y coordinate.
    /// </summary>
    public void DesktopAutomationService_ClickWindowPoint_NegativeY_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ClickWindowPoint(new WindowQueryOptions { TitlePattern = "*" }, 0, -1, MouseButton.Left, activate: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window-relative drags reject a negative step delay.
    /// </summary>
    public void DesktopAutomationService_DragWindowPoints_NegativeStepDelay_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.DragWindowPoints(new WindowQueryOptions { TitlePattern = "*" }, 0, 0, 1, 1, MouseButton.Left, -1, activate: false, clientArea: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures normalized clicks reject ratios outside the 0..1 range.
    /// </summary>
    public void DesktopAutomationService_ClickWindowPoint_InvalidRatio_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.ClickWindowPoint(new WindowQueryOptions { TitlePattern = "*" }, null, null, 1.5, 0.5, MouseButton.Left, activate: false, clientArea: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window geometry rejects a missing selector.
    /// </summary>
    public void DesktopAutomationService_GetWindowGeometry_NullOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.GetWindowGeometry(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch rejects negative window wait values.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_NegativeWindowWait_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "notepad.exe",
            WaitForWindowMilliseconds = -1
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch rejects non-positive window polling intervals.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_ZeroWindowInterval_ThrowsArgumentOutOfRangeException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "notepad.exe",
            WaitForWindowIntervalMilliseconds = 0
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch can require a real user-facing window when none appears.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_RequireWindowWithoutUserFacingWindow_ThrowsTimeoutException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<TimeoutException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "cmd.exe",
            Arguments = "/c exit",
            WaitForWindowMilliseconds = 1,
            WaitForWindowIntervalMilliseconds = 1,
            RequireWindow = true
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures process launch can require a matching window selector when no match appears.
    /// </summary>
    public void DesktopAutomationService_LaunchProcess_RequireWindowWithImpossibleSelector_ThrowsTimeoutException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<TimeoutException>(() => automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "cmd.exe",
            Arguments = "/c exit",
            WindowTitlePattern = "__DesktopManager_NoSuchWindow__",
            WaitForWindowMilliseconds = 1,
            WaitForWindowIntervalMilliseconds = 1,
            RequireWindow = true
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures target definitions require one horizontal selector.
    /// </summary>
    public void DesktopAutomationService_SaveWindowTarget_MissingHorizontalSelector_ThrowsArgumentException() {
        string targetName = "DesktopAutomationCoreTests-" + Guid.NewGuid().ToString("N");
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
            YRatio = 0.5
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures target area definitions reject zero width.
    /// </summary>
    public void DesktopAutomationService_SaveWindowTarget_ZeroWidth_ThrowsArgumentOutOfRangeException() {
        string targetName = "DesktopAutomationCoreTests-" + Guid.NewGuid().ToString("N");
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => automation.SaveWindowTarget(targetName, new DesktopWindowTargetDefinition {
            XRatio = 0.5,
            YRatio = 0.5,
            Width = 0,
            Height = 100
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures control target definitions require at least one selector or capability.
    /// </summary>
    public void DesktopAutomationService_SaveControlTarget_WithoutSelectors_ThrowsArgumentException() {
        string targetName = "DesktopAutomationCoreTests-Control-" + Guid.NewGuid().ToString("N");
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentException>(() => automation.SaveControlTarget(targetName, new DesktopControlTargetDefinition()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control-target lookups reject null window selectors.
    /// </summary>
    public void DesktopAutomationService_ControlTargetExists_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ControlTargetExists(null!, "sample"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures waiting for a control target rejects a missing window selector.
    /// </summary>
    public void DesktopAutomationService_WaitForControlTarget_NullWindowOptions_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.WaitForControlTarget(null!, "sample", 1000, 100));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control clicks reject null controls.
    /// </summary>
    public void DesktopAutomationService_ClickControl_NullControl_ThrowsArgumentNullException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<ArgumentNullException>(() => automation.ClickControl(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control clicks require parent window metadata.
    /// </summary>
    public void DesktopAutomationService_ClickControl_MissingParentWindow_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.ClickControl(new WindowControlInfo {
            Handle = new IntPtr(1)
        }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control text updates require parent window metadata.
    /// </summary>
    public void DesktopAutomationService_SetControlText_MissingParentWindow_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.SetControlText(new WindowControlInfo {
            Handle = new IntPtr(1)
        }, "hello"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures shared control key sends require parent window metadata.
    /// </summary>
    public void DesktopAutomationService_SendControlKeys_MissingParentWindow_ThrowsInvalidOperationException() {
        var automation = new DesktopAutomationService();

        Assert.ThrowsException<InvalidOperationException>(() => automation.SendControlKeys(new WindowControlInfo {
            Handle = new IntPtr(1)
        }, new[] { VirtualKey.VK_A }));
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation text routing requires an explicitly fallback-capable control.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_UnsupportedControl_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = false
        }, allowForegroundInputFallback: true);

        StringAssert.Contains(error, "does not expose direct value setting");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation text routing requires an explicit foreground-input opt-in.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_MissingOptIn_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: false);

        StringAssert.Contains(error, "Enable foreground input fallback");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation text routing allows explicitly approved foreground fallback.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_AllowedControl_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: true);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures handle-backed controls bypass the zero-handle UI Automation text fallback checks.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationTextFallback_HandleBackedControl_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationTextFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = new IntPtr(123),
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: false);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation key routing requires a fallback-capable control.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_UnsupportedControl_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = false
        }, allowForegroundInputFallback: true);

        StringAssert.Contains(error, "cannot receive foreground fallback key input");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation key routing requires an explicit foreground-input opt-in.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_MissingOptIn_ReturnsError() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: false);

        StringAssert.Contains(error, "does not expose a Win32 handle");
    }

    [TestMethod]
    /// <summary>
    /// Ensures zero-handle UI Automation key routing allows explicitly approved foreground fallback.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_AllowedControl_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.UiAutomation,
            Handle = IntPtr.Zero,
            ControlType = "Edit",
            ClassName = "TextBox",
            SupportsForegroundInputFallback = true
        }, allowForegroundInputFallback: true);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures Win32 controls bypass the zero-handle UI Automation key fallback checks.
    /// </summary>
    public void DesktopAutomationService_ValidateUiAutomationKeyFallback_Win32Control_ReturnsNull() {
        string? error = DesktopAutomationService.ValidateUiAutomationKeyFallback(new WindowControlInfo {
            Source = WindowControlSource.Win32,
            Handle = IntPtr.Zero,
            SupportsForegroundInputFallback = false
        }, allowForegroundInputFallback: false);

        Assert.IsNull(error);
    }

    [TestMethod]
    /// <summary>
    /// Ensures direct control text updates reject missing Win32 handles.
    /// </summary>
    public void WindowControlService_SetText_InvalidHandle_ThrowsArgumentException() {
        var control = new WindowControlInfo();

        Assert.ThrowsException<ArgumentException>(() => WindowControlService.SetText(control, "hello"));
    }

    [TestMethod]
    /// <summary>
    /// Ensures direct control key sends reject missing Win32 handles.
    /// </summary>
    public void WindowControlService_SendKeys_InvalidHandle_ThrowsArgumentException() {
        var control = new WindowControlInfo();

        Assert.ThrowsException<ArgumentException>(() => WindowControlService.SendKeys(control, VirtualKey.VK_A));
    }

    [TestMethod]
    /// <summary>
    /// Ensures direct control key sends require at least one key.
    /// </summary>
    public void WindowControlService_SendKeys_NoKeys_ThrowsArgumentException() {
        var control = new WindowControlInfo {
            Handle = new IntPtr(1)
        };

        Assert.ThrowsException<ArgumentException>(() => WindowControlService.SendKeys(control, Array.Empty<VirtualKey>()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground key sending requires at least one key.
    /// </summary>
    public void KeyboardInputService_SendToForeground_NoKeys_ThrowsArgumentException() {
        Assert.ThrowsException<ArgumentException>(() => KeyboardInputService.SendToForeground(Array.Empty<VirtualKey>()));
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground text sending rejects null text.
    /// </summary>
    public void KeyboardInputService_SendTextToForeground_NullText_ThrowsArgumentNullException() {
        Assert.ThrowsException<ArgumentNullException>(() => KeyboardInputService.SendTextToForeground(null!));
    }

    [TestMethod]
    /// <summary>
    /// Ensures foreground text sending rejects negative delays.
    /// </summary>
    public void KeyboardInputService_SendTextToForeground_NegativeDelay_ThrowsArgumentOutOfRangeException() {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => KeyboardInputService.SendTextToForeground("test", -1));
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing prefers real foreground input when the target already owns focus.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_WithForegroundTarget_UsesForegroundInput() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true
        }, targetOwnsForeground: true);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.ForegroundInput, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures window typing falls back to message delivery when foreground ownership is unavailable and strict typing is not required.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_WithoutForegroundTarget_UsesWindowMessageFallback() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true
        }, targetOwnsForeground: false);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.WindowMessage, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures strict foreground typing fails when the target window does not own the foreground.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_RequireForegroundWithoutForeground_ThrowsInvalidOperationException() {
        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            RequireForegroundWindowForTyping = true
        }, targetOwnsForeground: false));

        StringAssert.Contains(exception.Message, "Current:");
    }

    [TestMethod]
    /// <summary>
    /// Ensures strict foreground typing rejects message-only routing.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_RequireForegroundWithMessageOnly_ThrowsInvalidOperationException() {
        Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = false,
            RequireForegroundWindowForTyping = true
        }, targetOwnsForeground: true));
    }

    [TestMethod]
    /// <summary>
    /// Ensures physical-key typing requires the target window to own the foreground.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_PhysicalKeysWithoutForeground_ThrowsInvalidOperationException() {
        Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UsePhysicalKeyboardLayout = true
        }, targetOwnsForeground: false));
    }

    [TestMethod]
    /// <summary>
    /// Ensures physical-key typing still routes through the foreground delivery path when focus ownership is available.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_PhysicalKeysWithForeground_UsesForegroundInput() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UsePhysicalKeyboardLayout = true
        }, targetOwnsForeground: true);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.ForegroundInput, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session scan code typing requires the target window to own the foreground.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_HostedSessionWithoutForeground_ThrowsInvalidOperationException() {
        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UseHostedSessionScanCodes = true
        }, targetOwnsForeground: false));

        StringAssert.Contains(exception.Message, "Current:");
    }

    [TestMethod]
    /// <summary>
    /// Ensures hosted-session scan code typing routes through foreground delivery when the target owns focus.
    /// </summary>
    public void WindowInputService_ResolveTextDeliveryMode_HostedSessionWithForeground_UsesForegroundInput() {
        WindowInputService.WindowTextDeliveryMode mode = WindowInputService.ResolveTextDeliveryMode(new WindowInputOptions {
            UseSendInput = true,
            UseHostedSessionScanCodes = true
        }, targetOwnsForeground: true);

        Assert.AreEqual(WindowInputService.WindowTextDeliveryMode.ForegroundInput, mode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures script chunking preserves blank lines and line breaks.
    /// </summary>
    public void WindowInputService_CreateScriptChunks_PreservesBlankLines() {
        IReadOnlyList<WindowInputService.WindowScriptChunk> chunks = WindowInputService.CreateScriptChunks("line1\n\nline3", 120);

        Assert.AreEqual(3, chunks.Count);
        Assert.AreEqual("line1", chunks[0].Text);
        Assert.IsTrue(chunks[0].SendLineBreak);
        Assert.AreEqual(string.Empty, chunks[1].Text);
        Assert.IsTrue(chunks[1].SendLineBreak);
        Assert.AreEqual("line3", chunks[2].Text);
        Assert.IsFalse(chunks[2].SendLineBreak);
    }

    [TestMethod]
    /// <summary>
    /// Ensures script chunking splits long lines into smaller segments and only marks the final chunk with a line break.
    /// </summary>
    public void WindowInputService_CreateScriptChunks_SplitsLongLinesSafely() {
        IReadOnlyList<WindowInputService.WindowScriptChunk> chunks = WindowInputService.CreateScriptChunks("abcdef\ngh", 3);

        Assert.AreEqual(3, chunks.Count);
        Assert.AreEqual("abc", chunks[0].Text);
        Assert.IsFalse(chunks[0].SendLineBreak);
        Assert.AreEqual("def", chunks[1].Text);
        Assert.IsTrue(chunks[1].SendLineBreak);
        Assert.AreEqual("gh", chunks[2].Text);
        Assert.IsFalse(chunks[2].SendLineBreak);
    }
}
