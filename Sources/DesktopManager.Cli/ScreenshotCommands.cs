using System;

namespace DesktopManager.Cli;

internal static class ScreenshotCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "desktop" => Desktop(arguments),
            "window" => Window(arguments),
            _ => throw new CommandLineException($"Unknown screenshot command '{action}'.")
        };
    }

    private static int Desktop(CommandLineArguments arguments) {
        ScreenshotResult result = DesktopOperations.CaptureDesktopScreenshot(
            arguments.GetIntOption("monitor"),
            arguments.GetOption("device-id"),
            arguments.GetOption("device-name"),
            arguments.GetIntOption("left"),
            arguments.GetIntOption("top"),
            arguments.GetIntOption("width"),
            arguments.GetIntOption("height"),
            arguments.GetOption("output"));
        return WriteScreenshotResult(arguments, result);
    }

    private static int Window(CommandLineArguments arguments) {
        ScreenshotResult result = DesktopOperations.CaptureWindowScreenshot(
            new WindowSelectionCriteria {
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
            },
            arguments.GetOption("output"));
        return WriteScreenshotResult(arguments, result);
    }

    private static int WriteScreenshotResult(CommandLineArguments arguments, ScreenshotResult result) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"screenshot: {result.Kind}");
        Console.WriteLine($"- Path: {result.Path}");
        Console.WriteLine($"- Size: {result.Width}x{result.Height}");
        if (result.Window != null) {
            Console.WriteLine($"- Window: {result.Window.Title}");
        }
        if (result.MonitorIndex.HasValue) {
            Console.WriteLine($"- Monitor: {result.MonitorIndex}");
        }
        return 0;
    }
}
