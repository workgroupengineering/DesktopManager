namespace DesktopManager;

/// <summary>
/// Maps coordinates between encoded remote-session frames and the source viewport in desktop space.
/// </summary>
public static class DesktopRemoteSessionCoordinateMapper
{
    /// <summary>
    /// Maps a coordinate from encoded frame space into the source viewport in desktop space.
    /// </summary>
    /// <param name="session">Remote session metadata describing the source viewport and frame size.</param>
    /// <param name="frameX">X coordinate within the encoded frame.</param>
    /// <param name="frameY">Y coordinate within the encoded frame.</param>
    /// <returns>The mapped coordinate in source viewport space.</returns>
    public static (int X, int Y) MapFrameToSourceViewport(DesktopRemoteSessionInfo session, int frameX, int frameY)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        int boundedFrameWidth = Math.Max(1, session.FrameWidth);
        int boundedFrameHeight = Math.Max(1, session.FrameHeight);
        int clampedX = Clamp(frameX, 0, boundedFrameWidth - 1);
        int clampedY = Clamp(frameY, 0, boundedFrameHeight - 1);

        int viewportWidth = Math.Max(1, session.SourceViewport.Width);
        int viewportHeight = Math.Max(1, session.SourceViewport.Height);

        int mappedX = session.SourceViewport.Left + ScaleCoordinate(clampedX, boundedFrameWidth, viewportWidth);
        int mappedY = session.SourceViewport.Top + ScaleCoordinate(clampedY, boundedFrameHeight, viewportHeight);
        return (mappedX, mappedY);
    }

    /// <summary>
    /// Maps a coordinate from source viewport space in desktop coordinates into encoded frame space.
    /// </summary>
    /// <param name="session">Remote session metadata describing the source viewport and frame size.</param>
    /// <param name="sourceX">X coordinate within the source viewport in desktop space.</param>
    /// <param name="sourceY">Y coordinate within the source viewport in desktop space.</param>
    /// <returns>The mapped coordinate in encoded frame space.</returns>
    public static (int X, int Y) MapSourceViewportToFrame(DesktopRemoteSessionInfo session, int sourceX, int sourceY)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        int viewportWidth = Math.Max(1, session.SourceViewport.Width);
        int viewportHeight = Math.Max(1, session.SourceViewport.Height);
        int localX = Clamp(sourceX - session.SourceViewport.Left, 0, viewportWidth - 1);
        int localY = Clamp(sourceY - session.SourceViewport.Top, 0, viewportHeight - 1);

        int frameWidth = Math.Max(1, session.FrameWidth);
        int frameHeight = Math.Max(1, session.FrameHeight);
        return (
            ScaleCoordinate(localX, viewportWidth, frameWidth),
            ScaleCoordinate(localY, viewportHeight, frameHeight));
    }

    private static int ScaleCoordinate(int value, int sourceSize, int targetSize)
    {
        if (sourceSize <= 1 || targetSize <= 1)
        {
            return 0;
        }

        double ratio = (double)value / (sourceSize - 1);
        return (int)Math.Round((targetSize - 1) * ratio, MidpointRounding.AwayFromZero);
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }
}
