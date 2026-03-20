using System;
using System.Linq;

namespace DesktopManager.Cli;

internal static class TargetCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "get" => Get(arguments),
            "list" => List(arguments),
            "resolve" => Resolve(arguments),
            _ => throw new CommandLineException($"Unknown target command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        WindowTargetResult result = DesktopOperations.SaveWindowTarget(
            arguments.GetRequiredCommandPart(2, "target name"),
            arguments.GetOption("description"),
            arguments.GetIntOption("x"),
            arguments.GetIntOption("y"),
            arguments.GetDoubleOption("x-ratio"),
            arguments.GetDoubleOption("y-ratio"),
            arguments.GetIntOption("width"),
            arguments.GetIntOption("height"),
            arguments.GetDoubleOption("width-ratio"),
            arguments.GetDoubleOption("height-ratio"),
            arguments.GetBoolFlag("client-area"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"save: {result.Name}");
        Console.WriteLine(result.Path);
        return 0;
    }

    private static int Get(CommandLineArguments arguments) {
        WindowTargetResult result = DesktopOperations.GetWindowTarget(arguments.GetRequiredCommandPart(2, "target name"));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine(result.Name);
        Console.WriteLine($"- Path: {result.Path}");
        Console.WriteLine($"- ClientArea: {(result.Target.ClientArea ? "Yes" : "No")}");
        Console.WriteLine($"- X: {result.Target.X?.ToString() ?? "-"}");
        Console.WriteLine($"- Y: {result.Target.Y?.ToString() ?? "-"}");
        Console.WriteLine($"- XRatio: {result.Target.XRatio?.ToString() ?? "-"}");
        Console.WriteLine($"- YRatio: {result.Target.YRatio?.ToString() ?? "-"}");
        Console.WriteLine($"- Width: {result.Target.Width?.ToString() ?? "-"}");
        Console.WriteLine($"- Height: {result.Target.Height?.ToString() ?? "-"}");
        Console.WriteLine($"- WidthRatio: {result.Target.WidthRatio?.ToString() ?? "-"}");
        Console.WriteLine($"- HeightRatio: {result.Target.HeightRatio?.ToString() ?? "-"}");
        if (!string.IsNullOrWhiteSpace(result.Target.Description)) {
            Console.WriteLine($"- Description: {result.Target.Description}");
        }
        return 0;
    }

    private static int List(CommandLineArguments arguments) {
        var names = DesktopOperations.ListWindowTargets();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        if (names.Count == 0) {
            Console.WriteLine("No named targets found.");
            return 0;
        }

        foreach (string name in names.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) {
            Console.WriteLine(name);
        }
        return 0;
    }

    private static int Resolve(CommandLineArguments arguments) {
        IReadOnlyList<ResolvedWindowTargetResult> results = DesktopOperations.ResolveWindowTargets(
            CreateCriteria(arguments, includeEmptyDefault: true),
            arguments.GetRequiredCommandPart(2, "target name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        foreach (ResolvedWindowTargetResult result in results) {
            Console.WriteLine($"{result.Name}: {result.Window.Title} ({result.Window.Handle})");
            Console.WriteLine($"- Relative: {result.RelativeX},{result.RelativeY}");
            Console.WriteLine($"- Screen: {result.ScreenX},{result.ScreenY}");
            if (result.ScreenWidth.HasValue && result.ScreenHeight.HasValue) {
                Console.WriteLine($"- Area: {result.ScreenWidth}x{result.ScreenHeight}");
            }
            Console.WriteLine($"- ClientArea: {(result.Target.ClientArea ? "Yes" : "No")}");
        }
        return 0;
    }

    private static WindowSelectionCriteria CreateCriteria(CommandLineArguments arguments, bool includeEmptyDefault) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            ProcessId = arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("handle"),
            Active = arguments.GetBoolFlag("active"),
            IncludeHidden = arguments.GetBoolFlag("include-hidden"),
            IncludeCloaked = !arguments.GetBoolFlag("exclude-cloaked"),
            IncludeOwned = !arguments.GetBoolFlag("exclude-owned"),
            IncludeEmptyTitles = arguments.GetBoolFlag("include-empty") || includeEmptyDefault,
            All = arguments.GetBoolFlag("all")
        };
    }
}
