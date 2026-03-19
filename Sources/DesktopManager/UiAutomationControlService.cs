using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DesktopManager;

internal sealed class UiAutomationControlService {
    private readonly Assembly? _automationClientAssembly;
    private readonly Assembly? _automationTypesAssembly;
    private readonly Type? _automationElementType;
    private readonly Type? _automationElementCollectionType;
    private readonly Type? _conditionType;
    private readonly Type? _treeScopeType;

    public UiAutomationControlService() {
        _automationClientAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "UIAutomationClient", StringComparison.OrdinalIgnoreCase));
        _automationClientAssembly ??= TryLoadAssembly("UIAutomationClient");

        _automationTypesAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "UIAutomationTypes", StringComparison.OrdinalIgnoreCase));
        _automationTypesAssembly ??= TryLoadAssembly("UIAutomationTypes");

        _automationElementType = _automationClientAssembly?.GetType("System.Windows.Automation.AutomationElement", throwOnError: false);
        _automationElementCollectionType = _automationClientAssembly?.GetType("System.Windows.Automation.AutomationElementCollection", throwOnError: false);
        _conditionType = _automationClientAssembly?.GetType("System.Windows.Automation.Condition", throwOnError: false);
        _treeScopeType = _automationTypesAssembly?.GetType("System.Windows.Automation.TreeScope", throwOnError: false)
            ?? _automationClientAssembly?.GetType("System.Windows.Automation.TreeScope", throwOnError: false);
    }

    public bool IsAvailable => _automationElementType != null &&
        _automationElementCollectionType != null &&
        _conditionType != null &&
        _treeScopeType != null;

    public List<WindowControlInfo> EnumerateControls(IntPtr windowHandle) {
        if (!IsAvailable || windowHandle == IntPtr.Zero) {
            return new List<WindowControlInfo>();
        }

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            return EnumerateControlsCore(windowHandle);
        }

        List<WindowControlInfo> controls = new List<WindowControlInfo>();
        var thread = new Thread(() => {
            controls = EnumerateControlsCore(windowHandle);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return controls;
    }

    private List<WindowControlInfo> EnumerateControlsCore(IntPtr windowHandle) {
        try {
            object? rootElement = _automationElementType!.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static)?
                .Invoke(null, new object[] { windowHandle });
            if (rootElement == null) {
                return new List<WindowControlInfo>();
            }

            object? treeScope = Enum.Parse(_treeScopeType!, "Descendants", ignoreCase: false);
            object? trueCondition = _conditionType!.GetField("TrueCondition", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (treeScope == null || trueCondition == null) {
                return new List<WindowControlInfo>();
            }

            object? collection = _automationElementType.GetMethod("FindAll", new[] { _treeScopeType!, _conditionType! })?
                .Invoke(rootElement, new[] { treeScope, trueCondition });
            if (collection == null) {
                return new List<WindowControlInfo>();
            }

            PropertyInfo? countProperty = _automationElementCollectionType!.GetProperty("Count");
            PropertyInfo? itemProperty = _automationElementCollectionType.GetProperty("Item");
            if (countProperty == null || itemProperty == null) {
                return new List<WindowControlInfo>();
            }

            int count = (int)(countProperty.GetValue(collection) ?? 0);
            var controls = new List<WindowControlInfo>(count);
            for (int index = 0; index < count; index++) {
                object? element = itemProperty.GetValue(collection, new object[] { index });
                if (element == null) {
                    continue;
                }

                WindowControlInfo? info = CreateControlInfo(element);
                if (info != null) {
                    controls.Add(info);
                }
            }

            return controls;
        } catch {
            return new List<WindowControlInfo>();
        }
    }

    private static Assembly? TryLoadAssembly(string name) {
        try {
            return Assembly.Load(name);
        } catch {
            return null;
        }
    }

    private WindowControlInfo? CreateControlInfo(object element) {
        object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
        if (current == null) {
            return null;
        }

        string name = ReadString(current, "Name");
        string className = ReadString(current, "ClassName");
        string automationId = ReadString(current, "AutomationId");
        string frameworkId = ReadString(current, "FrameworkId");
        int nativeWindowHandle = ReadInt32(current, "NativeWindowHandle");
        bool? isKeyboardFocusable = ReadNullableBoolean(current, "IsKeyboardFocusable");
        bool? isEnabled = ReadNullableBoolean(current, "IsEnabled");
        string controlType = ReadControlTypeName(current);

        return new WindowControlInfo {
            Handle = nativeWindowHandle == 0 ? IntPtr.Zero : new IntPtr(nativeWindowHandle),
            ClassName = className,
            Id = 0,
            Text = name,
            Source = WindowControlSource.UiAutomation,
            AutomationId = automationId,
            ControlType = controlType,
            FrameworkId = frameworkId,
            IsKeyboardFocusable = isKeyboardFocusable,
            IsEnabled = isEnabled
        };
    }

    private static string ReadString(object instance, string propertyName) {
        return instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance) as string ?? string.Empty;
    }

    private static int ReadInt32(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        return value is int intValue ? intValue : 0;
    }

    private static bool? ReadNullableBoolean(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        return value is bool boolValue ? boolValue : null;
    }

    private static string ReadControlTypeName(object instance) {
        object? controlType = instance.GetType().GetProperty("ControlType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        if (controlType == null) {
            return string.Empty;
        }

        string? programmaticName = controlType.GetType().GetProperty("ProgrammaticName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(controlType) as string;
        if (string.IsNullOrWhiteSpace(programmaticName)) {
            return controlType.ToString() ?? string.Empty;
        }

        string normalized = programmaticName ?? string.Empty;
        const string prefix = "ControlType.";
        return normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(prefix.Length)
            : normalized;
    }
}
