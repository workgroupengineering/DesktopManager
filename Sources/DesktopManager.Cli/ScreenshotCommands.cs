using System;
using System.IO;

namespace DesktopManager.Cli;

internal static class ScreenshotCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "desktop" => Desktop(arguments),
            "window" => Window(arguments),
            "target" => Target(arguments),
            _ => throw new CommandLineException($"Unknown screenshot command '{action}'.")
        };
    }

    private static int Desktop(CommandLineArguments arguments) {
        DesktopScreenshotCommandOptions options = CreateDesktopOptions(arguments);
        ScreenshotResult result = DesktopOperations.CaptureDesktopScreenshot(
            options.MonitorIndex,
            options.DeviceId,
            options.DeviceName,
            options.Left,
            options.Top,
            options.Width,
            options.Height,
            options.OutputPath);
        return WriteScreenshotResult(arguments, result);
    }

    private static int Window(CommandLineArguments arguments) {
        string? targetName = arguments.GetOption("target");
        if (!string.IsNullOrWhiteSpace(targetName)) {
            ScreenshotResult targetResult = DesktopOperations.CaptureWindowTargetScreenshot(
                CreateWindowCriteria(arguments),
                targetName,
                arguments.GetOption("output"));
            return WriteScreenshotResult(arguments, targetResult);
        }

        ScreenshotResult result = DesktopOperations.CaptureWindowScreenshot(
            CreateWindowCriteria(arguments),
            arguments.GetOption("output"));
        return WriteScreenshotResult(arguments, result);
    }

    private static int Target(CommandLineArguments arguments) {
        ScreenshotResult result = DesktopOperations.CaptureWindowTargetScreenshot(
            CreateWindowCriteria(arguments),
            arguments.GetRequiredCommandPart(2, "target name"),
            arguments.GetOption("output"));
        return WriteScreenshotResult(arguments, result);
    }

    private static int WriteScreenshotResult(CommandLineArguments arguments, ScreenshotResult result) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteScreenshotResult(result, Console.Out);
    }

    internal static int WriteScreenshotResult(ScreenshotResult result, TextWriter writer) {
        writer.WriteLine($"screenshot: {result.Kind}");
        writer.WriteLine($"- Path: {result.Path}");
        writer.WriteLine($"- Size: {result.Width}x{result.Height}");
        if (result.Window != null) {
            writer.WriteLine($"- Window: {result.Window.Title}");
        }
        if (result.Geometry != null) {
            writer.WriteLine($"- Client: {result.Geometry.ClientWidth}x{result.Geometry.ClientHeight} at offset {result.Geometry.ClientOffsetLeft},{result.Geometry.ClientOffsetTop}");
        }
        if (result.MonitorIndex.HasValue) {
            writer.WriteLine($"- Monitor: {result.MonitorIndex}");
        }
        return 0;
    }

    internal static WindowSelectionCriteria CreateWindowCriteria(CommandLineArguments arguments) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            ProcessId = arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("handle"),
            Active = arguments.GetBoolFlag("active"),
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    internal static DesktopScreenshotCommandOptions CreateDesktopOptions(CommandLineArguments arguments) {
        return new DesktopScreenshotCommandOptions {
            MonitorIndex = arguments.GetIntOption("monitor"),
            DeviceId = arguments.GetOption("device-id"),
            DeviceName = arguments.GetOption("device-name"),
            Left = arguments.GetIntOption("left"),
            Top = arguments.GetIntOption("top"),
            Width = arguments.GetIntOption("width"),
            Height = arguments.GetIntOption("height"),
            OutputPath = arguments.GetOption("output")
        };
    }
}
