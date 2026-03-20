using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// End-to-end MCP server tests against a disposable desktop application.
/// </summary>
public class McpServerEndToEndTests {
    private const int NotepadLaunchTimeoutMs = 20000;
    private const int NotepadControlDiscoveryTimeoutMs = 20000;
    private const int EdgeLaunchTimeoutMs = 30000;
    private const int EdgeControlDiscoveryTimeoutMs = 15000;
    private const double EdgeOmniboxFallbackXRatio = 0.42;
    private const double EdgeOmniboxFallbackYRatio = 0.05;

    [TestCleanup]
    public void Cleanup() {
        TestHelper.KillAllNotepads();
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can launch Notepad, discover an editable control, set text, assert the value, and capture an artifact.
    /// </summary>
    public void McpServer_NotepadRoundTrip_LaunchesSetsTextAssertsValueAndCapturesArtifact() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            JsonElement launchResult = client.CallTool(2, "launch_and_wait_for_window", new {
                filePath = "notepad.exe",
                timeoutMs = NotepadLaunchTimeoutMs,
                intervalMs = 100,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(launchResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(launchResult.GetProperty("WindowWait").GetProperty("Count").GetInt32() > 0);
            resolvedProcessId = ReadResolvedProcessId(launchResult.GetProperty("Launch"));
            Assert.IsTrue(resolvedProcessId > 0);
            AssertScreenshotPathsExist(launchResult.GetProperty("AfterScreenshots"));
            JsonElement launchedWindow = launchResult.GetProperty("WindowWait").GetProperty("Windows")[0];
            string launchedWindowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(launchedWindowHandle), "Expected the launched Notepad window to expose a handle.");

            int requestId = 3;
            JsonElement editor = WaitForEditableNotepadControl(client, ref requestId, launchedWindowHandle, NotepadControlDiscoveryTimeoutMs, 100);
            string editorClassName = editor.GetProperty("ClassName").GetString() ?? string.Empty;
            string editorHandle = editor.GetProperty("Handle").GetString() ?? string.Empty;
            string windowHandle = editor.GetProperty("ParentWindow").GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(editorClassName), "Expected an editable Notepad control class.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the resolved editor control to include its parent window handle.");

            string resolvedControlHandle = editorHandle;
            if (string.IsNullOrWhiteSpace(resolvedControlHandle)) {
                JsonElement waitedControl = client.CallTool(requestId++, "wait_for_control", new {
                    windowHandle,
                    controlClassName = editorClassName,
                    timeoutMs = 5000,
                    intervalMs = 100
                });
                Assert.IsTrue(waitedControl.GetProperty("Count").GetInt32() >= 1, "Expected MCP to wait for the Notepad editor control before mutating it.");
                JsonElement freshEditor = waitedControl.GetProperty("Controls")[0];
                resolvedControlHandle = freshEditor.GetProperty("Handle").GetString() ?? string.Empty;
            }

            Assert.IsFalse(string.IsNullOrWhiteSpace(resolvedControlHandle), "Expected the resolved Notepad editor control to expose a handle.");

            string expectedText = "DesktopManager MCP E2E " + Guid.NewGuid().ToString("N");
            JsonElement setTextResult = client.CallTool(requestId++, "set_control_text", new {
                windowHandle,
                controlHandle = resolvedControlHandle,
                text = expectedText,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(setTextResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(setTextResult.GetProperty("Count").GetInt32() >= 1);
            StringAssert.Contains(setTextResult.GetProperty("SafetyMode").GetString() ?? string.Empty, "background");
            AssertScreenshotPathsExist(setTextResult.GetProperty("AfterScreenshots"));

            JsonElement assertionResult = client.CallTool(requestId++, "assert_control_value", new {
                windowHandle,
                controlHandle = resolvedControlHandle,
                expectedValue = expectedText
            });

            Assert.IsTrue(assertionResult.GetProperty("Matched").GetBoolean(), "Expected MCP to verify the Notepad editor value after text entry.");
            Assert.IsTrue(assertionResult.GetProperty("MatchedCount").GetInt32() >= 1);
        } finally {
            if (resolvedProcessId > 0) {
                try {
                    using Process process = Process.GetProcessById(resolvedProcessId);
                    TestHelper.SafeKillProcess(process);
                } catch {
                    // Ignore cleanup failures for already exited processes.
                }
            }

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can save a reusable target area, resolve it against Notepad, and capture that exact region.
    /// </summary>
    public void McpServer_NotepadTargetAreaRoundTrip_SavesResolvesAndCapturesTarget() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        string targetName = "McpServerEndToEnd-" + Guid.NewGuid().ToString("N");
        string targetPath = DesktopStateStore.GetTargetPath(targetName);
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchNotepadProcess(out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            WaitForProcessWindow(client, ref requestId, resolvedProcessId, NotepadLaunchTimeoutMs, 100, "target capture");

            JsonElement saveTargetResult = client.CallTool(requestId++, "save_window_target", new {
                name = targetName,
                description = "Notepad center area",
                xRatio = 0.25,
                yRatio = 0.2,
                widthRatio = 0.5,
                heightRatio = 0.5,
                clientArea = true
            });

            Assert.AreEqual(targetName, saveTargetResult.GetProperty("Name").GetString());
            Assert.IsTrue(File.Exists(targetPath));

            JsonElement resolvedTargets = client.CallTool(requestId++, "resolve_window_target", new {
                name = targetName,
                processId = resolvedProcessId
            });

            Assert.IsTrue(resolvedTargets.ValueKind == JsonValueKind.Array && resolvedTargets.GetArrayLength() == 1, "Expected the named target to resolve against exactly one Notepad window.");
            JsonElement resolvedTarget = resolvedTargets[0];
            Assert.AreEqual(targetName, resolvedTarget.GetProperty("Name").GetString());
            Assert.IsTrue(resolvedTarget.GetProperty("Target").GetProperty("ClientArea").GetBoolean());
            int screenWidth = resolvedTarget.GetProperty("ScreenWidth").GetInt32();
            int screenHeight = resolvedTarget.GetProperty("ScreenHeight").GetInt32();
            Assert.IsTrue(screenWidth > 0);
            Assert.IsTrue(screenHeight > 0);

            string outputPath = Path.Combine(artifactDirectory, "notepad-target.png");
            JsonElement screenshotResult = client.CallTool(requestId++, "screenshot_window", new {
                processId = resolvedProcessId,
                targetName,
                outputPath
            });

            Assert.AreEqual("window-target", screenshotResult.GetProperty("Kind").GetString());
            Assert.AreEqual(screenWidth, screenshotResult.GetProperty("Width").GetInt32());
            Assert.AreEqual(screenHeight, screenshotResult.GetProperty("Height").GetInt32());
            Assert.AreEqual(resolvedProcessId, screenshotResult.GetProperty("Window").GetProperty("ProcessId").GetInt32());
            Assert.IsTrue(File.Exists(screenshotResult.GetProperty("Path").GetString() ?? string.Empty));
            Assert.IsTrue(screenshotResult.TryGetProperty("Geometry", out JsonElement geometry));
            Assert.IsTrue(geometry.GetProperty("ClientWidth").GetInt32() >= screenshotResult.GetProperty("Width").GetInt32());
            Assert.IsTrue(geometry.GetProperty("ClientHeight").GetInt32() >= screenshotResult.GetProperty("Height").GetInt32());
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (File.Exists(targetPath)) {
                File.Delete(targetPath);
            }

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can move a live window, return screenshot artifacts, and verify the new geometry.
    /// </summary>
    public void McpServer_NotepadWindowMutationRoundTrip_MovesWindowAndVerifiesGeometry() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchNotepadProcess(out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, NotepadLaunchTimeoutMs, 100, "window mutation");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched Notepad window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();
            int targetLeft = initialLeft >= 40 ? initialLeft - 20 : initialLeft + 20;
            int targetTop = initialTop >= 40 ? initialTop - 20 : initialTop + 20;

            JsonElement moveResult = client.CallTool(requestId++, "move_window", new {
                handle = windowHandle,
                x = targetLeft,
                y = targetTop,
                width = initialWidth,
                height = initialHeight,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(moveResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(moveResult.GetProperty("Count").GetInt32() >= 1);
            AssertScreenshotPathsExist(moveResult.GetProperty("AfterScreenshots"));

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(targetLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 10, "Moved window left position");
            AssertIntWithinTolerance(targetTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 10, "Moved window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 10, "Moved window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 10, "Moved window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP can run a higher-level workflow against a live Notepad window and return structured evidence.
    /// </summary>
    public void McpServer_NotepadWorkflowRoundTrip_PreparesForCodingAndCapturesArtifact() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchNotepadProcess(out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, NotepadLaunchTimeoutMs, 100, "workflow");
            string launchedWindowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(launchedWindowHandle), "Expected the launched Notepad window to expose a handle.");

            JsonElement workflowResult = client.CallTool(requestId++, "prepare_for_coding", new {
                handle = launchedWindowHandle,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(workflowResult.GetProperty("Success").GetBoolean());
            Assert.IsFalse(workflowResult.GetProperty("LayoutApplied").GetBoolean());
            Assert.AreEqual("prepare-for-coding", workflowResult.GetProperty("Action").GetString());
            bool resolvedWindowMatched = workflowResult.TryGetProperty("ResolvedWindow", out JsonElement resolvedWindow) &&
                resolvedWindow.ValueKind == JsonValueKind.Object &&
                resolvedWindow.GetProperty("ProcessId").GetInt32() == resolvedProcessId &&
                string.Equals(launchedWindowHandle, resolvedWindow.GetProperty("Handle").GetString(), StringComparison.OrdinalIgnoreCase);

            bool focusedWindowMatched = false;
            if (workflowResult.TryGetProperty("FocusedWindow", out JsonElement focusedWindow) && focusedWindow.ValueKind == JsonValueKind.Object) {
                focusedWindowMatched =
                    focusedWindow.GetProperty("ProcessId").GetInt32() == resolvedProcessId &&
                    string.Equals(launchedWindowHandle, focusedWindow.GetProperty("Handle").GetString(), StringComparison.OrdinalIgnoreCase);
            }

            JsonElement notes = workflowResult.GetProperty("Notes");
            bool hasNotes = notes.ValueKind == JsonValueKind.Array && notes.GetArrayLength() > 0;
            Assert.IsTrue(resolvedWindowMatched || focusedWindowMatched || hasNotes, "Expected the workflow to return either resolved/focused window evidence or explanatory notes.");
            AssertScreenshotPathsExist(workflowResult.GetProperty("AfterScreenshots"));
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP process allowlists permit a scoped live Notepad mutation when both process name and exact handle are supplied.
    /// </summary>
    public void McpServer_NotepadAllowedProcessPolicy_AllowsScopedMoveWindowMutation() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        string artifactDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "McpE2E", Guid.NewGuid().ToString("N"));
        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process notepad");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            LaunchNotepadProcess(out launcherProcessId, out resolvedProcessId);
            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, NotepadLaunchTimeoutMs, 100, "allowed-process policy");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched Notepad window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();
            int targetLeft = initialLeft >= 60 ? initialLeft - 30 : initialLeft + 30;
            int targetTop = initialTop >= 60 ? initialTop - 30 : initialTop + 30;

            JsonElement moveResult = client.CallTool(requestId++, "move_window", new {
                processName = "notepad",
                handle = windowHandle,
                x = targetLeft,
                y = targetTop,
                width = initialWidth,
                height = initialHeight,
                captureAfter = true,
                artifactDirectory
            });

            Assert.IsTrue(moveResult.GetProperty("Success").GetBoolean());
            Assert.IsTrue(moveResult.GetProperty("Count").GetInt32() >= 1);
            AssertScreenshotPathsExist(moveResult.GetProperty("AfterScreenshots"));

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(targetLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 10, "Allowed-policy moved window left position");
            AssertIntWithinTolerance(targetTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 10, "Allowed-policy moved window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 10, "Allowed-policy moved window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 10, "Allowed-policy moved window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);

            if (Directory.Exists(artifactDirectory)) {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP denied-process filters block a scoped live Notepad window mutation without changing geometry.
    /// </summary>
    public void McpServer_NotepadDeniedProcessPolicy_BlocksScopedMoveWindowMutation() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            LaunchNotepadProcess(out launcherProcessId, out resolvedProcessId);

            using var client = McpTestClient.Start("mcp serve --allow-mutations --deny-process notepad");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });

            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, NotepadLaunchTimeoutMs, 100, "denied-process policy");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the denied-policy Notepad window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();

            JsonElement toolError = client.CallToolExpectError(requestId++, "move_window", new {
                processName = "notepad",
                handle = windowHandle,
                x = initialLeft + 40,
                y = initialTop + 40,
                width = initialWidth,
                height = initialHeight
            });
            StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "denied-process policy");

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(initialLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 5, "Denied-policy blocked window left position");
            AssertIntWithinTolerance(initialTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 5, "Denied-policy blocked window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 5, "Denied-policy blocked window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 5, "Denied-policy blocked window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    /// <summary>
    /// Ensures MCP dry-run mode previews a scoped live Notepad window mutation without changing geometry.
    /// </summary>
    public void McpServer_NotepadDryRunPolicy_PreviewsScopedMoveWindowMutationWithoutChangingGeometry() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireDesktopChanges();

        int launcherProcessId = 0;
        int resolvedProcessId = 0;

        try {
            LaunchNotepadProcess(out launcherProcessId, out resolvedProcessId);

            using var client = McpTestClient.Start("mcp serve --dry-run --allow-process notepad");
            JsonElement initializeResult = client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });
            Assert.IsTrue(initializeResult.GetProperty("safetyPolicy").GetProperty("dryRun").GetBoolean());

            int requestId = 2;
            JsonElement launchedWindow = WaitForProcessWindow(client, ref requestId, resolvedProcessId, NotepadLaunchTimeoutMs, 100, "dry-run policy");
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the dry-run Notepad window to expose a handle.");

            JsonElement initialGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(initialGeometryResult.ValueKind == JsonValueKind.Array && initialGeometryResult.GetArrayLength() == 1);
            JsonElement initialGeometry = initialGeometryResult[0];

            int initialLeft = initialGeometry.GetProperty("WindowLeft").GetInt32();
            int initialTop = initialGeometry.GetProperty("WindowTop").GetInt32();
            int initialWidth = initialGeometry.GetProperty("WindowWidth").GetInt32();
            int initialHeight = initialGeometry.GetProperty("WindowHeight").GetInt32();

            JsonElement dryRunResult = client.CallTool(requestId++, "move_window", new {
                processName = "notepad",
                handle = windowHandle,
                x = initialLeft + 40,
                y = initialTop + 40,
                width = initialWidth,
                height = initialHeight
            });

            Assert.IsTrue(dryRunResult.GetProperty("dryRun").GetBoolean());
            Assert.IsFalse(dryRunResult.GetProperty("applied").GetBoolean());
            Assert.AreEqual("move_window", dryRunResult.GetProperty("toolName").GetString());
            Assert.AreEqual("dry-run", dryRunResult.GetProperty("safetyMode").GetString());
            Assert.AreEqual("notepad", dryRunResult.GetProperty("requestedProcesses")[0].GetString());

            JsonElement finalGeometryResult = client.CallTool(requestId++, "get_window_geometry", new {
                handle = windowHandle
            });
            Assert.IsTrue(finalGeometryResult.ValueKind == JsonValueKind.Array && finalGeometryResult.GetArrayLength() == 1);
            JsonElement finalGeometry = finalGeometryResult[0];

            AssertIntWithinTolerance(initialLeft, finalGeometry.GetProperty("WindowLeft").GetInt32(), 5, "Dry-run preserved window left position");
            AssertIntWithinTolerance(initialTop, finalGeometry.GetProperty("WindowTop").GetInt32(), 5, "Dry-run preserved window top position");
            AssertIntWithinTolerance(initialWidth, finalGeometry.GetProperty("WindowWidth").GetInt32(), 5, "Dry-run preserved window width");
            AssertIntWithinTolerance(initialHeight, finalGeometry.GetProperty("WindowHeight").GetInt32(), 5, "Dry-run preserved window height");
        } finally {
            KillProcessById(resolvedProcessId);
            KillProcessById(launcherProcessId);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    [TestCategory("ExperimentalUITest")]
    /// <summary>
    /// Ensures the live Edge omnibox key path is blocked when foreground-input fallback is requested without server opt-in.
    /// </summary>
    public void McpServer_EdgeForegroundInputPolicy_BlocksOmniboxEnterWithoutServerOptIn() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExperimentalDesktopChanges();

        string edgePath = RequireEdgeExecutablePath();
        string profileDirectory = CreateTemporaryDirectory("DesktopManager-EdgeProfile-");
        string diagnosticDirectory = CreateTemporaryDirectory(Path.Combine("DesktopManager.Tests", "McpE2E", "Experimental", "Edge-Blocked-"));
        string windowTargetName = "McpServerEndToEnd-EdgeOmniboxWindow-" + Guid.NewGuid().ToString("N");
        string windowTargetPath = DesktopStateStore.GetTargetPath(windowTargetName);
        string controlTargetName = "McpServerEndToEnd-EdgeOmniboxControl-" + Guid.NewGuid().ToString("N");
        string controlTargetPath = DesktopStateStore.GetControlTargetPath(controlTargetName);
        string startTitle = "Foreground Fallback Start " + Guid.NewGuid().ToString("N");
        string targetTitle = "Foreground Fallback Target " + Guid.NewGuid().ToString("N");
        string startPagePath = CreateForegroundFallbackPage(startTitle);
        string targetPagePath = CreateForegroundFallbackPage(targetTitle);
        Process? edgeProcess = null;
        int resolvedProcessId = 0;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process msedge");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });
            int requestId = 2;
            SaveEdgeOmniboxTarget(client, ref requestId, windowTargetName);
            SaveEdgeOmniboxControlTarget(client, ref requestId, controlTargetName);

            edgeProcess = LaunchEdgeProcess(edgePath, profileDirectory, BuildFileUrl(startPagePath));

            JsonElement waitResult = client.CallTool(requestId++, "wait_for_window", new {
                processName = "msedge",
                windowTitle = "*" + startTitle + "*",
                timeoutMs = EdgeLaunchTimeoutMs,
                intervalMs = 200
            });

            Assert.IsTrue(waitResult.GetProperty("Count").GetInt32() >= 1, "Expected the sacrificial Edge start page window to appear.");
            JsonElement launchedWindow = waitResult.GetProperty("Windows")[0];
            resolvedProcessId = launchedWindow.GetProperty("ProcessId").GetInt32();
            Assert.IsTrue(resolvedProcessId > 0);
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched Edge window to expose a handle.");

            WaitForEdgeOmnibox(client, ref requestId, windowHandle, controlTargetName);

            JsonElement setTextResult = client.CallTool(requestId++, "set_control_text", new {
                processName = "msedge",
                windowHandle,
                targetName = controlTargetName,
                text = BuildFileUrl(targetPagePath)
            });
            Assert.IsTrue(setTextResult.GetProperty("Success").GetBoolean());

            JsonElement toolError = client.CallToolExpectError(requestId++, "send_control_keys", new {
                processName = "msedge",
                windowHandle,
                targetName = controlTargetName,
                allowForegroundInput = true,
                keys = new[] { "VK_RETURN" }
            });
            StringAssert.Contains(toolError.GetProperty("message").GetString() ?? string.Empty, "--allow-foreground-input");
        } finally {
            TestHelper.SafeKillProcess(edgeProcess);
            TryDeleteFile(startPagePath);
            TryDeleteFile(targetPagePath);
            TryDeleteDirectory(profileDirectory);
            TryDeleteDirectory(diagnosticDirectory);
            TryDeleteFile(windowTargetPath);
            TryDeleteFile(controlTargetPath);
            KillProcessById(resolvedProcessId);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    [TestCategory("ExperimentalUITest")]
    /// <summary>
    /// Ensures the live Edge omnibox key path can navigate to a local page when both the MCP server and tool call explicitly opt into foreground-input fallback.
    /// </summary>
    public void McpServer_EdgeForegroundInputPolicy_AllowsOmniboxEnterWithServerOptIn() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        RequireNet8McpLiveHarness();
        TestHelper.RequireExperimentalDesktopChanges();

        string edgePath = RequireEdgeExecutablePath();
        string profileDirectory = CreateTemporaryDirectory("DesktopManager-EdgeProfile-");
        string diagnosticDirectory = CreateTemporaryDirectory(Path.Combine("DesktopManager.Tests", "McpE2E", "Experimental", "Edge-Allow-"));
        string windowTargetName = "McpServerEndToEnd-EdgeOmniboxWindow-" + Guid.NewGuid().ToString("N");
        string windowTargetPath = DesktopStateStore.GetTargetPath(windowTargetName);
        string controlTargetName = "McpServerEndToEnd-EdgeOmniboxControl-" + Guid.NewGuid().ToString("N");
        string controlTargetPath = DesktopStateStore.GetControlTargetPath(controlTargetName);
        string startTitle = "Foreground Fallback Start " + Guid.NewGuid().ToString("N");
        string targetTitle = "Foreground Fallback Target " + Guid.NewGuid().ToString("N");
        string startPagePath = CreateForegroundFallbackPage(startTitle);
        string targetPagePath = CreateForegroundFallbackPage(targetTitle);
        Process? edgeProcess = null;
        int resolvedProcessId = 0;
        bool preserveDiagnostics = false;

        try {
            using var client = McpTestClient.Start("mcp serve --allow-mutations --allow-process msedge --allow-foreground-input");
            client.SendRequest(1, "initialize", new {
                protocolVersion = "2025-06-18"
            });
            int requestId = 2;
            SaveEdgeOmniboxTarget(client, ref requestId, windowTargetName);
            SaveEdgeOmniboxControlTarget(client, ref requestId, controlTargetName);

            edgeProcess = LaunchEdgeProcess(edgePath, profileDirectory, BuildFileUrl(startPagePath));

            JsonElement waitResult = client.CallTool(requestId++, "wait_for_window", new {
                processName = "msedge",
                windowTitle = "*" + startTitle + "*",
                timeoutMs = EdgeLaunchTimeoutMs,
                intervalMs = 200
            });

            Assert.IsTrue(waitResult.GetProperty("Count").GetInt32() >= 1, "Expected the sacrificial Edge start page window to appear.");
            JsonElement launchedWindow = waitResult.GetProperty("Windows")[0];
            resolvedProcessId = launchedWindow.GetProperty("ProcessId").GetInt32();
            Assert.IsTrue(resolvedProcessId > 0);
            string windowHandle = launchedWindow.GetProperty("Handle").GetString() ?? string.Empty;
            Assert.IsFalse(string.IsNullOrWhiteSpace(windowHandle), "Expected the launched Edge window to expose a handle.");

            JsonElement focusResult = client.CallTool(requestId++, "focus_window", new {
                processName = "msedge",
                handle = windowHandle
            });
            Assert.IsTrue(focusResult.GetProperty("Success").GetBoolean());
            var decisionTrace = new List<string> {
                "Saved window target: " + windowTargetName,
                "Saved control target: " + controlTargetName,
                "Focused window handle: " + windowHandle
            };
            string targetUrl = BuildFileUrl(targetPagePath);
            string? navigationSafetyMode = TryNavigateEdgeOmnibox(client, ref requestId, windowHandle, windowTargetName, controlTargetName, targetUrl, decisionTrace);
            if (string.IsNullOrWhiteSpace(navigationSafetyMode)) {
                preserveDiagnostics = true;
                string bundlePath = CaptureEdgeForegroundDiagnostics(client, ref requestId, windowHandle, controlTargetName, diagnosticDirectory, decisionTrace);
                Assert.Inconclusive($"Edge did not expose a reusable omnibox control through MCP in this session, so the experimental foreground-input success harness could not complete. Diagnostic bundle: {bundlePath}");
            }

            string resolvedNavigationSafetyMode = navigationSafetyMode!;
            Assert.IsTrue(
                resolvedNavigationSafetyMode.IndexOf("foreground", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(resolvedNavigationSafetyMode, "window-key-input", StringComparison.OrdinalIgnoreCase),
                "Expected the Edge navigation safety mode to reflect either explicit foreground control input or the shared window-level key fallback.");
            decisionTrace.Add("Navigation safety mode: " + resolvedNavigationSafetyMode);

            try {
                JsonElement targetWindowWait = client.CallTool(requestId++, "wait_for_window", new {
                    processName = "msedge",
                    windowTitle = "*" + targetTitle + "*",
                    timeoutMs = EdgeLaunchTimeoutMs,
                    intervalMs = 200
                });

                Assert.IsTrue(targetWindowWait.GetProperty("Count").GetInt32() >= 1, "Expected Edge to navigate to the target page after explicit foreground-input opt-in.");
                JsonElement targetWindow = targetWindowWait.GetProperty("Windows")[0];
                Assert.AreEqual(resolvedProcessId, targetWindow.GetProperty("ProcessId").GetInt32(), "Expected the target page to reuse the same sacrificial Edge window process.");
            } catch (AssertFailedException ex) {
                preserveDiagnostics = true;
                decisionTrace.Add("Navigation verification failed: " + ex.Message);
                string bundlePath = CaptureEdgeForegroundDiagnostics(client, ref requestId, windowHandle, controlTargetName, diagnosticDirectory, decisionTrace);
                Assert.Inconclusive($"Edge accepted the experimental input path but did not complete navigation reliably in this session. Diagnostic bundle: {bundlePath}. Failure: {ex.Message}");
            }
        } finally {
            TestHelper.SafeKillProcess(edgeProcess);
            TryDeleteFile(startPagePath);
            TryDeleteFile(targetPagePath);
            TryDeleteDirectory(profileDirectory);
            if (!preserveDiagnostics) {
                TryDeleteDirectory(diagnosticDirectory);
            }
            TryDeleteFile(windowTargetPath);
            TryDeleteFile(controlTargetPath);
            KillProcessById(resolvedProcessId);
        }
    }

    private static int ReadResolvedProcessId(JsonElement launchResult) {
        JsonElement resolvedProcessId = launchResult.GetProperty("ResolvedProcessId");
        if (resolvedProcessId.ValueKind == JsonValueKind.Number) {
            return resolvedProcessId.GetInt32();
        }

        return launchResult.GetProperty("ProcessId").GetInt32();
    }

    private static void LaunchNotepadProcess(out int launcherProcessId, out int resolvedProcessId) {
        var automation = new DesktopAutomationService();
        DesktopProcessLaunchInfo launch = automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = "notepad.exe",
            WaitForInputIdleMilliseconds = 5000,
            WaitForWindowMilliseconds = NotepadLaunchTimeoutMs,
            WaitForWindowIntervalMilliseconds = 100,
            RequireWindow = true
        });

        launcherProcessId = launch.ProcessId;
        resolvedProcessId = launch.ResolvedProcessId ?? launch.ProcessId;
        Assert.IsTrue(launcherProcessId > 0, "Expected direct Notepad setup to return a launcher process id.");
        Assert.IsTrue(resolvedProcessId > 0, "Expected direct Notepad setup to resolve the live window process.");
        Assert.IsNotNull(launch.MainWindow, "Expected direct Notepad setup to resolve a visible window.");
    }

    private static void RequireNet8McpLiveHarness() {
#if NET472
        Assert.Inconclusive("Live MCP desktop harness runs only under net8.0-windows to avoid driving the same desktop twice through the shared net8 CLI executable.");
#endif
    }

    private static string RequireEdgeExecutablePath() {
        string[] candidates = {
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
        };

        foreach (string candidate in candidates) {
            if (File.Exists(candidate)) {
                return candidate;
            }
        }

        Assert.Inconclusive("Microsoft Edge was not found on this machine, so the live foreground-input MCP harness cannot run.");
        return string.Empty;
    }

    private static string CreateTemporaryDirectory(string prefix) {
        string path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateForegroundFallbackPage(string title) {
        string path = Path.Combine(Path.GetTempPath(), "DesktopManager-ForegroundFallback-" + Guid.NewGuid().ToString("N") + ".html");
        string html = @"<!doctype html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>" + title + @"</title>
</head>
<body style=""font-family:Segoe UI;padding:24px"">
    <h1>" + title + @"</h1>
    <textarea id=""editor"" autofocus style=""width:900px;height:260px"">seed</textarea>
    <script>
        window.addEventListener('load', function () {
            var editor = document.getElementById('editor');
            if (editor) {
                editor.focus();
            }
        });
    </script>
</body>
</html>";
        File.WriteAllText(path, html);
        return path;
    }

    private static string BuildFileUrl(string path) {
        return new Uri(path).AbsoluteUri;
    }

    private static string BuildEdgeArguments(string profileDirectory, string url) {
        return $"--user-data-dir=\"{profileDirectory}\" --no-first-run --new-window {url}";
    }

    private static Process? LaunchEdgeProcess(string edgePath, string profileDirectory, string url) {
        var startInfo = new ProcessStartInfo(edgePath, BuildEdgeArguments(profileDirectory, url)) {
            UseShellExecute = true
        };

        return Process.Start(startInfo);
    }

    private static void WaitForEdgeOmnibox(McpTestClient client, ref int requestId, string windowHandle, string controlTargetName, int timeoutMilliseconds = EdgeControlDiscoveryTimeoutMs) {
        if (TryWaitForEdgeOmnibox(client, ref requestId, windowHandle, controlTargetName, timeoutMilliseconds)) {
            return;
        }

        Assert.Fail($"Timed out after {timeoutMilliseconds}ms waiting for the Edge omnibox control to be discoverable through MCP.");
    }

    private static bool TryWaitForEdgeOmnibox(McpTestClient client, ref int requestId, string windowHandle, string controlTargetName, int timeoutMilliseconds) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            JsonElement controls = GetEdgeOmniboxControls(client, ref requestId, windowHandle, controlTargetName);
            if (controls.ValueKind == JsonValueKind.Array && controls.GetArrayLength() > 0) {
                return true;
            }

            System.Threading.Thread.Sleep(100);
        }

        return false;
    }

    private static string? TryNavigateEdgeOmnibox(McpTestClient client, ref int requestId, string windowHandle, string windowTargetName, string controlTargetName, string targetUrl, IList<string> decisionTrace) {
        for (int attempt = 0; attempt < 3; attempt++) {
            try {
                decisionTrace.Add($"Attempt {attempt + 1}: start");
                if (!TryWaitForEdgeOmnibox(client, ref requestId, windowHandle, controlTargetName, EdgeControlDiscoveryTimeoutMs)) {
                    decisionTrace.Add($"Attempt {attempt + 1}: named control target not resolved within {EdgeControlDiscoveryTimeoutMs}ms");
                    ClickEdgeOmniboxFallbackPoint(client, ref requestId, windowHandle, windowTargetName);
                    decisionTrace.Add($"Attempt {attempt + 1}: clicked named window target fallback {windowTargetName}");
                    WaitForEdgeOmnibox(client, ref requestId, windowHandle, controlTargetName, EdgeLaunchTimeoutMs);
                    decisionTrace.Add($"Attempt {attempt + 1}: named control target resolved after fallback click");
                } else {
                    decisionTrace.Add($"Attempt {attempt + 1}: named control target resolved without fallback");
                }

                JsonElement setTextResult = client.CallTool(requestId++, "set_control_text", new {
                    processName = "msedge",
                    windowHandle,
                    targetName = controlTargetName,
                    allowForegroundInput = true,
                    text = targetUrl
                });
                Assert.IsTrue(setTextResult.GetProperty("Success").GetBoolean(), "Expected Edge omnibox text entry to succeed once foreground-input fallback is explicitly allowed.");
                decisionTrace.Add($"Attempt {attempt + 1}: set_control_text succeeded with safety mode {setTextResult.GetProperty("SafetyMode").GetString()}");

                bool controlAvailableAfterText = TryWaitForEdgeOmnibox(client, ref requestId, windowHandle, controlTargetName, EdgeControlDiscoveryTimeoutMs);
                if (!controlAvailableAfterText) {
                    decisionTrace.Add($"Attempt {attempt + 1}: named control target disappeared after text entry");
                    ClickEdgeOmniboxFallbackPoint(client, ref requestId, windowHandle, windowTargetName);
                    decisionTrace.Add($"Attempt {attempt + 1}: clicked named window target fallback after text entry");
                    JsonElement sendWindowKeysResult = client.CallTool(requestId++, "send_window_keys", new {
                        processName = "msedge",
                        handle = windowHandle,
                        keys = new[] { "VK_RETURN" },
                        activate = true
                    });

                    Assert.IsTrue(sendWindowKeysResult.GetProperty("Success").GetBoolean(), "Expected the window-level Edge Enter fallback to succeed after omnibox text entry.");
                    decisionTrace.Add($"Attempt {attempt + 1}: send_window_keys fallback succeeded with safety mode {sendWindowKeysResult.GetProperty("SafetyMode").GetString()}");
                    return sendWindowKeysResult.GetProperty("SafetyMode").GetString();
                } else {
                    decisionTrace.Add($"Attempt {attempt + 1}: named control target remained available after text entry");
                }

                JsonElement sendKeysResult = client.CallTool(requestId++, "send_control_keys", new {
                    processName = "msedge",
                    windowHandle,
                    targetName = controlTargetName,
                    allowForegroundInput = true,
                    keys = new[] { "VK_RETURN" }
                });

                Assert.IsTrue(sendKeysResult.GetProperty("Success").GetBoolean(), "Expected Edge omnibox Enter to succeed once foreground-input fallback is explicitly allowed.");
                decisionTrace.Add($"Attempt {attempt + 1}: send_control_keys succeeded with safety mode {sendKeysResult.GetProperty("SafetyMode").GetString()}");
                return sendKeysResult.GetProperty("SafetyMode").GetString();
            } catch (AssertFailedException) when (attempt < 2) {
                decisionTrace.Add($"Attempt {attempt + 1}: assertion failed, retrying");
                System.Threading.Thread.Sleep(500);
            } catch (AssertFailedException) {
                decisionTrace.Add($"Attempt {attempt + 1}: assertion failed, giving up");
                return null;
            }
        }

        decisionTrace.Add("Navigation failed after all attempts");
        return null;
    }

    private static void ClickEdgeOmniboxFallbackPoint(McpTestClient client, ref int requestId, string windowHandle, string targetName) {
        JsonElement clickResult = client.CallTool(requestId++, "click_window_point", new {
            processName = "msedge",
            handle = windowHandle,
            targetName,
            activate = true,
            clientArea = false
        });
        Assert.IsTrue(clickResult.GetProperty("Success").GetBoolean(), "Expected the geometry-assisted Edge omnibox fallback click to succeed.");
        System.Threading.Thread.Sleep(250);
    }

    private static void SaveEdgeOmniboxTarget(McpTestClient client, ref int requestId, string targetName) {
        JsonElement saveTargetResult = client.CallTool(requestId++, "save_window_target", new {
            name = targetName,
            description = "Experimental Edge omnibox fallback point",
            xRatio = EdgeOmniboxFallbackXRatio,
            yRatio = EdgeOmniboxFallbackYRatio,
            clientArea = false
        });
        Assert.AreEqual(targetName, saveTargetResult.GetProperty("Name").GetString());
    }

    private static void SaveEdgeOmniboxControlTarget(McpTestClient client, ref int requestId, string targetName) {
        JsonElement saveResult = client.CallTool(requestId++, "save_control_target", new {
            name = targetName,
            description = "Experimental Edge omnibox control",
            controlClassName = "OmniboxViewViews",
            controlType = "Edit",
            controlText = "Address and search bar",
            isKeyboardFocusable = true,
            supportsForegroundInputFallback = true,
            uiAutomation = true
        });
        Assert.AreEqual(targetName, saveResult.GetProperty("Name").GetString());
    }

    private static string CaptureEdgeForegroundDiagnostics(McpTestClient client, ref int requestId, string windowHandle, string controlTargetName, string diagnosticDirectory, IReadOnlyList<string>? decisionTrace = null) {
        Directory.CreateDirectory(diagnosticDirectory);

        string screenshotPath = Path.Combine(diagnosticDirectory, "edge-window.png");
        JsonElement screenshotResult = client.CallTool(requestId++, "screenshot_window", new {
            processName = "msedge",
            windowHandle,
            outputPath = screenshotPath
        });

        JsonElement diagnostics = client.CallTool(requestId++, "diagnose_window_controls", new {
            processName = "msedge",
            windowHandle,
            targetName = controlTargetName,
            sampleLimit = 10
        });

        JsonElement controls = GetEdgeOmniboxControls(client, ref requestId, windowHandle, controlTargetName);

        File.WriteAllText(Path.Combine(diagnosticDirectory, "screenshot.json"), JsonSerializer.Serialize(JsonDocument.Parse(screenshotResult.GetRawText()).RootElement, new JsonSerializerOptions {
            WriteIndented = true
        }));
        File.WriteAllText(Path.Combine(diagnosticDirectory, "diagnostics.json"), JsonSerializer.Serialize(JsonDocument.Parse(diagnostics.GetRawText()).RootElement, new JsonSerializerOptions {
            WriteIndented = true
        }));
        File.WriteAllText(Path.Combine(diagnosticDirectory, "controls.json"), JsonSerializer.Serialize(JsonDocument.Parse(controls.GetRawText()).RootElement, new JsonSerializerOptions {
            WriteIndented = true
        }));
        if (decisionTrace != null && decisionTrace.Count > 0) {
            File.WriteAllLines(Path.Combine(diagnosticDirectory, "decision-trace.txt"), decisionTrace);
        }
        WriteEdgeForegroundComparisonReport(diagnosticDirectory);

        return diagnosticDirectory;
    }

    private static void WriteEdgeForegroundComparisonReport(string diagnosticDirectory) {
        string? familyPrefix = GetEdgeDiagnosticFamilyPrefix(diagnosticDirectory);
        if (string.IsNullOrWhiteSpace(familyPrefix)) {
            return;
        }

        string parentDirectory = Path.GetDirectoryName(diagnosticDirectory) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory)) {
            return;
        }

        DirectoryInfo? previousBundle = new DirectoryInfo(parentDirectory)
            .EnumerateDirectories(familyPrefix + "*", SearchOption.TopDirectoryOnly)
            .Where(directory => !string.Equals(directory.FullName, diagnosticDirectory, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(directory => directory.LastWriteTimeUtc)
            .FirstOrDefault();

        var summaryLines = new List<string> {
            "Current bundle: " + diagnosticDirectory
        };

        if (previousBundle == null) {
            summaryLines.Add("Previous bundle: none");
            File.WriteAllLines(Path.Combine(diagnosticDirectory, "comparison.txt"), summaryLines);
            return;
        }

        summaryLines.Add("Previous bundle: " + previousBundle.FullName);

        EdgeDiagnosticSnapshot currentSnapshot = ReadEdgeDiagnosticSnapshot(diagnosticDirectory);
        EdgeDiagnosticSnapshot previousSnapshot = ReadEdgeDiagnosticSnapshot(previousBundle.FullName);

        summaryLines.Add(string.Format("Matched controls: current {0}, previous {1}, delta {2}", currentSnapshot.MatchedControlCount, previousSnapshot.MatchedControlCount, currentSnapshot.MatchedControlCount - previousSnapshot.MatchedControlCount));
        summaryLines.Add(string.Format("Effective controls: current {0}, previous {1}, delta {2}", currentSnapshot.EffectiveControlCount, previousSnapshot.EffectiveControlCount, currentSnapshot.EffectiveControlCount - previousSnapshot.EffectiveControlCount));
        summaryLines.Add(string.Format("Listed controls: current {0}, previous {1}, delta {2}", currentSnapshot.ListedControlCount, previousSnapshot.ListedControlCount, currentSnapshot.ListedControlCount - previousSnapshot.ListedControlCount));
        summaryLines.Add(string.Format("Elapsed milliseconds: current {0}, previous {1}, delta {2}", currentSnapshot.ElapsedMilliseconds, previousSnapshot.ElapsedMilliseconds, currentSnapshot.ElapsedMilliseconds - previousSnapshot.ElapsedMilliseconds));
        summaryLines.Add("Preferred root handle: current " + currentSnapshot.PreferredRootHandle + ", previous " + previousSnapshot.PreferredRootHandle);
        summaryLines.Add("Preferred root reused: current " + currentSnapshot.UsedPreferredRoot + ", previous " + previousSnapshot.UsedPreferredRoot);
        summaryLines.Add("Cached controls reused: current " + currentSnapshot.UsedCachedControls + ", previous " + previousSnapshot.UsedCachedControls);
        summaryLines.Add("First listed control: current " + currentSnapshot.FirstListedControlSummary + ", previous " + previousSnapshot.FirstListedControlSummary);
        summaryLines.Add("Sample control classes: current " + currentSnapshot.SampleControlClasses + ", previous " + previousSnapshot.SampleControlClasses);

        File.WriteAllLines(Path.Combine(diagnosticDirectory, "comparison.txt"), summaryLines);
    }

    private static string? GetEdgeDiagnosticFamilyPrefix(string diagnosticDirectory) {
        string name = Path.GetFileName(diagnosticDirectory);
        if (name.StartsWith("Edge-Allow-", StringComparison.OrdinalIgnoreCase)) {
            return "Edge-Allow-";
        }

        if (name.StartsWith("Edge-Blocked-", StringComparison.OrdinalIgnoreCase)) {
            return "Edge-Blocked-";
        }

        return null;
    }

    private static EdgeDiagnosticSnapshot ReadEdgeDiagnosticSnapshot(string diagnosticDirectory) {
        string diagnosticsPath = Path.Combine(diagnosticDirectory, "diagnostics.json");
        string controlsPath = Path.Combine(diagnosticDirectory, "controls.json");
        var snapshot = new EdgeDiagnosticSnapshot();

        if (File.Exists(diagnosticsPath)) {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(diagnosticsPath));
            if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0) {
                JsonElement diagnostic = document.RootElement[0];
                snapshot.MatchedControlCount = ReadOptionalInt32(diagnostic, "MatchedControlCount");
                snapshot.EffectiveControlCount = ReadOptionalInt32(diagnostic, "EffectiveControlCount");
                snapshot.ElapsedMilliseconds = ReadOptionalInt32(diagnostic, "ElapsedMilliseconds");
                snapshot.PreferredRootHandle = ReadOptionalString(diagnostic, "PreferredUiAutomationRootHandle");
                snapshot.UsedPreferredRoot = ReadOptionalBoolean(diagnostic, "UsedPreferredUiAutomationRoot");
                snapshot.UsedCachedControls = ReadOptionalBoolean(diagnostic, "UsedCachedUiAutomationControls");
                snapshot.SampleControlClasses = ReadSampleControlClasses(diagnostic);
            }
        }

        if (File.Exists(controlsPath)) {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(controlsPath));
            if (document.RootElement.ValueKind == JsonValueKind.Array) {
                snapshot.ListedControlCount = document.RootElement.GetArrayLength();
                if (snapshot.ListedControlCount > 0) {
                    JsonElement firstControl = document.RootElement[0];
                    snapshot.FirstListedControlSummary = string.Format(
                        "{0} / {1} / {2}",
                        ReadOptionalString(firstControl, "ClassName"),
                        ReadOptionalString(firstControl, "ControlType"),
                        ReadOptionalString(firstControl, "AutomationId"));
                }
            }
        }

        return snapshot;
    }

    private static int ReadOptionalInt32(JsonElement element, string propertyName) {
        if (element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.Number) {
            return value.GetInt32();
        }

        return 0;
    }

    private static bool ReadOptionalBoolean(JsonElement element, string propertyName) {
        if (element.TryGetProperty(propertyName, out JsonElement value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)) {
            return value.GetBoolean();
        }

        return false;
    }

    private static string ReadOptionalString(JsonElement element, string propertyName) {
        if (element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String) {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ReadSampleControlClasses(JsonElement diagnostic) {
        if (!diagnostic.TryGetProperty("SampleControls", out JsonElement sampleControls) || sampleControls.ValueKind != JsonValueKind.Array) {
            return string.Empty;
        }

        return string.Join(", ", sampleControls.EnumerateArray().Take(5).Select(control => ReadOptionalString(control, "ClassName")).Where(className => !string.IsNullOrWhiteSpace(className)));
    }

    private sealed class EdgeDiagnosticSnapshot {
        public int MatchedControlCount { get; set; }
        public int EffectiveControlCount { get; set; }
        public int ListedControlCount { get; set; }
        public int ElapsedMilliseconds { get; set; }
        public string PreferredRootHandle { get; set; } = string.Empty;
        public bool UsedPreferredRoot { get; set; }
        public bool UsedCachedControls { get; set; }
        public string FirstListedControlSummary { get; set; } = string.Empty;
        public string SampleControlClasses { get; set; } = string.Empty;
    }

    private static JsonElement GetEdgeOmniboxControls(McpTestClient client, ref int requestId, string windowHandle, string controlTargetName) {
        JsonElement namedTargetControls = client.CallTool(requestId++, "list_window_controls", new {
            processName = "msedge",
            windowHandle,
            targetName = controlTargetName
        });
        if (namedTargetControls.ValueKind == JsonValueKind.Array && namedTargetControls.GetArrayLength() > 0) {
            return namedTargetControls;
        }

        JsonElement preferredControls = client.CallTool(requestId++, "list_window_controls", new {
            processName = "msedge",
            windowHandle,
            controlClassName = "OmniboxViewViews",
            controlType = "Edit",
            controlText = "Address and search bar",
            isKeyboardFocusable = true,
            supportsForegroundInputFallback = true,
            uiAutomation = true,
            ensureForegroundWindow = true
        });
        if (preferredControls.ValueKind == JsonValueKind.Array && preferredControls.GetArrayLength() > 0) {
            return preferredControls;
        }

        return client.CallTool(requestId++, "list_window_controls", new {
            processName = "msedge",
            windowHandle,
            controlType = "Edit",
            isKeyboardFocusable = true,
            supportsForegroundInputFallback = true,
            uiAutomation = true,
            ensureForegroundWindow = true
        });
    }

    private static JsonElement WaitForProcessWindow(McpTestClient client, ref int requestId, int processId, int timeoutMilliseconds, int intervalMilliseconds, string scenario) {
        JsonElement windows = client.CallTool(requestId++, "wait_for_window", new {
            processId,
            timeoutMs = timeoutMilliseconds,
            intervalMs = intervalMilliseconds
        });
        Assert.IsTrue(windows.GetProperty("Count").GetInt32() >= 1, $"Expected MCP to resolve a live window for the {scenario} scenario.");
        return windows.GetProperty("Windows")[0];
    }

    private static void KillProcessById(int processId) {
        if (processId <= 0) {
            return;
        }

        try {
            using Process process = Process.GetProcessById(processId);
            TestHelper.SafeKillProcess(process);
        } catch {
            // Ignore cleanup failures for already exited processes.
        }
    }

    private static void TryDeleteDirectory(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        } catch {
            // Ignore cleanup failures for files still locked by the browser.
        }
    }

    private static void TryDeleteFile(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch {
            // Ignore cleanup failures for already removed files.
        }
    }

    private static JsonElement FindEditableNotepadControl(JsonElement controls) {
        if (TryFindEditableNotepadControl(controls, out JsonElement control)) {
            return control;
        }

        Assert.Inconclusive("No editable Notepad control with background text support was exposed through MCP.");
        return default;
    }

    private static JsonElement WaitForEditableNotepadControl(McpTestClient client, ref int requestId, string windowHandle, int timeoutMilliseconds, int intervalMilliseconds) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            JsonElement controls = client.CallTool(requestId++, "list_window_controls", new {
                windowHandle,
                includeUiAutomation = true,
                ensureForegroundWindow = true
            });
            if (controls.ValueKind == JsonValueKind.Array &&
                controls.GetArrayLength() > 0 &&
                TryFindEditableNotepadControl(controls, out JsonElement control)) {
                return control;
            }

            System.Threading.Thread.Sleep(intervalMilliseconds);
        }

        Assert.Fail($"Timed out after {timeoutMilliseconds}ms waiting for Notepad to expose an editable control through MCP.");
        return default;
    }

