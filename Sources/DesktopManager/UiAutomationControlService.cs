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
        Exception? workerException = null;
        var thread = new Thread(() => {
            try {
                controls = new UiAutomationControlService().EnumerateControlsCore(windowHandle);
            } catch (Exception ex) {
                workerException = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (workerException != null) {
            throw workerException;
        }
        return controls;
    }

    public bool TryInvoke(WindowInfo window, WindowControlInfo control) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TryInvokeCore(window, control));
    }

    public bool TrySetValue(WindowInfo window, WindowControlInfo control, string value) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        return RunInSta(service => service.TrySetValueCore(window, control, value));
    }

    private T RunInSta<T>(Func<UiAutomationControlService, T> operation) {
        if (!IsAvailable) {
            return default!;
        }

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            return operation(this);
        }

        T result = default!;
        Exception? workerException = null;
        var thread = new Thread(() => {
            try {
                result = operation(new UiAutomationControlService());
            } catch (Exception ex) {
                workerException = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (workerException != null) {
            throw workerException;
        }

        return result;
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

    private bool TryInvokeCore(WindowInfo window, WindowControlInfo control) {
        object? element = FindMatchingElement(window.Handle, control);
        if (element == null) {
            return false;
        }

        return TryPatternAction(element, "System.Windows.Automation.InvokePattern", "Invoke") ||
            TryPatternAction(element, "System.Windows.Automation.SelectionItemPattern", "Select") ||
            TryPatternAction(element, "System.Windows.Automation.ExpandCollapsePattern", "Expand") ||
            TryPatternAction(element, "System.Windows.Automation.TogglePattern", "Toggle");
    }

    private bool TrySetValueCore(WindowInfo window, WindowControlInfo control, string value) {
        object? element = FindMatchingElement(window.Handle, control);
        if (element == null) {
            return false;
        }

        return TryPatternAction(element, "System.Windows.Automation.ValuePattern", "SetValue", value) ||
            TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "SetValue", value);
    }

    private object? FindMatchingElement(IntPtr windowHandle, WindowControlInfo control) {
        object? rootElement = _automationElementType!.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static)?
            .Invoke(null, new object[] { windowHandle });
        if (rootElement == null) {
            return null;
        }

        object? treeScope = Enum.Parse(_treeScopeType!, "Descendants", ignoreCase: false);
        object? trueCondition = _conditionType!.GetField("TrueCondition", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (treeScope == null || trueCondition == null) {
            return null;
        }

        object? collection = _automationElementType.GetMethod("FindAll", new[] { _treeScopeType!, _conditionType! })?
            .Invoke(rootElement, new[] { treeScope, trueCondition });
        if (collection == null) {
            return null;
        }

        PropertyInfo? countProperty = _automationElementCollectionType!.GetProperty("Count");
        PropertyInfo? itemProperty = _automationElementCollectionType.GetProperty("Item");
        if (countProperty == null || itemProperty == null) {
            return null;
        }

        int count = (int)(countProperty.GetValue(collection) ?? 0);
        object? bestMatch = null;
        int bestScore = 0;
        for (int index = 0; index < count; index++) {
            object? candidate = itemProperty.GetValue(collection, new object[] { index });
            if (candidate == null) {
                continue;
            }

            WindowControlInfo? candidateInfo = CreateControlInfo(candidate);
            if (candidateInfo == null) {
                continue;
            }

            int score = ScoreMatch(control, candidateInfo);
            if (score > bestScore) {
                bestScore = score;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    private static int ScoreMatch(WindowControlInfo expected, WindowControlInfo candidate) {
        int score = 0;
        if (expected.Handle != IntPtr.Zero && candidate.Handle == expected.Handle) {
            score += 100;
        }

        if (!string.IsNullOrWhiteSpace(expected.AutomationId) &&
            string.Equals(expected.AutomationId, candidate.AutomationId, StringComparison.OrdinalIgnoreCase)) {
            score += 40;
        }

        if (!string.IsNullOrWhiteSpace(expected.ControlType) &&
            string.Equals(expected.ControlType, candidate.ControlType, StringComparison.OrdinalIgnoreCase)) {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(expected.ClassName) &&
            string.Equals(expected.ClassName, candidate.ClassName, StringComparison.OrdinalIgnoreCase)) {
            score += 10;
        }

        if (!string.IsNullOrWhiteSpace(expected.Text) &&
            string.Equals(expected.Text, candidate.Text, StringComparison.OrdinalIgnoreCase)) {
            score += 10;
        }

        return score;
    }

    private bool TryPatternAction(object element, string patternTypeName, string methodName, params object[] parameters) {
        Type? patternType = _automationClientAssembly?.GetType(patternTypeName, throwOnError: false);
        if (patternType == null) {
            return false;
        }

        object? pattern = GetCurrentPattern(element, patternType);
        if (pattern == null) {
            return false;
        }

        MethodInfo? method = pattern.GetType().GetMethod(methodName, parameters.Select(parameter => parameter.GetType()).ToArray());
        if (method == null) {
            return false;
        }

        method.Invoke(pattern, parameters);
        return true;
    }

    private static object? GetCurrentPattern(object element, Type patternType) {
        object? patternIdentifier = patternType.GetField("Pattern", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (patternIdentifier == null) {
            return null;
        }

        return element.GetType().GetMethod("GetCurrentPattern", new[] { patternIdentifier.GetType() })?
            .Invoke(element, new[] { patternIdentifier });
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
