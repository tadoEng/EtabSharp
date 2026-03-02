using EtabSharp.Core.Models;
using EtabSharp.Interfaces.System;
using EtabSharp.System;
using ETABSv1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void Close(bool savePrompt = false)
    {
        if (_disposed) return;

        try
        {
            _application.Value.ApplicationExit(savePrompt);
            _logger.LogInformation("ETABS application closed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Close: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Disposes this wrapper. Calls ApplicationExit(false) — does NOT save.
    /// For Mode A (attach) flows, do NOT dispose — release COM manually via ComCleanup.
    /// For Mode B (hidden) flows, disposing is correct and will exit the hidden instance.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Close(false);
            _disposed = true;
        }

        GC.SuppressFinalize(this);
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