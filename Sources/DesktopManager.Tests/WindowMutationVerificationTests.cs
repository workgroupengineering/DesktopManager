#if NET8_0_OR_GREATER
using System;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for window mutation verification helpers.
/// </summary>
public class WindowMutationVerificationTests {
    [TestMethod]
    /// <summary>
    /// Ensures move verification reports a successful geometry match within the requested tolerance.
    /// </summary>
    public void BuildWindowPostconditionVerificationResult_VerifiesRequestedGeometry() {
        var expected = new[] {
            new WindowInfo {
                Title = "Editor",
                Handle = new IntPtr(0x1001),
                ProcessId = 10,
                ThreadId = 1,
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 600,
                MonitorIndex = 1,
                State = WindowState.Normal
            }
        };
        var observed = new[] {
            new WindowInfo {
                Title = "Editor",
                Handle = new IntPtr(0x1001),
                ProcessId = 10,
                ThreadId = 1,
                Left = 6,
                Top = 4,
                Right = 806,
                Bottom = 604,
                MonitorIndex = 1,
                State = WindowState.Normal
            }
        };

        global::DesktopManager.Cli.WindowMutationVerificationResult verification = global::DesktopManager.Cli.DesktopOperations.BuildWindowPostconditionVerificationResult(
            "move",
            expected,
            observed,
            activeWindow: null,
            tolerancePixels: 10,
            monitorIndex: 1,
            x: 0,
            y: 0,
            width: 800,
            height: 600);

        Assert.IsTrue(verification.Verified);
        Assert.AreEqual("geometry", verification.Mode);
        Assert.AreEqual(1, verification.ExpectedCount);
        Assert.AreEqual(1, verification.ObservedCount);
        Assert.AreEqual(1, verification.MatchedCount);
        Assert.AreEqual(0, verification.MismatchCount);
    }

    [TestMethod]
    /// <summary>
    /// Ensures focus verification fails when a different window owns the foreground after the action.
    /// </summary>
    public void BuildWindowPostconditionVerificationResult_FailsWhenForegroundDoesNotMatch() {
        var expected = new[] {
            new WindowInfo {
                Title = "Editor",
                Handle = new IntPtr(0x1001),
                ProcessId = 10,
                ThreadId = 1,
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 600,
                MonitorIndex = 1,
                State = WindowState.Normal
            }
        };
        var observed = new[] {
            new WindowInfo {
                Title = "Editor",
                Handle = new IntPtr(0x1001),
                ProcessId = 10,
                ThreadId = 1,
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 600,
                MonitorIndex = 1,
                State = WindowState.Normal
            }
        };
        var activeWindow = new WindowInfo {
            Title = "Browser",
            Handle = new IntPtr(0x2002),
            ProcessId = 11,
            ThreadId = 2,
            Left = 0,
            Top = 0,
            Right = 900,
            Bottom = 700,
            MonitorIndex = 1,
            State = WindowState.Normal
        };

        global::DesktopManager.Cli.WindowMutationVerificationResult verification = global::DesktopManager.Cli.DesktopOperations.BuildWindowPostconditionVerificationResult(
            "focus",
            expected,
            observed,
            activeWindow,
            tolerancePixels: 10,
            requireForegroundMatch: true);

        Assert.IsFalse(verification.Verified);
        Assert.AreEqual("foreground", verification.Mode);
        Assert.AreEqual(0, verification.MatchedCount);
        Assert.AreEqual(1, verification.MismatchCount);
        Assert.IsTrue(verification.Notes.Count > 0);
    }

    [TestMethod]
    /// <summary>
    /// Ensures minimize verification checks the reported window state instead of only the presence of the window.
    /// </summary>
    public void BuildWindowPostconditionVerificationResult_FailsWhenMinimizeStateDidNotStick() {
        var expected = new[] {
            new WindowInfo {
                Title = "Editor",
                Handle = new IntPtr(0x1001),
                ProcessId = 10,
                ThreadId = 1,
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 600,
                MonitorIndex = 1,
                State = WindowState.Normal
            }
        };
        var observed = new[] {
            new WindowInfo {
                Title = "Editor",
                Handle = new IntPtr(0x1001),
                ProcessId = 10,
                ThreadId = 1,
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 600,
                MonitorIndex = 1,
                State = WindowState.Normal
            }
        };

        global::DesktopManager.Cli.WindowMutationVerificationResult verification = global::DesktopManager.Cli.DesktopOperations.BuildWindowPostconditionVerificationResult(
            "minimize",
            expected,
            observed,
            activeWindow: null,
            tolerancePixels: 10);

        Assert.IsFalse(verification.Verified);
        Assert.AreEqual("window-state", verification.Mode);
        Assert.AreEqual(0, verification.MatchedCount);
        Assert.AreEqual(1, verification.MismatchCount);
        StringAssert.Contains(verification.Summary, "Minimize");
    }
}
#endif
