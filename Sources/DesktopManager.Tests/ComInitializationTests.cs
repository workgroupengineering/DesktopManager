using System;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for COM initialization during logon wallpaper operations.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
public class ComInitializationTests {
    private class ComTrackingService : MonitorService {
        public bool InitCalled;
        public bool UninitCalled;
        public ComTrackingService() : base(new FakeDesktopManager()) { }
        protected override bool InitializeCom() { InitCalled = true; return true; }
        protected override void UninitializeCom() { UninitCalled = true; }
    }

    [TestMethod]
    /// <summary>
    /// Ensure SetLogonWallpaper initializes and uninitializes COM.
    /// </summary>
    public void SetLogonWallpaper_InitializesCom() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var service = new ComTrackingService();
        try { 
            service.SetLogonWallpaper("path"); 
        } catch (UnauthorizedAccessException) {
            // Expected when not elevated - COM should still be initialized before privilege check
        } catch (InvalidOperationException) {
            // Expected when Windows runtime isn't available - COM should still be initialized
        } catch { 
            // Other exceptions are fine for this test
        }
        Assert.IsTrue(service.InitCalled, "InitializeCom should have been called");
        Assert.IsTrue(service.UninitCalled, "UninitializeCom should have been called");
    }

    [TestMethod]
    /// <summary>
    /// Ensure GetLogonWallpaper initializes and uninitializes COM.
    /// </summary>
    public void GetLogonWallpaper_InitializesCom() {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
        var service = new ComTrackingService();
        try { _ = service.GetLogonWallpaper(); } catch { }
        Assert.IsTrue(service.InitCalled);
        Assert.IsTrue(service.UninitCalled);
    }
}

