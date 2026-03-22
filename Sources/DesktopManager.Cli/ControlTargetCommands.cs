using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class ControlTargetCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "get" => Get(arguments),
            "list" => List(arguments),
            "resolve" => Resolve(arguments),
            _ => throw new CommandLineException($"Unknown control-target command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        ControlTargetResult result = DesktopOperations.SaveControlTarget(
            arguments.GetRequiredCommandPart(2, "control target name"),
            CreateControlCriteria(arguments),
            arguments.GetOption("description"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteSavedTargetResult(result, Console.Out);
    }

    private static int Get(CommandLineArguments arguments) {
        ControlTargetResult result = DesktopOperations.GetControlTarget(arguments.GetRequiredCommandPart(2, "control target name"));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteTargetResult(result, Console.Out);
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<string> names = DesktopOperations.ListControlTargets();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        return WriteTargetNames(names, Console.Out);
    }

    private static int Resolve(CommandLineArguments arguments) {
        IReadOnlyList<ResolvedControlTargetResult> results = DesktopOperations.ResolveControlTargets(
            CreateWindowCriteria(arguments, includeEmptyDefault: true),
            arguments.GetRequiredCommandPart(2, "control target name"),
            arguments.GetBoolFlag("all"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteResolvedTargets(results, Console.Out);
    }

    internal static int WriteSavedTargetResult(ControlTargetResult result, TextWriter writer) {
        writer.WriteLine($"save: {result.Name}");
        writer.WriteLine(result.Path);
        return 0;
    }

    internal static int WriteTargetResult(ControlTargetResult result, TextWriter writer) {
        writer.WriteLine(result.Name);
        writer.WriteLine($"- Path: {result.Path}");
        writer.WriteLine($"- Class: {result.Target.ClassNamePattern}");
        writer.WriteLine($"- Text: {result.Target.TextPattern}");
        writer.WriteLine($"- Value: {result.Target.ValuePattern}");
        writer.WriteLine($"- ControlType: {result.Target.ControlTypePattern}");
        writer.WriteLine($"- AutomationId: {result.Target.AutomationIdPattern}");
        writer.WriteLine($"- BackgroundClick: {result.Target.SupportsBackgroundClick?.ToString() ?? "-"}");
        writer.WriteLine($"- BackgroundText: {result.Target.SupportsBackgroundText?.ToString() ?? "-"}");
        writer.WriteLine($"- BackgroundKeys: {result.Target.SupportsBackgroundKeys?.ToString() ?? "-"}");
        writer.WriteLine($"- ForegroundFallback: {result.Target.SupportsForegroundInputFallback?.ToString() ?? "-"}");
        if (!string.IsNullOrWhiteSpace(result.Target.Description)) {
            writer.WriteLine($"- Description: {result.Target.Description}");
        }
        return 0;
    }

    internal static int WriteTargetNames(IReadOnlyList<string> names, TextWriter writer) {
        if (names.Count == 0) {
            writer.WriteLine("No named control targets found.");
            return 0;
        }

        foreach (string name in names.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) {
            writer.WriteLine(name);
        }
        return 0;
    }

    internal static int WriteResolvedTargets(IReadOnlyList<ResolvedControlTargetResult> results, TextWriter writer) {
        foreach (ResolvedControlTargetResult result in results) {
            writer.WriteLine($"{result.Name}: {result.Window.Title} ({result.Window.Handle})");
            writer.WriteLine($"- Control: {result.Control.ControlType} {result.Control.Text}");
            writer.WriteLine($"- Handle: {result.Control.Handle}");
            writer.WriteLine($"- BackgroundText: {result.Control.SupportsBackgroundText}");
            writer.WriteLine($"- ForegroundFallback: {result.Control.SupportsForegroundInputFallback}");
        }

        return 0;
    }

    internal static WindowSelectionCriteria CreateWindowCriteria(CommandLineArguments arguments, bool includeEmptyDefault) {
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
            All = arguments.GetBoolFlag("all-windows")
        };
    }

    internal static ControlSelectionCriteria CreateControlCriteria(CommandLineArguments arguments) {
        return new ControlSelectionCriteria {
            ClassNamePattern = arguments.GetOption("class") ?? "*",
            TextPattern = arguments.GetOption("text-pattern") ?? "*",
            ValuePattern = arguments.GetOption("value-pattern") ?? "*",
            Id = arguments.GetIntOption("id"),
            Handle = arguments.GetOption("handle"),
            AutomationIdPattern = arguments.GetOption("automation-id") ?? "*",
            ControlTypePattern = arguments.GetOption("control-type") ?? "*",
            FrameworkIdPattern = arguments.GetOption("framework-id") ?? "*",
            IsEnabled = arguments.GetBoolFlag("enabled") ? true : arguments.GetBoolFlag("disabled") ? false : null,
            IsKeyboardFocusable = arguments.GetBoolFlag("focusable") ? true : arguments.GetBoolFlag("not-focusable") ? false : null,
            SupportsBackgroundClick = arguments.GetBoolFlag("background-click") ? true : null,
            SupportsBackgroundText = arguments.GetBoolFlag("background-text") ? true : null,
            SupportsBackgroundKeys = arguments.GetBoolFlag("background-keys") ? true : null,
            SupportsForegroundInputFallback = arguments.GetBoolFlag("foreground-fallback") ? true : null,
            EnsureForegroundWindow = arguments.GetBoolFlag("ensure-foreground"),
            UiAutomation = arguments.GetBoolFlag("uia"),
            IncludeUiAutomation = arguments.GetBoolFlag("include-uia"),
            All = arguments.GetBoolFlag("all")
        };
    }
}