    private static bool TryFindEditableNotepadControl(JsonElement controls, out JsonElement control) {
        JsonElement? richEdit = null;
        JsonElement? fallback = null;

        foreach (JsonElement candidate in controls.EnumerateArray()) {
            string className = candidate.GetProperty("ClassName").GetString() ?? string.Empty;
            bool supportsBackgroundText = candidate.GetProperty("SupportsBackgroundText").GetBoolean();
            if (!supportsBackgroundText) {
                continue;
            }

            if (string.Equals(className, "RichEditD2DPT", StringComparison.OrdinalIgnoreCase)) {
                richEdit = candidate.Clone();
                break;
            }

            if (fallback == null &&
                (string.Equals(className, "NotepadTextBox", StringComparison.OrdinalIgnoreCase) ||
                 className.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 className.IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0)) {
                fallback = candidate.Clone();
            }
        }

        if (richEdit.HasValue) {
            control = richEdit.Value;
            return true;
        }

        if (fallback.HasValue) {
            control = fallback.Value;
            return true;
        }

        control = default;
        return false;
    }

    private static void AssertScreenshotPathsExist(JsonElement screenshots) {
        if (screenshots.ValueKind != JsonValueKind.Array) {
            Assert.Fail("Expected screenshots to be returned as an array.");
        }

        Assert.IsTrue(screenshots.GetArrayLength() > 0, "Expected at least one screenshot artifact.");
        foreach (JsonElement screenshot in screenshots.EnumerateArray()) {
            string? path = screenshot.GetProperty("Path").GetString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(path), "Expected a non-empty artifact path.");
            Assert.IsTrue(File.Exists(path), $"Expected artifact file to exist: {path}");
        }
    }

    private static void AssertIntWithinTolerance(int expected, int actual, int tolerance, string label) {
        int delta = Math.Abs(expected - actual);
        Assert.IsTrue(delta <= tolerance, $"{label} expected {expected} but was {actual} (delta {delta}, tolerance {tolerance}).");
    }
}
