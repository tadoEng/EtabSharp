using EtabSharp.Core.Models;
using EtabSharp.Interfaces.System;
using EtabSharp.System;
using ETABSv1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;


namespace EtabSharp.Core;

/// <summary>
/// ETABS application wrapper for v22 and newer.
/// Entry point returned by ETABSWrapper.Connect() and ETABSWrapper.CreateNew().
///
/// Two access paths:
///   app.Application  — lifecycle, visibility, ROT (wraps cOAPI via IApplication)
///   app.Model        — all model operations (geometry, loads, analysis, results)
/// </summary>
public sealed class ETABSApplication : IDisposable
{
    private readonly cOAPI _api;
    private readonly cSapModel _sapModel;
    private readonly int _majorVersion;
    private readonly double _apiVersion;
    private readonly string _fullVersion;
    private bool _disposed = false;

    private readonly ILogger<ETABSApplication> _logger;

    private readonly Lazy<IApplication> _application;
    private readonly Lazy<ETABSModel> _model;

    /// <summary>
    /// Application-level control: lifecycle (start/exit), visibility (hide/unhide),
    /// version info, and ROT registration.
    /// Wraps cOAPI.
    /// </summary>
    public IApplication Application => _application.Value;

    /// <summary>
    /// Model operations: geometry, properties, loads, analysis, results, design.
    /// Wraps cSapModel.
    /// </summary>
    public ETABSModel Model => _model.Value;

    /// <summary>
    /// ETABS major version number (e.g., 22 for v22.7.0).
    /// </summary>
    public int MajorVersion => _majorVersion;

    /// <summary>
    /// Full ETABS version string (e.g., "22.7.0").
    /// </summary>
    public string FullVersion => _fullVersion;

    /// <summary>
    /// OAPI version number reported by the running ETABS instance.
    /// </summary>
    public double ApiVersion => _apiVersion;

    /// <summary>
    /// Always "ETABSv1.DLL" for v22+.
    /// </summary>
    public string DllName => "ETABSv1.DLL";

    /// <summary>
    /// Always true for v22+ (.NET Standard 2.0 API).
    /// </summary>
    public bool IsNetStandard => true;

    /// <summary>
    /// Direct access to the underlying cSapModel for advanced usage.
    /// Prefer Model.* properties over this wherever possible.
    /// </summary>
    public cSapModel SapModel => _sapModel;

    internal ETABSApplication(
        cOAPI api,
        int majorVersion,
        double apiVersion,
        string fullVersion,
        ILogger<ETABSApplication>? logger = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _sapModel = api.SapModel ?? throw new InvalidOperationException("SapModel is null");
        _majorVersion = majorVersion;
        _apiVersion = apiVersion;
        _fullVersion = fullVersion;
        _logger = logger ?? NullLogger<ETABSApplication>.Instance;

        _application = new Lazy<IApplication>(
            () => new ETABSApplicationManager(_api, _logger));

        _model = new Lazy<ETABSModel>(
            () => new ETABSModel(_sapModel, _logger));

        _logger.LogInformation(
            "Connected to ETABS v{Version}, API v{ApiVersion}",
            fullVersion,
            apiVersion);
    }

    /// <summary>
    /// Returns a summary of API connection info.
    /// </summary>
    public ETABSApiInfo GetApiInfo() => new ETABSApiInfo
    {
        MajorVersion = MajorVersion,
        FullVersion = FullVersion,
        ApiVersion = ApiVersion,
        DllName = DllName,
        IsNetStandard = IsNetStandard
    };

    /// <summary>
    /// Safely executes an API call with error handling and logging.
    /// ETABS v22+ throws catchable exceptions for unsupported functions.
    /// </summary>
    public T ExecuteSafely<T>(Func<T> apiCall, string? functionName = null)
    {
        try
        {
            return apiCall();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling {FunctionName}: {Message}. This function may not be supported in your version of ETABS.",
                functionName ?? "API function", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Safely executes a void API call with error handling and logging.
    /// </summary>
    public void ExecuteSafely(Action apiCall, string? functionName = null)
    {
        try
        {
            apiCall();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling {FunctionName}: {Message}. This function may not be supported in your version of ETABS.",
                functionName ?? "API function", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Exits the ETABS application.
    /// Prefer using Application.ApplicationExit() for explicit control.
    /// This method exists for IDisposable and convenience.
    /// </summary>
    /// <param name="savePrompt">
    /// If true, ETABS prompts to save unsaved changes.
    /// If false (default), exits immediately — correct for Mode B hidden instances.
    /// </param>
    // Close() remains explicit — user calls this when they want ETABS to exit
    public void Close(bool savePrompt = false)
    {
        if (_disposed) return;

        try
        {
            _application.Value.ApplicationExit(savePrompt);
            _logger.LogInformation("ETABS application exited");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during ApplicationExit: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Disposes this wrapper by releasing the COM references it holds. It does <b>not</b>
    /// call <c>ApplicationExit</c> and never shuts ETABS down.
    ///
    /// <para><b>Attached / external session</b> (<see cref="ETABSWrapper.Connect"/>,
    /// <see cref="ETABSWrapper.ConnectToProcess"/>): disposing on its own is the correct
    /// and complete cleanup. The user's ETABS stays running, which is the point — the
    /// session was never ours to end.</para>
    ///
    /// <para><b>Caller-owned session being shut down</b> (a hidden instance from
    /// <see cref="ETABSWrapper.CreateNew"/>, or a handle wrapped with
    /// <see cref="ETABSWrapper.WrapExisting"/>): request the authoritative
    /// <c>app.Application.ApplicationExit(false)</c> and resolve the process exit first,
    /// then dispose. Dispose is never a substitute for that exit — it releases references,
    /// not the application — and CSI documents that the <c>cSapModel</c> reference should
    /// be dropped after <c>ApplicationExit</c>.</para>
    ///
    /// <para>Safe to call more than once.</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Only attempt COM cleanup on Windows platforms where Marshal.ReleaseComObject is supported.
        // This prevents CA1416 diagnostics and avoids calling Windows-only runtime APIs on other platforms.
        if (OperatingSystem.IsWindows())
        {
            // Only _sapModel and _api are real COM references. _model and _application are
            // ordinary managed wrappers around them, and Marshal.ReleaseComObject rejects a
            // non-COM object — releasing them here used to throw and, because every release
            // shared one try block, could skip the two releases that actually matter.
            // Each real reference is now released independently.
            ReleaseComReference(_sapModel, nameof(SapModel));
            ReleaseComReference(_api, "cOAPI");
        }
        else
        {
            _logger.LogInformation("Skipping COM cleanup: not running on Windows platform");
        }

        GC.SuppressFinalize(this);
    }

    [SupportedOSPlatform("windows")]
    private void ReleaseComReference(object comReference, string name)
    {
        try
        {
            Marshal.ReleaseComObject(comReference);
            _logger.LogInformation("COM reference released: {Reference}", name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error releasing COM reference {Reference}: {Message}", name, ex.Message);
        }
    }

    #region Advanced / Raw Access

    /// <summary>
    /// Gets the raw cOAPI object.
    /// Use only when IApplication does not cover what you need.
    /// </summary>
    internal cOAPI GetRawAPI() => _api;

    /// <summary>
    /// Gets the raw cSapModel object.
    /// Use only when Model.* does not cover what you need.
    /// </summary>
    internal cSapModel GetRawModel() => _sapModel;

    #endregion
}