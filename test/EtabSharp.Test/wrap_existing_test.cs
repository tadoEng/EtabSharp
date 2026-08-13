using EtabSharp.Core;
using ETABSv1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace EtabSharp.Test;

// ─────────────────────────────────────────────────────────────
//  ETABSWrapper.WrapExisting — Mode C (pure wrapping)
//
//  These tests are OFFLINE. They never start, attach to, or exit ETABS: the
//  cOAPI passed in is a recording proxy, so every member the factory touches is
//  observable. That is the point of the suite — the contract is as much about
//  what WrapExisting must NOT do as what it returns.
// ─────────────────────────────────────────────────────────────

[Trait("Category", "Offline")]
public class WrapExistingTests
{
    private const int MajorVersion = 23;
    private const double ApiVersion = 1.0;
    private const string FullVersion = "23.3.0";

    private static readonly string[] LifecycleMembers =
    [
        "ApplicationStart",
        "ApplicationExit",
        "Hide",
        "Unhide",
        "Visible",
        "SetAsActiveObject",
        "UnsetAsActiveObject",
        "InternalExec",
        "GetOAPIVersionNumber"
    ];

    [Fact]
    public void WrapExisting_PreservesTheExactObjectsSupplied()
    {
        var (api, recorder, sapModel) = CreateRecordingApi();

        var app = ETABSWrapper.WrapExisting(api, MajorVersion, ApiVersion, FullVersion);

        // Same cSapModel instance — not a re-read from some other application object.
        Assert.Same(sapModel, app.SapModel);
        Assert.Equal(MajorVersion, app.MajorVersion);
        Assert.Equal(ApiVersion, app.ApiVersion);
        Assert.Equal(FullVersion, app.FullVersion);
        Assert.Single(recorder.Calls, call => call == "get_SapModel");
    }

    [Fact]
    public void WrapExisting_TouchesNoLifecycleMemberAndStartsNoProcess()
    {
        var (api, recorder, _) = CreateRecordingApi();
        var etabsBefore = Process.GetProcessesByName("ETABS").Length;

        _ = ETABSWrapper.WrapExisting(api, MajorVersion, ApiVersion, FullVersion);

        // The only member the factory may touch is SapModel.
        Assert.Equal(["get_SapModel"], recorder.Calls);
        foreach (var member in LifecycleMembers)
        {
            Assert.DoesNotContain(recorder.Calls, call => call.Contains(member, StringComparison.Ordinal));
        }

        // No creation, no attach, no ROT, no process selection.
        Assert.Equal(etabsBefore, Process.GetProcessesByName("ETABS").Length);
    }

    [Fact]
    public void WrapExisting_DisposeReleasesReferencesWithoutExitingEtabs()
    {
        var (api, recorder, _) = CreateRecordingApi();
        var app = ETABSWrapper.WrapExisting(api, MajorVersion, ApiVersion, FullVersion);

        app.Dispose();

        // Dispose is COM cleanup only. It is never a substitute for an authoritative
        // ApplicationExit(false), and must not issue one behind the caller's back.
        Assert.DoesNotContain("ApplicationExit", recorder.Calls);
        Assert.Equal(["get_SapModel"], recorder.Calls);
    }

    [Fact]
    public void WrapExisting_IsIdempotentAcrossCallsAndNeverSubstitutesAnotherInstance()
    {
        var (firstApi, _, firstSapModel) = CreateRecordingApi();
        var (secondApi, _, secondSapModel) = CreateRecordingApi();

        var first = ETABSWrapper.WrapExisting(firstApi, MajorVersion, ApiVersion, FullVersion);
        var second = ETABSWrapper.WrapExisting(secondApi, MajorVersion, ApiVersion, FullVersion);

        Assert.Same(firstSapModel, first.SapModel);
        Assert.Same(secondSapModel, second.SapModel);
        Assert.NotSame(first.SapModel, second.SapModel);
    }

    [Fact]
    public void WrapExisting_RejectsANullApi()
    {
        var error = Assert.Throws<ArgumentNullException>(
            () => ETABSWrapper.WrapExisting(null!, MajorVersion, ApiVersion, FullVersion));

        Assert.Equal("api", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WrapExisting_RejectsMissingFullVersion(string? fullVersion)
    {
        var (api, recorder, _) = CreateRecordingApi();

        var error = Assert.Throws<ArgumentException>(
            () => ETABSWrapper.WrapExisting(api, MajorVersion, ApiVersion, fullVersion!));

        Assert.Equal("fullVersion", error.ParamName);
        // Rejected before anything was read from the caller's object.
        Assert.Empty(recorder.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void WrapExisting_RejectsUnsupportedMajorVersion(int majorVersion)
    {
        var (api, recorder, _) = CreateRecordingApi();

        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => ETABSWrapper.WrapExisting(api, majorVersion, ApiVersion, FullVersion));

        Assert.Equal("majorVersion", error.ParamName);
        Assert.Empty(recorder.Calls);
    }

    [Fact]
    public void WrapExisting_RejectsNegativeApiVersion()
    {
        var (api, recorder, _) = CreateRecordingApi();

        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => ETABSWrapper.WrapExisting(api, MajorVersion, -1.0, FullVersion));

        Assert.Equal("apiVersion", error.ParamName);
        Assert.Empty(recorder.Calls);
    }

    [Fact]
    public void WrapExisting_RejectsAnApiWithNoSapModel()
    {
        var (api, recorder, _) = CreateRecordingApi(withSapModel: false);

        // An object that was never started has no usable model, and WrapExisting will not
        // start it — that stays the caller's job.
        Assert.Throws<InvalidOperationException>(
            () => ETABSWrapper.WrapExisting(api, MajorVersion, ApiVersion, FullVersion));
        Assert.Equal(["get_SapModel"], recorder.Calls);
    }

    private static (cOAPI Api, CallRecorder Recorder, cSapModel? SapModel) CreateRecordingApi(
        bool withSapModel = true)
    {
        var sapModel = withSapModel ? CreateProxy<cSapModel>() : null;
        var api = CreateProxy<cOAPI>();
        var recorder = ((RecordingProxy)(object)api).Recorder;
        recorder.Returns["get_SapModel"] = sapModel;
        return (api, recorder, sapModel);
    }

    private static T CreateProxy<T>() where T : class =>
        DispatchProxy.Create<T, RecordingProxy>();

    /// <summary>Records every member invocation so "did not touch" is provable, not assumed.</summary>
    public sealed class CallRecorder
    {
        public List<string> Calls { get; } = [];
        public Dictionary<string, object?> Returns { get; } = [];
    }

    public class RecordingProxy : DispatchProxy
    {
        public CallRecorder Recorder { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var name = targetMethod?.Name ?? "<unknown>";
            Recorder.Calls.Add(name);

            if (Recorder.Returns.TryGetValue(name, out var configured))
            {
                return configured;
            }

            var returnType = targetMethod?.ReturnType;
            return returnType is null || returnType == typeof(void) || !returnType.IsValueType
                ? null
                : Activator.CreateInstance(returnType);
        }
    }
}
