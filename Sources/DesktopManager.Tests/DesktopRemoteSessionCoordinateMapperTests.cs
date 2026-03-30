using System;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for <see cref="DesktopRemoteSessionCoordinateMapper"/>.
/// </summary>
public class DesktopRemoteSessionCoordinateMapperTests
{
    [TestMethod]
    /// <summary>
    /// Frame coordinates should map into the configured source viewport.
    /// </summary>
    public void MapFrameToSourceViewport_MapsIntoViewport() {
        DesktopRemoteSessionInfo session = CreateSession(
            left: 100,
            top: 200,
            viewportWidth: 1280,
            viewportHeight: 720,
            frameWidth: 640,
            frameHeight: 360);

        (int x, int y) = DesktopRemoteSessionCoordinateMapper.MapFrameToSourceViewport(session, 320, 180);

        Assert.AreEqual(741, x);
        Assert.AreEqual(561, y);
    }

    [TestMethod]
    /// <summary>
    /// Out-of-range frame coordinates should clamp into the source viewport bounds.
    /// </summary>
    public void MapFrameToSourceViewport_ClampsOutsideTheFrame() {
        DesktopRemoteSessionInfo session = CreateSession(
            left: -50,
            top: 25,
            viewportWidth: 1920,
            viewportHeight: 1080,
            frameWidth: 960,
            frameHeight: 540);

        (int x, int y) = DesktopRemoteSessionCoordinateMapper.MapFrameToSourceViewport(session, 4000, -200);

        Assert.AreEqual(1869, x);
        Assert.AreEqual(25, y);
    }

    [TestMethod]
    /// <summary>
    /// Source viewport coordinates should map back into frame space.
    /// </summary>
    public void MapSourceViewportToFrame_MapsBackIntoFrame() {
        DesktopRemoteSessionInfo session = CreateSession(
            left: 100,
            top: 200,
            viewportWidth: 1280,
            viewportHeight: 720,
            frameWidth: 640,
            frameHeight: 360);

        (int x, int y) = DesktopRemoteSessionCoordinateMapper.MapSourceViewportToFrame(session, 741, 561);

        Assert.AreEqual(320, x);
        Assert.AreEqual(180, y);
    }

    private static DesktopRemoteSessionInfo CreateSession(
        int left,
        int top,
        int viewportWidth,
        int viewportHeight,
        int frameWidth,
        int frameHeight) {
        return new DesktopRemoteSessionInfo {
            SessionId = "session-1",
            Target = new DesktopRemoteSessionTarget {
                TargetKind = DesktopRemoteSessionTargetKind.Window,
                WindowId = "0x123",
                DisplayName = "Target"
            },
            SourceViewport = new DesktopRemoteSessionViewport {
                Left = left,
                Top = top,
                Width = viewportWidth,
                Height = viewportHeight,
                DpiX = 96,
                DpiY = 96,
                ScaleX = 1,
                ScaleY = 1
            },
            FrameWidth = frameWidth,
            FrameHeight = frameHeight,
            CursorIncluded = true,
            ActiveWindowId = "0x123",
            CreatedUtc = new DateTimeOffset(2026, 03, 29, 14, 00, 00, TimeSpan.Zero)
        };
    }
}
