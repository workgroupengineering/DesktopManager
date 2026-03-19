using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class WindowCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "exists" => Exists(arguments),
            "active-matches" => ActiveMatches(arguments),
            "move" => Move(arguments),
            "click" => Click(arguments),
            "focus" => Focus(arguments),
            "minimize" => Minimize(arguments),
            "snap" => Snap(arguments),
            "type" => Type(arguments),
            "wait" => Wait(arguments),
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

    private static int Exists(CommandLineArguments arguments) {
        WindowAssertionResult result = DesktopOperations.WindowExists(CreateCriteria(arguments, includeEmptyDefault: true));
        return WriteAssertionResult(arguments, result, "Matching window found.", "No matching windows found.");
    }

    private static int ActiveMatches(CommandLineArguments arguments) {
        WindowAssertionResult result = DesktopOperations.ActiveWindowMatches(CreateCriteria(arguments, includeEmptyDefault: true));
        return WriteAssertionResult(arguments, result, "Active window matches.", "Active window does not match.");
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

    private static int Click(CommandLineArguments arguments) {
        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.ClickWindowPoint(
                CreateCriteria(arguments, includeEmptyDefault: true),
                arguments.GetRequiredIntOption("x"),
                arguments.GetRequiredIntOption("y"),
                arguments.GetOption("button") ?? "left",
                arguments.GetBoolFlag("activate")));
    }

    private static int Minimize(CommandLineArguments arguments) {
        return WriteWindowMutationResult(arguments, DesktopOperations.MinimizeWindows(CreateCriteria(arguments, includeEmptyDefault: true)));
    }

    private static int Snap(CommandLineArguments arguments) {
        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.SnapWindow(CreateCriteria(arguments, includeEmptyDefault: true), arguments.GetRequiredOption("position")));
    }

    private static int Type(CommandLineArguments arguments) {
        return WriteWindowMutationResult(
            arguments,
            DesktopOperations.TypeWindowText(
                CreateCriteria(arguments, includeEmptyDefault: true),
                arguments.GetRequiredOption("text"),
                arguments.GetBoolFlag("paste"),
                arguments.GetIntOption("delay-ms") ?? 0));
    }

    private static int Wait(CommandLineArguments arguments) {
        WaitForWindowResult result = DesktopOperations.WaitForWindow(
            CreateCriteria(arguments, includeEmptyDefault: true),
            arguments.GetIntOption("timeout-ms") ?? 10000,
            arguments.GetIntOption("interval-ms") ?? 200);

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"wait: {result.Count} window(s) after {result.ElapsedMilliseconds}ms");
        foreach (WindowResult window in result.Windows) {
            Console.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
        }
        return 0;
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

    private static int WriteAssertionResult(CommandLineArguments arguments, WindowAssertionResult payload, string successText, string failureText) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
        } else {
            Console.WriteLine(payload.Matched ? successText : failureText);
            if (payload.ActiveWindow != null) {
                Console.WriteLine($"Active: {payload.ActiveWindow.Title} [PID {payload.ActiveWindow.ProcessId}]");
            }

            foreach (WindowResult window in payload.Windows) {
                Console.WriteLine($"- {window.Title} [PID {window.ProcessId}]");
            }
        }

        return payload.Matched ? 0 : 2;
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
