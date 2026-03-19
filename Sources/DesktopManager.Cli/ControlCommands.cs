using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager.Cli;

internal static class ControlCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "exists" => Exists(arguments),
            "wait" => Wait(arguments),
            "click" => Click(arguments),
            "set-text" => SetText(arguments),
            "send-keys" => SendKeys(arguments),
            _ => throw new CommandLineException($"Unknown control command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<ControlResult> controls = DesktopOperations.ListControls(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetBoolFlag("all-windows"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(controls);
            return 0;
        }

        if (controls.Count == 0) {
            Console.WriteLine("No matching controls found.");
            return 0;
        }

        var rows = controls
            .Select(control => (IReadOnlyList<string>)new[] {
                control.ParentWindow.ProcessId.ToString(),
                control.Id.ToString(),
                control.Handle.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase),
                control.Source,
                control.ControlType,
                control.IsEnabled?.ToString() ?? string.Empty,
                control.IsKeyboardFocusable?.ToString() ?? string.Empty,
                control.AutomationId,
                control.ClassName,
                control.Text,
                control.Value,
                control.ParentWindow.Title
            })
            .ToArray();
        OutputFormatter.WriteTable(new[] { "PID", "Id", "Handle", "Source", "Type", "Enabled", "Focusable", "AutomationId", "Class", "Text", "Value", "Window" }, rows);
        return 0;
    }

    private static int Exists(CommandLineArguments arguments) {
        ControlAssertionResult result = DesktopOperations.ControlExists(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetBoolFlag("all-windows"));
        return WriteAssertion(arguments, result, "Matching control found.", "No matching controls found.");
    }

    private static int Wait(CommandLineArguments arguments) {
        WaitForControlResult result = DesktopOperations.WaitForControl(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetIntOption("timeout-ms") ?? 10000,
            arguments.GetIntOption("interval-ms") ?? 200,
            arguments.GetBoolFlag("all-windows"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"wait: {result.Count} control(s) after {result.ElapsedMilliseconds}ms");
        foreach (ControlResult control in result.Controls) {
            Console.WriteLine($"- {control.ControlType} {control.Text} in {control.ParentWindow.Title}");
        }
        return 0;
    }

    private static int Click(CommandLineArguments arguments) {
        ControlActionResult result = DesktopOperations.ClickControl(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetOption("button") ?? "left",
            arguments.GetBoolFlag("all-windows"));
        return WriteAction(arguments, result);
    }

    private static int SetText(CommandLineArguments arguments) {
        ControlActionResult result = DesktopOperations.SetControlText(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            arguments.GetRequiredOption("text"),
            arguments.GetBoolFlag("all-windows"));
        return WriteAction(arguments, result);
    }

    private static int SendKeys(CommandLineArguments arguments) {
        IReadOnlyList<string> keys = arguments.GetOptions("keys");
        if (keys.Count == 0) {
            string single = arguments.GetRequiredOption("keys");
            keys = new[] { single };
        }

        ControlActionResult result = DesktopOperations.SendControlKeys(
            CreateWindowCriteria(arguments),
            CreateControlCriteria(arguments),
            keys,
            arguments.GetBoolFlag("all-windows"));
        return WriteAction(arguments, result);
    }

    private static int WriteAction(CommandLineArguments arguments, ControlActionResult result) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        Console.WriteLine($"{result.Action}: {result.Count} control(s)");
        foreach (ControlResult control in result.Controls) {
            Console.WriteLine($"- {control.ClassName} [{control.Id}] in {control.ParentWindow.Title}");
        }
        return 0;
    }

    private static int WriteAssertion(CommandLineArguments arguments, ControlAssertionResult result, string successText, string failureText) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
        } else {
            Console.WriteLine(result.Matched ? successText : failureText);
            foreach (ControlResult control in result.Controls) {
                Console.WriteLine($"- {control.ControlType} {control.Text} in {control.ParentWindow.Title}");
            }
        }

        return result.Matched ? 0 : 2;
    }

    private static WindowSelectionCriteria CreateWindowCriteria(CommandLineArguments arguments) {
        return new WindowSelectionCriteria {
            TitlePattern = arguments.GetOption("window-title") ?? arguments.GetOption("title") ?? "*",
            ProcessNamePattern = arguments.GetOption("window-process") ?? arguments.GetOption("process") ?? "*",
            ClassNamePattern = arguments.GetOption("window-class") ?? "*",
            ProcessId = arguments.GetIntOption("window-pid") ?? arguments.GetIntOption("pid"),
            Handle = arguments.GetOption("window-handle"),
            Active = arguments.GetBoolFlag("window-active") || arguments.GetBoolFlag("active"),
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    private static ControlSelectionCriteria CreateControlCriteria(CommandLineArguments arguments) {
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
            UiAutomation = arguments.GetBoolFlag("uia"),
            IncludeUiAutomation = arguments.GetBoolFlag("include-uia"),
            All = arguments.GetBoolFlag("all")
        };
    }
}
