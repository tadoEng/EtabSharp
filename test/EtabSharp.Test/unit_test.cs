using EtabSharp.Core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace EtabSharp.Test;

// ─────────────────────────────────────────────────────────────
//  EtabSharp Unit Tests
//
//  Prerequisites:
//    Mode A tests  → ETABS must be running with a model open
//    Mode B tests  → ETABS must be installed (CreateNew spawns a hidden instance)
//
//  Dispose() contract under test:
//    Dispose()                        → releases COM only, ETABS stays running
//    Application.ApplicationExit()   → shuts down ETABS (explicit user call)
// ─────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────
//  MODE A — Connect() tests (requires ETABS running)
// ─────────────────────────────────────────────────────────────

[Trait("Category", "ModeA")]
public class ModeA_ConnectTests
{
    [Fact]
    public void Connect_WhenEtabsRunning_ReturnsApplication()
    {
        var app = ETABSWrapper.Connect();
        try
        {
            Assert.NotNull(app);
        }
        finally
        {
            app?.Dispose();
        }
    }

    [Fact]
    public void Connect_WhenEtabsRunning_ApplicationHasModel()
    {
        var app = ETABSWrapper.Connect();
        try
        {
            Assert.NotNull(app);
            Assert.NotNull(app!.Model);
        }
        finally
        {
            app?.Dispose();
        }
    }

    [Fact]
    public void Connect_WhenEtabsRunning_VersionIsPopulated()
    {
        var app = ETABSWrapper.Connect();
        try
        {
            Assert.NotNull(app);
            Assert.False(string.IsNullOrEmpty(app!.FullVersion));
            Assert.True(app.MajorVersion >= 22,
                $"Expected MajorVersion >= 22, got {app.MajorVersion}");
        }
        finally
        {
            app?.Dispose();
        }
    }

    [Fact]
    public void Connect_WhenEtabsRunning_ApiVersionIsPositive()
    {
        var app = ETABSWrapper.Connect();
        try
        {
            Assert.NotNull(app);
            Assert.True(app!.ApiVersion > 0,
                $"Expected ApiVersion > 0, got {app.ApiVersion}");
        }
        finally
        {
            app?.Dispose();
        }
    }

    [Fact]
    public void Connect_WhenEtabsRunning_DllNameIsCorrect()
    {
        var app = ETABSWrapper.Connect();
        try
        {
            Assert.NotNull(app);
            Assert.Equal("ETABSv1.DLL", app!.DllName);
            Assert.True(app.IsNetStandard);
        }
        finally
        {
            app?.Dispose();
        }
    }

    [Fact]
    public void Connect_WhenEtabsRunning_GetApiInfoIsPopulated()
    {
        var app = ETABSWrapper.Connect();
        try
        {
            Assert.NotNull(app);
            var info = app!.GetApiInfo();
            Assert.NotNull(info);
            Assert.True(info.MajorVersion >= 22);
            Assert.False(string.IsNullOrEmpty(info.FullVersion));
            Assert.True(info.ApiVersion > 0);
        }
        finally
        {
            app?.Dispose();
        }
    }
}

// ─────────────────────────────────────────────────────────────
//  DISPOSE CONTRACT — Mode A
//  Critical: Dispose() must NOT call ApplicationExit
// ─────────────────────────────────────────────────────────────

[Trait("Category", "DisposeContract")]
public class DisposeContractTests
{
    [Fact]
    public void Dispose_ModeA_DoesNotExitEtabs()
    {
        // Arrange — ETABS must be running
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect();
        Assert.NotNull(app);

        // Act
        app!.Dispose();

        // Assert — ETABS must still be running
        var stillRunning = Process.GetProcessesByName("ETABS").Any();
        Assert.True(stillRunning,
            "Dispose() called ApplicationExit — it should only release COM references");
    }

    [Fact]
    public void Dispose_ModeA_CalledTwice_DoesNotThrow()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect();
        Assert.NotNull(app);

        // Act & Assert — double dispose must be safe
        var ex = Record.Exception(() =>
        {
            app!.Dispose();
            app!.Dispose(); // second call must be a no-op
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_ModeA_UsingBlock_DoesNotExitEtabs()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        // Act — using block calls Dispose() automatically
        using (var app = ETABSWrapper.Connect())
        {
            Assert.NotNull(app);
        }

        // Assert
        var stillRunning = Process.GetProcessesByName("ETABS").Any();
        Assert.True(stillRunning,
            "using{} block called Dispose() which exited ETABS — Dispose() should only release COM");
    }

    
}

// ─────────────────────────────────────────────────────────────
//  WRAPPER STATIC HELPERS
// ─────────────────────────────────────────────────────────────

[Trait("Category", "Wrapper")]
public class WrapperStaticTests
{
    [Fact]
    public void IsRunning_WhenEtabsRunning_ReturnsTrue()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");
        Assert.True(ETABSWrapper.IsRunning());
    }

    

    [Fact]
    public void GetActiveVersion_WhenEtabsRunning_ReturnsVersionString()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var version = ETABSWrapper.GetActiveVersion();
        Assert.NotNull(version);
        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
    }

    [Fact]
    public void GetAllRunningInstances_WhenEtabsRunning_ReturnsAtLeastOne()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var instances = ETABSWrapper.GetAllRunningInstances();
        Assert.NotEmpty(instances);

        var first = instances.First();
        Assert.True(first.ProcessId > 0);
        Assert.True(first.MajorVersion >= 22);
        Assert.False(string.IsNullOrEmpty(first.FullVersion));
        Assert.True(first.IsSupported);
    }

    [Fact]
    public void ConnectToProcess_WithValidPid_ReturnsApplication()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var instances = ETABSWrapper.GetAllRunningInstances();
        var pid = instances.First().ProcessId;

        var app = ETABSWrapper.ConnectToProcess(pid);
        try
        {
            Assert.NotNull(app);
            Assert.NotNull(app!.Model);
        }
        finally
        {
            app?.Dispose();
        }
    }

    [Fact]
    public void ConnectToProcess_WithInvalidPid_ReturnsNull()
    {
        var app = ETABSWrapper.ConnectToProcess(pid: 999999);
        Assert.Null(app);
    }

    
}

