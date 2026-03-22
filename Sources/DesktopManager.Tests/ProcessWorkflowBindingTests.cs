#if NET8_0_OR_GREATER
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for CLI launch-and-wait process binding decisions.
/// </summary>
public class ProcessWorkflowBindingTests {
    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait prefers a resolved window process when one is available.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_UsesResolvedProcessIdWhenAvailable() {
        var launch = new global::DesktopManager.Cli.ProcessLaunchResult {
            FilePath = "notepad.exe",
            ProcessId = 100,
            ResolvedProcessId = 200
        };

        global::DesktopManager.Cli.LaunchWaitBindingPlan plan = global::DesktopManager.Cli.DesktopOperations.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: "*Launch*",
            launchWindowClassNamePattern: "Notepad",
            windowTitlePattern: "*Final*",
            windowClassNamePattern: "NotepadFinal",
            includeHidden: true,
            includeEmpty: false,
            all: true,
            followProcessFamily: true);

        Assert.AreEqual("resolved-process-id", plan.WaitBinding);
        Assert.AreEqual(200, plan.BoundProcessId);
        Assert.IsNull(plan.BoundProcessName);
        Assert.AreEqual(200, plan.Criteria.ProcessId);
        Assert.AreEqual("*Final*", plan.Criteria.TitlePattern);
        Assert.AreEqual("NotepadFinal", plan.Criteria.ClassNamePattern);
        Assert.IsTrue(plan.Criteria.IncludeHidden);
        Assert.IsTrue(plan.Criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait uses the launcher process by default.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_UsesLauncherProcessIdByDefault() {
        var launch = new global::DesktopManager.Cli.ProcessLaunchResult {
            FilePath = "notepad.exe",
            ProcessId = 100
        };

        global::DesktopManager.Cli.LaunchWaitBindingPlan plan = global::DesktopManager.Cli.DesktopOperations.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: "*Launch*",
            launchWindowClassNamePattern: "LaunchClass",
            windowTitlePattern: null,
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmpty: true,
            all: false,
            followProcessFamily: false);

        Assert.AreEqual("launcher-process-id", plan.WaitBinding);
        Assert.AreEqual(100, plan.BoundProcessId);
        Assert.IsNull(plan.BoundProcessName);
        Assert.AreEqual(100, plan.Criteria.ProcessId);
        Assert.AreEqual("*Launch*", plan.Criteria.TitlePattern);
        Assert.AreEqual("LaunchClass", plan.Criteria.ClassNamePattern);
        Assert.IsTrue(plan.Criteria.IncludeEmptyTitles);
        Assert.IsFalse(plan.Criteria.All);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait can opt into following a same-name process family.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_UsesProcessFamilyWhenRequested() {
        var launch = new global::DesktopManager.Cli.ProcessLaunchResult {
            FilePath = "\"C:\\Program Files\\Microsoft VS Code\\Code.exe\"",
            ProcessId = 100
        };

        global::DesktopManager.Cli.LaunchWaitBindingPlan plan = global::DesktopManager.Cli.DesktopOperations.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: null,
            launchWindowClassNamePattern: null,
            windowTitlePattern: "*Code*",
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmpty: false,
            all: false,
            followProcessFamily: true);

        Assert.AreEqual("process-name-family", plan.WaitBinding);
        Assert.IsNull(plan.BoundProcessId);
        Assert.AreEqual("Code", plan.BoundProcessName);
        Assert.IsNull(plan.Criteria.ProcessId);
        Assert.AreEqual("Code", plan.Criteria.ProcessNamePattern);
        Assert.AreEqual("*Code*", plan.Criteria.TitlePattern);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait falls back to the launcher process when no process-family hint can be inferred.
    /// </summary>
    public void CreateLaunchWaitBindingPlan_FallsBackToLauncherProcessWhenProcessFamilyCannotBeInferred() {
        var launch = new global::DesktopManager.Cli.ProcessLaunchResult {
            FilePath = "\"\"",
            ProcessId = 100
        };

        global::DesktopManager.Cli.LaunchWaitBindingPlan plan = global::DesktopManager.Cli.DesktopOperations.CreateLaunchWaitBindingPlan(
            launch,
            launchWindowTitlePattern: null,
            launchWindowClassNamePattern: null,
            windowTitlePattern: null,
            windowClassNamePattern: null,
            includeHidden: false,
            includeEmpty: false,
            all: false,
            followProcessFamily: true);

        Assert.AreEqual("launcher-process-id", plan.WaitBinding);
        Assert.AreEqual(100, plan.BoundProcessId);
        Assert.IsNull(plan.BoundProcessName);
        Assert.AreEqual(100, plan.Criteria.ProcessId);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait result payloads keep resolved-process binding metadata in structured JSON output.
    /// </summary>
    public void LaunchAndWaitResult_SerializesResolvedProcessBindingMetadata() {
        var result = new global::DesktopManager.Cli.LaunchAndWaitResult {
            Action = "launch-and-wait-for-window",
            Success = true,
            WaitBinding = "resolved-process-id",
            BoundProcessId = 200,
            BoundProcessName = null,
            Launch = new global::DesktopManager.Cli.ProcessLaunchResult {
                FilePath = "notepad.exe",
                ProcessId = 100,
                ResolvedProcessId = 200
            },
            WindowWait = new global::DesktopManager.Cli.WaitForWindowResult {
                Count = 1
            }
        };

        JsonElement json = JsonSerializer.SerializeToElement(result, global::DesktopManager.Cli.JsonUtilities.SerializerOptions);

        Assert.AreEqual("resolved-process-id", json.GetProperty("WaitBinding").GetString());
        Assert.AreEqual(200, json.GetProperty("BoundProcessId").GetInt32());
        Assert.AreEqual(JsonValueKind.Null, json.GetProperty("BoundProcessName").ValueKind);
    }

    [TestMethod]
    /// <summary>
    /// Ensures launch-and-wait result payloads keep process-family binding metadata in structured JSON output.
    /// </summary>
    public void LaunchAndWaitResult_SerializesProcessFamilyBindingMetadata() {
        var result = new global::DesktopManager.Cli.LaunchAndWaitResult {
            Action = "launch-and-wait-for-window",
            Success = true,
            WaitBinding = "process-name-family",
            BoundProcessId = null,
            BoundProcessName = "Code",
            Launch = new global::DesktopManager.Cli.ProcessLaunchResult {
                FilePath = "\"C:\\Program Files\\Microsoft VS Code\\Code.exe\"",
                ProcessId = 100
            },
            WindowWait = new global::DesktopManager.Cli.WaitForWindowResult {
                Count = 1
            }
        };

        JsonElement json = JsonSerializer.SerializeToElement(result, global::DesktopManager.Cli.JsonUtilities.SerializerOptions);

        Assert.AreEqual("process-name-family", json.GetProperty("WaitBinding").GetString());
        Assert.AreEqual(JsonValueKind.Null, json.GetProperty("BoundProcessId").ValueKind);
        Assert.AreEqual("Code", json.GetProperty("BoundProcessName").GetString());
    }
}
#endif
