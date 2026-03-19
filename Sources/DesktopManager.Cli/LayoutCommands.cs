using System;
using System.Linq;

namespace DesktopManager.Cli;

internal static class LayoutCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "save" => Save(arguments),
            "apply" => Apply(arguments),
            "list" => List(arguments),
            _ => throw new CommandLineException($"Unknown layout command '{action}'.")
        };
    }

    private static int Save(CommandLineArguments arguments) {
        return WritePathResult(arguments, DesktopOperations.SaveLayout(arguments.GetRequiredCommandPart(2, "layout name")));
    }

    private static int Apply(CommandLineArguments arguments) {
        return WritePathResult(
            arguments,
            DesktopOperations.ApplyLayout(arguments.GetRequiredCommandPart(2, "layout name"), arguments.GetBoolFlag("validate")));
    }

    private static int List(CommandLineArguments arguments) {
        var names = DesktopOperations.ListLayouts();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(names);
            return 0;
        }

        if (names.Count == 0) {
            Console.WriteLine("No named layouts found.");
            return 0;
        }

        foreach (string name in names.OrderBy(value => value)) {
            Console.WriteLine(name);
        }
        return 0;
    }

    private static int WritePathResult(CommandLineArguments arguments, NamedStateResult payload) {
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(payload);
            return 0;
        }

        Console.WriteLine($"{payload.Action}: {payload.Name}");
        Console.WriteLine(payload.Path);
        return 0;
    }
}
