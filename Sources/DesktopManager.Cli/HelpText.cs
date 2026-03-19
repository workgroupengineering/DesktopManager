namespace DesktopManager.Cli;

internal static class HelpText {
    public static string GetGeneralHelp() {
        return """
desktopmanager - Windows desktop automation CLI

Usage:
  desktopmanager <group> <command> [options]

Groups:
  window     List and control windows
  control    Inspect and interact with child controls
  monitor    Inspect connected monitors
  process    Start desktop applications
  screenshot Capture the desktop, monitors, or windows
  layout     Save, apply, and list named layouts
  snapshot   Save, restore, and list named snapshots
  mcp        Host an MCP server over stdio
  help       Show help for a command group

Examples:
  desktopmanager window list
  desktopmanager window wait --process notepad --timeout-ms 5000
  desktopmanager control list --window-process notepad
  desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
  desktopmanager screenshot desktop
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400
  desktopmanager monitor list --json
  desktopmanager layout save coding
  desktopmanager layout apply coding --validate
  desktopmanager snapshot save workday
  desktopmanager snapshot restore workday

Use:
  desktopmanager help window
  desktopmanager help control
  desktopmanager help monitor
  desktopmanager help process
  desktopmanager help screenshot
  desktopmanager help layout
  desktopmanager help snapshot
  desktopmanager help mcp
""";
    }

    public static string GetWindowHelp() {
        return """
Window commands:
  desktopmanager window list [--title <pattern>] [--process <pattern>] [--class <pattern>] [--pid <id>] [--handle <value>] [--active] [--include-empty] [--include-hidden] [--exclude-cloaked] [--exclude-owned] [--json]
  desktopmanager window exists [selector] [--json]
  desktopmanager window active-matches [selector] [--json]
  desktopmanager window move [selector] [--monitor <index>] [--x <value>] [--y <value>] [--width <value>] [--height <value>] [--activate] [--all] [--json]
  desktopmanager window focus [selector] [--all] [--json]
  desktopmanager window minimize [selector] [--all] [--json]
  desktopmanager window snap [selector] --position <left|right|top-left|top-right|bottom-left|bottom-right> [--all] [--json]
  desktopmanager window type [selector] --text <value> [--paste] [--delay-ms <value>] [--all] [--json]
  desktopmanager window wait [selector] [--timeout-ms <value>] [--interval-ms <value>] [--all] [--json]

Selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty

Examples:
  desktopmanager window list --title "*Notepad*" --json
  desktopmanager window exists --process notepad
  desktopmanager window active-matches --title "Codex"
  desktopmanager window type --active --text "Hello world"
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400 --activate
  desktopmanager window snap --process notepad --position left
  desktopmanager window type --process notepad --text "Hello world"
  desktopmanager window wait --process notepad --timeout-ms 5000
""";
    }

    public static string GetControlHelp() {
        return """
Control commands:
  desktopmanager control list [window-selector] [--class <pattern>] [--text-pattern <pattern>] [--value-pattern <pattern>] [--id <value>] [--handle <value>] [--automation-id <pattern>] [--control-type <pattern>] [--framework-id <pattern>] [--enabled|--disabled] [--focusable|--not-focusable] [--uia] [--include-uia] [--all-windows] [--json]
  desktopmanager control exists [window-selector] [control-selector] [--all-windows] [--json]
  desktopmanager control wait [window-selector] [control-selector] [--timeout-ms <value>] [--interval-ms <value>] [--all-windows] [--json]
  desktopmanager control click [window-selector] [control-selector] [--button <left|right>] [--all] [--all-windows] [--json]
  desktopmanager control set-text [window-selector] [control-selector] --text <value> [--all] [--all-windows] [--json]
  desktopmanager control send-keys [window-selector] [control-selector] --keys <VK_A,VK_B> [--keys <VK_C>] [--all] [--all-windows] [--json]

Window selectors:
  --window-title <pattern>
  --window-process <pattern>
  --window-pid <id>
  --window-class <pattern>
  --window-handle <value>
  --window-active

Control selectors:
  --class <pattern>
  --text-pattern <pattern>
  --value-pattern <pattern>
  --id <value>
  --handle <value>
  --automation-id <pattern>
  --control-type <pattern>
  --framework-id <pattern>
  --enabled
  --disabled
  --focusable
  --not-focusable
  --uia
  --include-uia

Examples:
  desktopmanager control list --window-process notepad --json
  desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable
  desktopmanager control wait --window-active --uia --control-type Button --text-pattern "Show sidebar" --enabled --timeout-ms 3000
  desktopmanager control list --window-active --uia --control-type Edit --json
  desktopmanager control set-text --window-active --class RichEditD2DPT --text "Hello world"
  desktopmanager control click --window-process notepad --class Edit
  desktopmanager control set-text --window-process notepad --class Edit --text "Hello world"
  desktopmanager control send-keys --window-process notepad --class Edit --keys VK_CONTROL,VK_A
""";
    }

    public static string GetMonitorHelp() {
        return """
Monitor commands:
  desktopmanager monitor list [--connected] [--primary] [--index <value>] [--json]

Examples:
  desktopmanager monitor list
  desktopmanager monitor list --json
  desktopmanager monitor list --primary
""";
    }

    public static string GetProcessHelp() {
        return """
Process commands:
  desktopmanager process start <file> [--arguments <text>] [--working-directory <path>] [--wait-for-input-idle-ms <value>] [--json]

Examples:
  desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
  desktopmanager process start code --arguments "." --working-directory C:\Support\GitHub\DesktopManager
""";
    }

    public static string GetScreenshotHelp() {
        return """
Screenshot commands:
  desktopmanager screenshot desktop [--monitor <index>] [--device-id <value>] [--device-name <value>] [--left <value> --top <value> --width <value> --height <value>] [--output <path>] [--json]
  desktopmanager screenshot window [selector] [--active] [--output <path>] [--json]

Examples:
  desktopmanager screenshot desktop
  desktopmanager screenshot desktop --monitor 0 --output .\monitor0.png
  desktopmanager screenshot window --active --output .\active-window.png
  desktopmanager screenshot window --process notepad --output .\notepad.png
""";
    }

    public static string GetLayoutHelp() {
        return """
Layout commands:
  desktopmanager layout save <name> [--json]
  desktopmanager layout apply <name> [--validate] [--json]
  desktopmanager layout list [--json]

Named layouts are stored under the current user's AppData profile.
""";
    }

    public static string GetSnapshotHelp() {
        return """
Snapshot commands:
  desktopmanager snapshot save <name> [--json]
  desktopmanager snapshot restore <name> [--validate] [--json]
  desktopmanager snapshot list [--json]

Snapshots currently store window layout state only. This command group is designed
to grow into broader desktop state capture later.
""";
    }

    public static string GetMcpHelp() {
        return """
MCP commands:
  desktopmanager mcp serve

This command group hosts a stdio MCP server that exposes tools, resources, and prompts.
""";
    }
}
