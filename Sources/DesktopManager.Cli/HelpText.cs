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
  target     Save and resolve reusable window-relative targets
  control-target Save and resolve reusable control selector targets
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
  desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
  desktopmanager control-target save edge-address --control-type Edit --background-text --uia
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
  desktopmanager help target
  desktopmanager help control-target
  desktopmanager help layout
  desktopmanager help snapshot
  desktopmanager help mcp
""";
    }

    public static string GetWindowHelp() {
        return """
Window commands:
  desktopmanager window list [--title <pattern>] [--process <pattern>] [--class <pattern>] [--pid <id>] [--handle <value>] [--active] [--include-empty] [--include-hidden] [--exclude-cloaked] [--exclude-owned] [--json]
  desktopmanager window geometry [selector] [--all] [--json]
  desktopmanager window exists [selector] [--json]
  desktopmanager window active-matches [selector] [--json]
  desktopmanager window move [selector] [--monitor <index>] [--x <value>] [--y <value>] [--width <value>] [--height <value>] [--activate] [--all] [--json]
  desktopmanager window click [selector] ((--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) | --target <name>) [--button <left|right>] [--activate] [--client-area] [--all] [--json]
  desktopmanager window drag [selector] (((--start-x <value> --start-y <value>) | (--start-x-ratio <value> --start-y-ratio <value>)) ((--end-x <value> --end-y <value>) | (--end-x-ratio <value> --end-y-ratio <value>)) | (--start-target <name> --end-target <name>)) [--button <left|right>] [--step-delay-ms <value>] [--activate] [--client-area] [--all] [--json]
  desktopmanager window scroll [selector] ((--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) | --target <name>) --delta <value> [--activate] [--client-area] [--all] [--json]
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
  desktopmanager window geometry --handle 0xFF1802 --json
  desktopmanager window exists --process notepad
  desktopmanager window active-matches --title "Codex"
  desktopmanager window click --process notepad --x 200 --y 200 --client-area
  desktopmanager window click --process notepad --x-ratio 0.5 --y-ratio 0.5 --client-area
  desktopmanager window click --process notepad --target editor-center
  desktopmanager window drag --process notepad --start-x 200 --start-y 200 --end-x 500 --end-y 200 --client-area
  desktopmanager window drag --process notepad --start-x-ratio 0.2 --start-y-ratio 0.2 --end-x-ratio 0.6 --end-y-ratio 0.2 --client-area
  desktopmanager window drag --process notepad --start-target editor-center --end-target editor-right
  desktopmanager window scroll --process notepad --x 200 --y 200 --delta -120 --client-area
  desktopmanager window scroll --process notepad --x-ratio 0.5 --y-ratio 0.5 --delta -120 --client-area
  desktopmanager window scroll --process notepad --target editor-center --delta -120
  desktopmanager window type --active --text "Hello world"
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400 --activate
  desktopmanager window snap --process notepad --position left
  desktopmanager window type --process notepad --text "Hello world"
  desktopmanager window wait --process notepad --timeout-ms 5000
""";
    }

    public static string GetTargetHelp() {
        return """
Target commands:
  desktopmanager target save <name> (--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) [--client-area] [--description <text>] [--json]
  desktopmanager target get <name> [--json]
  desktopmanager target list [--json]
  desktopmanager target resolve <name> [selector] [--all] [--json]

Selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty

Examples:
  desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
  desktopmanager target save browser-top-right --x-ratio 0.9 --y-ratio 0.1 --client-area --description "Toolbar area"
  desktopmanager target get editor-center --json
  desktopmanager target list
  desktopmanager target resolve editor-center --process notepad --json
""";
    }

    public static string GetControlTargetHelp() {
        return """
Control-target commands:
  desktopmanager control-target save <name> [control-selector] [--description <text>] [--json]
  desktopmanager control-target get <name> [--json]
  desktopmanager control-target list [--json]
  desktopmanager control-target resolve <name> [window-selector] [--all] [--all-windows] [--json]

Window selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty

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
  --background-click
  --background-text
  --background-keys
  --foreground-fallback
  --uia
  --include-uia
  --ensure-foreground

Examples:
  desktopmanager control-target save edge-address --control-type Edit --background-text --uia --description "Browser address bar"
  desktopmanager control-target save codex-sidebar-toggle --control-type Button --text-pattern "Hide sidebar" --background-click --uia
  desktopmanager control-target get edge-address --json
  desktopmanager control-target list
  desktopmanager control-target resolve edge-address --process msedge --json
""";
    }

    public static string GetControlHelp() {
        return """
Control commands:
  desktopmanager control list [window-selector] ([control-selector] | --target <name>) [--all] [--all-windows] [--json]
  desktopmanager control diagnose [window-selector] ([control-selector] | --target <name>) [--sample-limit <value>] [--action-probe] [--all-windows] [--json]
  desktopmanager control exists [window-selector] ([control-selector] | --target <name>) [--all] [--all-windows] [--json]
  desktopmanager control wait [window-selector] ([control-selector] | --target <name>) [--timeout-ms <value>] [--interval-ms <value>] [--all] [--all-windows] [--json]
  desktopmanager control click [window-selector] ([control-selector] | --target <name>) [--button <left|right>] [--all] [--all-windows] [--json]
  desktopmanager control set-text [window-selector] ([control-selector] | --target <name>) --text <value> [--allow-foreground-input] [--all] [--all-windows] [--json]
  desktopmanager control send-keys [window-selector] ([control-selector] | --target <name>) --keys <VK_A,VK_B> [--keys <VK_C>] [--allow-foreground-input] [--all] [--all-windows] [--json]

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
  --background-click
  --background-text
  --background-keys
  --foreground-fallback
  --uia
  --include-uia
  --ensure-foreground
  --allow-foreground-input
  --action-probe
  --target <name>

Examples:
  desktopmanager control list --window-process notepad --json
  desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --json
  desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --action-probe --json
  desktopmanager control diagnose --window-process msedge --uia --ensure-foreground --sample-limit 5 --json
  desktopmanager control diagnose --window-title "Codex" --target codex-sidebar-toggle --sample-limit 5 --json
  desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground
  desktopmanager control list --window-process msedge --uia --background-click --json
  desktopmanager control list --window-process msedge --uia --foreground-fallback --json
  desktopmanager control list --window-title "Codex" --target codex-sidebar-toggle --json
  desktopmanager control exists --window-title "Codex" --target codex-sidebar-toggle --json
  desktopmanager control wait --window-title "Codex" --target codex-sidebar-toggle --timeout-ms 1000 --interval-ms 100 --json
  desktopmanager control wait --window-active --uia --control-type Button --text-pattern "Show sidebar" --enabled --ensure-foreground --timeout-ms 3000
  desktopmanager control list --window-active --uia --control-type Edit --json
  desktopmanager control click --window-process msedge --target edge-address
  desktopmanager control send-keys --window-title "Codex" --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground --allow-foreground-input --keys VK_SPACE
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
  desktopmanager process start <file> [--arguments <text>] [--working-directory <path>] [--wait-for-input-idle-ms <value>] [--wait-for-window-ms <value>] [--wait-for-window-interval-ms <value>] [--window-title <pattern>] [--window-class <pattern>] [--require-window] [--json]

Examples:
  desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
  desktopmanager process start notepad.exe --wait-for-window-ms 5000
  desktopmanager process start notepad.exe --wait-for-window-ms 5000 --window-title "Untitled - Notepad" --require-window
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
