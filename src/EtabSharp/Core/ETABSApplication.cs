using EtabSharp.Core.Models;
using ETABSv1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EtabSharp.Core;

/// <summary>
/// ETABS application wrapper for v22 and newer
/// Provides strongly-typed access to ETABS API using ETABSv1.DLL (.NET Standard 2.0)
/// </summary>
public class ETABSApplication : IDisposable
{
    private readonly cOAPI _api;
    private readonly cSapModel _sapModel;
    private readonly int _majorVersion;
    private readonly double _apiVersion;
    private readonly string _fullVersion;
    private bool _disposed = false;

    private readonly ILogger<ETABSApplication> _logger;

    // NEW: Add the model wrapper
    private readonly Lazy<ETABSModel> _model;

    /// <summary>
    /// Gets the ETABS model instance, providing access to the building analysis and design data.
    /// </summary>
    public ETABSModel Model => _model.Value;

    /// <summary>
    /// Gets the ETABS major version (e.g., 22 for v22.7.0)
    /// </summary>
    public int MajorVersion => _majorVersion;

    /// <summary>
    /// Gets the full ETABS version (e.g., "22.7.0")
    /// </summary>
    public string FullVersion => _fullVersion;

    /// <summary>
    /// Gets the API version number
    /// </summary>
    public double ApiVersion => _apiVersion;

    /// <summary>
    /// Gets the DLL name being used
    /// </summary>
    public string DllName => "ETABSv1.DLL";

    /// <summary>
    /// Always true for v22+ (.NET Standard 2.0)
    /// </summary>
    public bool IsNetStandard => true;

    /// <summary>
    /// Gets strongly-typed cOAPI object for ETABS v22+
    /// Use this for application-level operations (file, program control, etc.)
    /// </summary>
    public ETABSv1.cOAPI API => _api;

    /// <summary>
    /// Gets strongly-typed cSapModel object for ETABS v22+
    /// Use this for model operations (geometry, loads, analysis, results, etc.)
    /// </summary>
    public ETABSv1.cSapModel SapModel => _sapModel;

    internal ETABSApplication(ETABSv1.cOAPI api, int majorVersion, double apiVersion, string fullVersion,
        ILogger<ETABSApplication>? logger = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _sapModel = api.SapModel ?? throw new InvalidOperationException("SapModel is null");
        _majorVersion = majorVersion;
        _apiVersion = apiVersion;
        _fullVersion = fullVersion;
        _logger = logger ?? NullLogger<ETABSApplication>.Instance;

        // Initialize the model wrapper with lazy loading
        _model = new Lazy<ETABSModel>(() => new ETABSModel(_sapModel, _logger));

        _logger.LogInformation(
            "Connected to ETABS v{Version}, API v{ApiVersion}",
            fullVersion,
            apiVersion);
    }

    /// <summary>
    /// Gets API information
    /// </summary>
    public ETABSApiInfo GetApiInfo()
    {
        return new ETABSApiInfo
        {
            MajorVersion = MajorVersion,
            FullVersion = FullVersion,
            ApiVersion = ApiVersion,
            DllName = DllName,
            IsNetStandard = IsNetStandard
        };
    }

    /// <summary>
    /// Safely executes an API function with error handling
    /// ETABS v22+ throws catchable exceptions for unsupported functions
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="apiCall">The API function to execute</param>
    /// <param name="functionName">Optional name for better error messages</param>
    /// <returns>Result of the API call</returns>
    public T ExecuteSafely<T>(Func<T> apiCall, string functionName = null)
    {
        try
        {
            return apiCall();
        }
        catch (Exception ex)
        {
            var funcName = functionName ?? "API function";
            _logger.LogError(ex, "Error calling {FunctionName}: {Message}. This function may not be supported in your version of ETABS.", funcName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Safely executes an API action with error handling (void return)
    /// ETABS v22+ throws catchable exceptions for unsupported functions
    /// </summary>
    /// <param name="apiCall">The API action to execute</param>
    /// <param name="functionName">Optional name for better error messages</param>
    public void ExecuteSafely(Action apiCall, string functionName = null)
    {
        try
        {
            apiCall();
        }
        catch (Exception ex)
        {
            var funcName = functionName ?? "API function";
            _logger.LogError(ex, "Error calling {FunctionName}: {Message}. This function may not be supported in your version of ETABS.", funcName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the underlying cOAPI object (for advanced usage)
    /// </summary>
    public cOAPI GetRawAPI() => _api;

    /// <summary>
    /// Gets the underlying cSapModel object (for advanced usage)
    /// </summary>
    public cSapModel GetRawModel() => _sapModel;

    /// <summary>
    /// Closes the ETABS application
    /// </summary>
    public void Close(bool savePrompt = false)
    {
        if (!_disposed)
        {
            try
            {
                _api.ApplicationExit(savePrompt);
                _logger.LogInformation("ETABS application closed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing ETABS: {Message}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Implements IDisposable to close ETABS when disposed
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
}