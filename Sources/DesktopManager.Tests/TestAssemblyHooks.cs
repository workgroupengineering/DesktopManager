using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
public sealed class TestAssemblyHooks {
    [AssemblyInitialize]
    public static void Initialize(TestContext context) {
        TestHelper.KillAllNotepads();
    }

    [AssemblyCleanup]
    public static void Cleanup() {
        TestHelper.KillAllNotepads();
    }
}
