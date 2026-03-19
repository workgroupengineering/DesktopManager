using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class WindowCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "move" => Move(arguments),
            "focus" => Focus(arguments),
            "minimize" => Minimize(arguments),
            "snap" => Snap(arguments),
            _ => throw new CommandLineException($"Unknown window command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<WindowResult> windows = DesktopOperations.ListWindows(CreateCriteria(arguments, includeEmptyDefault: false));
        if (windows.Count == 0) {
            if (arguments.GetBoolFlag("json")) {
                OutputFormatter.WriteJson(Array.Empty<object>());
            } else {
                Console.WriteLine("No matching windows found.");
            }
            return 0;
        }

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(windows);
            return 0;
        }

        var rows = windows
            .Select(window => (IReadOnlyList<string>)new[] {
                window.ProcessId.ToString(),
                window.Handle.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase),
                window.MonitorIndex.ToString(),
                window.IsVisible ? "Yes" : "No",
                window.State ?? string.Empty,
                window.Title
            })
            .ToArray();
        OutputFormatter.WriteTable(new[] { "PID", "Handle", "Mon", "Visible", "State", "Title" }, rows);
        return 0;
    }

    private static int Move(CommandLineArguments arguments) {
        WindowChangeResult result = DesktopOperations.MoveWindow(
            CreateCriteria(arguments, includeEmptyDefault: true),
            arguments.GetIntOption("monitor"),
            arguments.GetIntOption("x"),
            arguments.GetIntOption("y"),
            arguments.GetIntOption("width"),
            arguments.GetIntOption("height"),
            arguments.GetBoolFlag("activate"));
        return WriteWindowMutationResult(arguments, result);
    }

    private static int Focus(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.FocusWindow(CreateCriteria(arguments, includeEmptyDefault: true)));
    }

    private static int Minimize(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.MinimizeWindows(CreateCriteria(arguments, includeEmptyDefault: true)));
    }

    private static int Snap(CommandLineArguments arguments) {
        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SnapWindow(CreateCriteria(arguments, includeEmptyDefault: true), arguments.GetRequiredOption("position")));
    }

    private static int WriteWindowMutationResult(CommandLineArguments arguments, WindowChangeResult payload) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
            return 0;
        }

        Console.WriteLine($"{payload.Action}: {payload.Count} window(s)");
        foreach (WindowResult window in payload.Windows) {
            Console.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
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
            IncludeHidden = arguments.GetBoolFlag("include-hidden"),
            IncludeCloaked = !arguments.GetBoolFlag("exclude-cloaked"),
            IncludeOwned = !arguments.GetBoolFlag("exclude-owned"),
            IncludeEmptyTitles = arguments.GetBoolFlag("include-empty") || includeEmptyDefault,
            All = arguments.GetBoolFlag("all")
        };
    }
}
