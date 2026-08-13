using EtabSharp.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace EtabSharp.Core;

/// <summary>
/// Factory for creating and connecting to ETABS v22+ instances.
/// Returns ETABSApplication — the single entry point for all ETABS interaction.
///
/// Three usage patterns:
///   ETABSWrapper.Connect()      — Mode A: attach to the user's running ETABS
///   ETABSWrapper.CreateNew()    — Mode B: start a new hidden instance
///   ETABSWrapper.WrapExisting() — Mode C: wrap a cOAPI the caller created and started itself
/// </summary>
public static class ETABSWrapper
{
    private const string ETABS_PROCESS_NAME = "ETABS";
    private const string ETABS_PROGID = "CSI.ETABS.API.ETABSObject";
    private const int MINIMUM_SUPPORTED_VERSION = 22;

    #region Public Factory Methods

    /// <summary>
    /// Mode C: wraps a <see cref="ETABSv1.cOAPI"/> the caller has <b>already created and
    /// already started</b>, without performing any lifecycle action of its own.
    ///
    /// <para>This is deliberately low level. It exists for callers that must own the raw
    /// CSI lifecycle and the exact OS process identity themselves — creating the object
    /// with <c>cHelper.CreateObject(path)</c>, checking <c>cOAPI.ApplicationStart()</c>,
    /// proving which process they own, and calling <c>cSapModel.InitializeNewModel()</c> —
    /// and only then want EtabSharp's model and domain abstractions over that exact
    /// instance. Callers that do not need that control should use
    /// <see cref="Connect"/> or <see cref="CreateNew"/> instead.</para>
    ///
    /// <para>This method performs <b>no</b> lifecycle or discovery work: it does not create
    /// an object, does not call <c>ApplicationStart</c>, does not attach through
    /// <c>GetObject</c>/<c>GetObjectProcess</c> or the ROT, does not enumerate or select
    /// ETABS processes, and never calls <c>Hide</c>, <c>Unhide</c> or <c>ApplicationExit</c>.
    /// The only member it touches on <paramref name="api"/> is <c>SapModel</c>.</para>
    ///
    /// <para><b>Ownership:</b> the caller owns the application lifecycle. Wrapping does not
    /// transfer it. Disposing the returned wrapper releases COM references only — it does
    /// not exit ETABS — so the caller must perform its own authoritative
    /// <c>ApplicationExit(false)</c> and process-exit confirmation, and dispose only
    /// afterwards.</para>
    /// </summary>
    /// <param name="api">
    /// An existing, already-started API object. The exact instance is preserved; nothing
    /// is created or re-attached on the caller's behalf.
    /// </param>
    /// <param name="majorVersion">ETABS major version of the running instance (22 or newer).</param>
    /// <param name="apiVersion">OAPI version reported by that instance; 0 if the caller could not read it.</param>
    /// <param name="fullVersion">Full ETABS version string of the running instance, e.g. "23.3.0".</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException"><paramref name="api"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="fullVersion"/> is null or blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="majorVersion"/> is below the minimum supported version, or
    /// <paramref name="apiVersion"/> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="api"/> exposes no <c>SapModel</c>. An object that was never started
    /// is not usable, and this method will not start it.
    /// </exception>
    public static ETABSApplication WrapExisting(
        ETABSv1.cOAPI api,
        int majorVersion,
        double apiVersion,
        string fullVersion,
        ILogger<ETABSApplication>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(api);

        if (string.IsNullOrWhiteSpace(fullVersion))
        {
            throw new ArgumentException(
                "Full version must be supplied by the caller; wrapping does not discover it.",
                nameof(fullVersion));
        }

        if (majorVersion < MINIMUM_SUPPORTED_VERSION)
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorVersion),
                majorVersion,
                $"ETABS v{MINIMUM_SUPPORTED_VERSION} is the minimum supported version.");
        }

        if (apiVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(apiVersion),
                apiVersion,
                "OAPI version cannot be negative.");
        }

        // Unlike Connect/CreateNew this returns non-null or throws: the caller already holds
        // a handle it proved, so a silent null would discard the reason wrapping failed.
        return new ETABSApplication(api, majorVersion, apiVersion, fullVersion, logger);
    }

    /// <summary>
    /// Mode A: Connects to the currently running ETABS instance (v22+).
    /// Does NOT call ApplicationStart or Hide — attaches to whatever ETABS the user has open.
    /// Returns null if no running instance is found or if version is unsupported.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public static ETABSApplication? Connect(ILogger<ETABSApplication>? logger = null)
    {
        var log = logger ?? NullLogger<ETABSApplication>.Instance;
        var processes = GetETABSProcesses();

        if (!processes.Any())
        {
            log.LogWarning("No running ETABS instances found");
            return null;
        }

        var active = FindActiveProcess(processes, log);
        if (active == null)
        {
            log.LogWarning("No ETABS instance with active main window found");
            return null;
        }

        return ConnectToETABS(active, logger);
    }

    /// <summary>
    /// Mode B: Creates a new ETABS instance and optionally hides it.
    /// Used for background / pipeline operations (generate-e2k, run-analysis, extract-results).
    ///
    /// Always call app.Application.Hide() immediately after if running in background.
    /// Always call app.Application.ApplicationExit(false) in your finally block when done.
    /// </summary>
    /// <param name="programPath">
    /// Optional path to ETABS.exe. If null, uses the latest installed version via ProgID.
    /// Can also be overridden without code changes via environment variable:
    ///   CSI_ETABS_API_ETABSObject_PATH=C:\path\to\ETABS.exe
    /// </param>
    /// <param name="startApplication">
    /// Whether to call ApplicationStart(). Default true.
    /// Set false only if you intend to call it manually after creation.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public static ETABSApplication? CreateNew(
        string? programPath = null,
        bool startApplication = true,
        ILogger<ETABSApplication>? logger = null)
    {
        var log = logger ?? NullLogger<ETABSApplication>.Instance;

        try
        {
            ETABSv1.cHelper helper = new ETABSv1.Helper();
            ETABSv1.cOAPI api;

            if (!string.IsNullOrEmpty(programPath))
            {
                log.LogDebug("Creating ETABS instance from path: {Path}", programPath);
                api = helper.CreateObject(programPath);
            }
            else
            {
                log.LogDebug("Creating ETABS instance via ProgID: {ProgID}", ETABS_PROGID);
                api = helper.CreateObjectProgID(ETABS_PROGID);
            }

            if (startApplication)
            {
                int ret = api.ApplicationStart();
                if (ret != 0)
                    log.LogWarning("ApplicationStart returned non-zero: {ReturnValue}", ret);
            }

            var versionInfo = GetVersionFromProcess(log);
            double apiVersion = GetApiVersion(helper, log);

            log.LogInformation(
                "Created new ETABS instance v{Version}, API v{ApiVersion}",
                versionInfo.fullVersion, apiVersion);

            return new ETABSApplication(api, versionInfo.majorVersion, apiVersion, versionInfo.fullVersion, logger);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to create new ETABS instance");
            return null;
        }
    }

    /// <summary>
    /// Mode A: Connects to a specific running ETABS instance by PID.
    /// Use GetAllRunningInstances() first to discover available instances and their PIDs.
    /// Returns null if the process is not found, not supported, or attach fails.
    /// </summary>
    /// <param name="pid">Process ID of the ETABS instance to attach to.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public static ETABSApplication? ConnectToProcess(int pid, ILogger<ETABSApplication>? logger = null)
    {
        var log = logger ?? NullLogger<ETABSApplication>.Instance;

        var process = GetETABSProcesses().FirstOrDefault(p => p.Id == pid);
        if (process == null)
        {
            log.LogWarning("No ETABS process found with PID {Pid}", pid);
            return null;
        }

        try
        {
            var fvi = process.MainModule!.FileVersionInfo;
            var processInfo = new ETABSProcessInfo
            {
                Process = process,
                MajorVersion = fvi.FileMajorPart,
                MinorVersion = fvi.FileMinorPart,
                BuildVersion = fvi.FileBuildPart,
                FullVersion = $"{fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}",
                ProcessName = process.ProcessName
            };

            return ConnectToETABS(processInfo, logger);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to read process info for PID {Pid}", pid);
            return null;
        }
    }



    /// <summary>
    /// Returns true if any ETABS process is currently running.
    /// </summary>
    public static bool IsRunning() => GetETABSProcesses().Any();

    /// <summary>
    /// Returns true if a supported ETABS version (v22+) is running with a main window.
    /// </summary>
    public static bool IsSupportedVersionRunning(ILogger? logger = null)
    {
        var active = FindActiveProcess(GetETABSProcesses(), logger ?? NullLogger.Instance);
        return active != null && active.MajorVersion >= MINIMUM_SUPPORTED_VERSION;
    }

    /// <summary>
    /// Returns the full version string of the active ETABS instance, or null if none found.
    /// </summary>
    public static string? GetActiveVersion(ILogger? logger = null)
    {
        var active = FindActiveProcess(GetETABSProcesses(), logger ?? NullLogger.Instance);
        return active?.FullVersion;
    }

    /// <summary>
    /// Returns info about all currently running ETABS instances.
    /// </summary>
    public static List<ETABSInstanceInfo> GetAllRunningInstances(ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        var result = new List<ETABSInstanceInfo>();

        foreach (var process in GetETABSProcesses())
        {
            try
            {
                var fvi = process.MainModule!.FileVersionInfo;
                result.Add(new ETABSInstanceInfo
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    MajorVersion = fvi.FileMajorPart,
                    FullVersion = $"{fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}",
                    HasMainWindow = process.MainWindowHandle != IntPtr.Zero,
                    WindowTitle = process.MainWindowTitle,
                    IsSupported = fvi.FileMajorPart >= MINIMUM_SUPPORTED_VERSION
                });
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Error reading process info for PID {ProcessId}", process.Id);
            }
        }

        return result;
    }

    #endregion

    #region Private Helpers

    private static List<Process> GetETABSProcesses() =>
        Process.GetProcessesByName(ETABS_PROCESS_NAME).ToList();

    private static ETABSProcessInfo? FindActiveProcess(List<Process> processes, ILogger logger)
    {
        foreach (var process in processes)
        {
            if (process.MainWindowHandle == IntPtr.Zero) continue;

            try
            {
                var fvi = process.MainModule!.FileVersionInfo;
                return new ETABSProcessInfo
                {
                    Process = process,
                    MajorVersion = fvi.FileMajorPart,
                    MinorVersion = fvi.FileMinorPart,
                    BuildVersion = fvi.FileBuildPart,
                    FullVersion = $"{fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}",
                    ProcessName = process.ProcessName
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error reading process info for PID {ProcessId}", process.Id);
            }
        }

        return null;
    }

    private static ETABSApplication? ConnectToETABS(ETABSProcessInfo processInfo, ILogger<ETABSApplication>? logger)
    {
        var log = logger ?? NullLogger<ETABSApplication>.Instance;

        if (processInfo.MajorVersion == 0)
        {
            log.LogWarning("Unable to determine ETABS version");
            return null;
        }

        if (processInfo.MajorVersion < MINIMUM_SUPPORTED_VERSION)
        {
            log.LogWarning(
                "ETABS v{Version} is not supported. Minimum required: v{MinVersion}.",
                processInfo.FullVersion, MINIMUM_SUPPORTED_VERSION);
            return null;
        }

        try
        {
            log.LogInformation("Connecting to ETABS v{Version} (PID: {Pid})...",
                processInfo.FullVersion, processInfo.Process?.Id);

            ETABSv1.cHelper helper = new ETABSv1.Helper();
            ETABSv1.cOAPI api = AttachToProcess(helper, processInfo, log);
            double apiVersion = GetApiVersion(helper, log);

            return new ETABSApplication(
                api,
                processInfo.MajorVersion,
                apiVersion,
                processInfo.FullVersion,
                logger);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to attach to ETABS v{Version}", processInfo.FullVersion);
            throw;
        }
    }

    /// <summary>
    /// Attaches to a specific ETABS process by PID using GetObjectProcess().
    /// Falls back to GetObject() (ROT-based) if PID attach fails or PID is unavailable.
    /// </summary>
    private static ETABSv1.cOAPI AttachToProcess(
        ETABSv1.cHelper helper,
        ETABSProcessInfo processInfo,
        ILogger logger)
    {
        if (processInfo.Process?.Id is int pid)
        {
            try
            {
                logger.LogDebug("Attaching to ETABS via PID {Pid}", pid);
                return helper.GetObjectProcess(ETABS_PROGID, pid);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "GetObjectProcess({Pid}) failed, falling back to GetObject()", pid);
            }
        }

        logger.LogDebug("Attaching to ETABS via ROT (GetObject)");
        return helper.GetObject(ETABS_PROGID);
    }

    private static double GetApiVersion(ETABSv1.cHelper helper, ILogger logger)
    {
        try { return helper.GetOAPIVersionNumber(); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get API version number");
            return 0.0;
        }
    }

    /// <summary>
    /// Reads version from currently running ETABS process (used after CreateNew).
    /// Falls back to 22.0.0 if process can't be read.
    /// </summary>
    private static (int majorVersion, string fullVersion) GetVersionFromProcess(ILogger logger)
    {
        try
        {
            var active = FindActiveProcess(GetETABSProcesses(), logger);
            if (active != null)
                return (active.MajorVersion, active.FullVersion);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error reading version from process");
        }

        logger.LogWarning("Could not determine ETABS version, defaulting to v22.0.0");
        return (22, "22.0.0");
    }

    #endregion
}

/// <summary>
/// Internal process snapshot used during connection setup.
/// </summary>
internal sealed class ETABSProcessInfo
{
    public Process? Process { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int BuildVersion { get; set; }
    public string? FullVersion { get; set; }
    public string? ProcessName { get; set; }
}