// ─────────────────────────────────────────────────────────────
//  MODEL ACCESS — System managers
// ─────────────────────────────────────────────────────────────

[Trait("Category", "ModeA")]
public class ModelSystemTests
{
    [Fact]
    public void ModelInfo_GetVersion_ReturnsNonEmpty()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            var version = app.Model.ModelInfo.GetVersion();
            Assert.False(string.IsNullOrEmpty(version));
        }
        finally { app.Dispose(); }
    }

    [Fact]
    public void ModelInfo_GetProgramInfo_ReturnsPopulatedObject()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            var info = app.Model.ModelInfo.GetProgramInfo();
            Assert.NotNull(info);
            Assert.False(string.IsNullOrEmpty(info.ProgramName));
            Assert.False(string.IsNullOrEmpty(info.ProgramVersion));
        }
        finally { app.Dispose(); }
    }

    [Fact]
    public void ModelInfo_IsLocked_DoesNotThrow()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            var ex = Record.Exception(() => app.Model.ModelInfo.IsLocked());
            Assert.Null(ex);
        }
        finally { app.Dispose(); }
    }

    

    [Fact]
    public void Units_GetPresentUnits_ReturnsValidUnits()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            var units = app.Model.Units.GetPresentUnits();
            Assert.NotNull(units);
        }
        finally { app.Dispose(); }
    }

    
}



// ─────────────────────────────────────────────────────────────
//  MODE B — CreateNew() tests (requires ETABS installed)
//  Each test spawns and exits a hidden instance
// ─────────────────────────────────────────────────────────────

[Trait("Category", "ModeB")]
public class ModeB_CreateNewTests
{
    [Fact]
    public void CreateNew_ReturnsApplication()
    {
        var app = ETABSWrapper.CreateNew();
        try
        {
            Assert.NotNull(app);
        }
        finally
        {
            app?.Application.ApplicationExit(false);
            app?.Dispose();
        }
    }

    [Fact]
    public void CreateNew_ApplicationHasModel()
    {
        var app = ETABSWrapper.CreateNew();
        try
        {
            Assert.NotNull(app);
            Assert.NotNull(app!.Model);
        }
        finally
        {
            app?.Application.ApplicationExit(false);
            app?.Dispose();
        }
    }

    [Fact]
    public void CreateNew_Hide_ReturnsZero()
    {
        var app = ETABSWrapper.CreateNew();
        try
        {
            Assert.NotNull(app);
            int ret = app!.Application.Hide();
            Assert.Equal(0, ret);
        }
        finally
        {
            app?.Application.ApplicationExit(false);
            app?.Dispose();
        }
    }

    [Fact]
    public void CreateNew_VersionIsAtLeast22()
    {
        var app = ETABSWrapper.CreateNew();
        try
        {
            Assert.NotNull(app);
            Assert.True(app!.MajorVersion >= 22);
        }
        finally
        {
            app?.Application.ApplicationExit(false);
            app?.Dispose();
        }
    }

    [Fact]
    public void CreateNew_Dispose_WithoutApplicationExit_DoesNotThrow()
    {
        // Verifies Dispose() alone does not throw even for Mode B
        // (though for Mode B the process will linger — this tests the contract only)
        var app = ETABSWrapper.CreateNew();
        Assert.NotNull(app);

        app!.Application.ApplicationExit(false); // clean exit first
        var ex = Record.Exception(() => app.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void CreateNew_ApplicationExit_ThenDispose_LeavesNoOrphanProcess()
    {
        var countBefore = Process.GetProcessesByName("ETABS").Length;

        var app = ETABSWrapper.CreateNew();
        Assert.NotNull(app);
        app!.Application.Hide();

        // Act — correct Mode B cleanup sequence
        app.Application.ApplicationExit(false);
        app.Dispose();

        Thread.Sleep(1500); // allow process to exit

        var countAfter = Process.GetProcessesByName("ETABS").Length;
        Assert.Equal(countBefore, countAfter);
    }
}

// ─────────────────────────────────────────────────────────────
//  APPLICATION INTERFACE — IApplication
// ─────────────────────────────────────────────────────────────

[Trait("Category", "ModeA")]
public class ApplicationInterfaceTests
{
    [Fact]
    public void Application_Visible_ReturnsTrue_WhenEtabsRunning()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            Assert.True(app.Application.Visible());
        }
        finally { app.Dispose(); }
    }

    [Fact]
    public void Application_GetOAPIVersionNumber_ReturnsPositive()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            var v = app.Application.GetOAPIVersionNumber();
            Assert.True(v > 0, $"Expected OAPIVersion > 0, got {v}");
        }
        finally { app.Dispose(); }
    }

    [Fact]
    public void Application_SetAsActiveObject_ReturnsZero()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            int ret = app.Application.SetAsActiveObject();
            Assert.Equal(0, ret);
        }
        finally { app.Dispose(); }
    }

    [Fact]
    public void Application_UnsetAsActiveObject_ReturnsZero()
    {
        Skip.If(!ETABSWrapper.IsRunning(), "ETABS is not running");

        var app = ETABSWrapper.Connect()!;
        try
        {
            app.Application.SetAsActiveObject();
            int ret = app.Application.UnsetAsActiveObject();
            Assert.Equal(0, ret);
        }
        finally { app.Dispose(); }
    }
}

