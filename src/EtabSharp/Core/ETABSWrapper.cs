using EtabSharp.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace EtabSharp.Core;

/// <summary>
/// Wrapper for connecting to and interacting with ETABS v22 and newer
/// Uses ETABSv1.DLL with .NET Standard 2.0 API
/// </summary>
public class ETABSWrapper
{
    private const string ETABS_PROCESS_NAME = "ETABS";
    private const string ETABS_PROGID = "CSI.ETABS.API.ETABSObject";
    private const int MINIMUM_SUPPORTED_VERSION = 22;

    /// <summary>
    /// Connects to running ETABS instance (v22+)
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>ETABS application wrapper with typed access to API, or null if none found</returns>
    public static ETABSApplication Connect(ILogger<ETABSApplication>? logger = null)
    {
        var wrapperLogger = logger ?? NullLogger<ETABSApplication>.Instance;
        var etabsProcesses = GetETABSProcesses();

        if (!etabsProcesses.Any())
        {
            wrapperLogger.LogWarning("No running ETABS instances found");
            return null;
        }

        var activeProcess = FindActiveProcess(etabsProcesses, wrapperLogger);

        if (activeProcess == null)
        {
            wrapperLogger.LogWarning("No ETABS instance with active window found");
            return null;
        }

        return ConnectToETABS(activeProcess, logger);
    }

    /// <summary>
    /// Creates a new ETABS instance
    /// </summary>
    /// <param name="programPath">Optional path to ETABS.exe. If null, uses latest installed version</param>
    /// <param name="startApplication">Whether to start the application UI</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>ETABS application wrapper</returns>
    public static ETABSApplication CreateNew(string programPath = null, bool startApplication = true,
        ILogger<ETABSApplication>? logger = null)
    {
        var wrapperLogger = logger ?? NullLogger<ETABSApplication>.Instance;

        try
        {
            // Create API helper object
            ETABSv1.cHelper helper = new ETABSv1.Helper();
            ETABSv1.cOAPI api;

            if (!string.IsNullOrEmpty(programPath))
            {
                // Create instance from specified path
                wrapperLogger.LogDebug("Creating ETABS instance from path: {Path}", programPath);
                api = helper.CreateObject(programPath);
            }
            else
            {
                // Create instance from latest installed ETABS
                wrapperLogger.LogDebug("Creating ETABS instance using ProgID: {ProgID}", ETABS_PROGID);
                api = helper.CreateObjectProgID(ETABS_PROGID);
            }

            if (startApplication)
            {
                // Start ETABS application UI
                int ret = api.ApplicationStart();
                if (ret != 0)
                {
                    wrapperLogger.LogWarning("ApplicationStart returned non-zero value: {ReturnValue}", ret);
                }
            }

            // Get version info
            var versionInfo = GetVersionFromAPI(api, wrapperLogger);
            int version = versionInfo.majorVersion;
            string fullVersion = versionInfo.fullVersion;
            double apiVersion = GetApiVersionNumber(helper, wrapperLogger);

            wrapperLogger.LogInformation("Created new ETABS instance v{Version}, API Version: {ApiVersion}", fullVersion, apiVersion);

            return new ETABSApplication(api, version, apiVersion, fullVersion, logger);
        }
        catch (Exception ex)
        {
            wrapperLogger.LogError(ex, "Error creating new ETABS instance");
            return null;
        }
    }

    /// <summary>
    /// Gets all running ETABS processes
    /// </summary>
    private static List<Process> GetETABSProcesses()
    {
        return Process.GetProcessesByName(ETABS_PROCESS_NAME).ToList();
    }

    /// <summary>
    /// Finds the first ETABS process with an active main window
    /// </summary>
    private static ETABSProcessInfo FindActiveProcess(List<Process> processes, ILogger logger)
    {
        foreach (var process in processes)
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                try
                {
                    var fileVersionInfo = process.MainModule.FileVersionInfo;

                    return new ETABSProcessInfo
                    {
                        Process = process,
                        MajorVersion = fileVersionInfo.FileMajorPart,
                        MinorVersion = fileVersionInfo.FileMinorPart,
                        BuildVersion = fileVersionInfo.FileBuildPart,
                        FullVersion =
                            $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}",
                        ProcessName = process.ProcessName
                    };
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error reading process info for PID {ProcessId}", process.Id);
                    continue;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Connects to the ETABS process and returns wrapper
    /// </summary>
    private static ETABSApplication ConnectToETABS(ETABSProcessInfo processInfo, ILogger<ETABSApplication>? logger)
    {
        var wrapperLogger = logger ?? NullLogger<ETABSApplication>.Instance;

        if (processInfo.MajorVersion == 0)
        {
            wrapperLogger.LogWarning("Unable to determine ETABS version");
            return null;
        }

        if (processInfo.MajorVersion < MINIMUM_SUPPORTED_VERSION)
        {
            wrapperLogger.LogWarning(
                "ETABS v{Version} is not supported. This wrapper requires ETABS v{MinVersion} or newer. Please upgrade your ETABS installation.",
                processInfo.FullVersion,
                MINIMUM_SUPPORTED_VERSION);
            return null;
        }

        try
        {
            wrapperLogger.LogInformation("Connecting to ETABS v{Version}...", processInfo.FullVersion);
            return CreateETABSApplication(processInfo.MajorVersion, processInfo.FullVersion, logger);
        }
        catch (Exception ex)
        {
            wrapperLogger.LogError(ex, "Error connecting to ETABS v{Version}", processInfo.FullVersion);
            return null;
        }
    }

    /// <summary>
    /// Creates ETABS application wrapper for v22+ by attaching to running instance
    /// </summary>
    private static ETABSApplication CreateETABSApplication(int majorVersion, string fullVersion,
        ILogger<ETABSApplication>? logger)
    {
        var wrapperLogger = logger ?? NullLogger<ETABSApplication>.Instance;

        try
        {
            // Create helper object
            ETABSv1.cHelper helper = new ETABSv1.Helper();

            // Get the active ETABS object
            ETABSv1.cOAPI api = helper.GetObject(ETABS_PROGID);

            // Get the API version number
            double apiVersion = GetApiVersionNumber(helper, wrapperLogger);

            wrapperLogger.LogInformation("Connected to ETABS v{Version}, API Version: {ApiVersion}", fullVersion, apiVersion);

            return new ETABSApplication(api, majorVersion, apiVersion, fullVersion, logger);
        }
        catch (Exception ex)
        {
            wrapperLogger.LogError(ex, "Failed to attach to ETABS");
            throw;
        }
    }

    /// <summary>
    /// Gets the API version number from helper
    /// </summary>
    private static double GetApiVersionNumber(ETABSv1.cHelper helper, ILogger logger)
    {
        try
        {
            return helper.GetOAPIVersionNumber();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get API version number");
            return 0.0;
        }
    }

    /// <summary>
    /// Gets version from API object by examining process
    /// </summary>
    private static (int majorVersion, string fullVersion) GetVersionFromAPI(ETABSv1.cOAPI api, ILogger logger)
    {
        try
        {
            var processes = GetETABSProcesses();
            var activeProcess = FindActiveProcess(processes, logger);
            if (activeProcess != null)
            {
                return (activeProcess.MajorVersion, activeProcess.FullVersion);
            }

            logger.LogWarning("Unable to determine ETABS version from process, defaulting to v22.0.0");
            return (22, "22.0.0"); // Default to 22.0.0 if can't determine
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting version from API, defaulting to v22.0.0");
            return (22, "22.0.0"); // Default to 22.0.0
        }
    }

    /// <summary>
    /// Gets list of all running ETABS instances with their version info
    /// </summary>
    public static List<ETABSInstanceInfo> GetAllRunningInstances(ILogger? logger = null)
    {
        var wrapperLogger = logger ?? NullLogger.Instance;
        var instances = new List<ETABSInstanceInfo>();
        var processes = GetETABSProcesses();

        foreach (var process in processes)
        {
            try
            {
                var fileVersionInfo = process.MainModule.FileVersionInfo;
                int majorVersion = fileVersionInfo.FileMajorPart;
                string fullVersion =
                    $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}";

                instances.Add(new ETABSInstanceInfo
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    MajorVersion = majorVersion,
                    FullVersion = fullVersion,
                    HasMainWindow = process.MainWindowHandle != IntPtr.Zero,
                    WindowTitle = process.MainWindowTitle,
                    IsSupported = majorVersion >= MINIMUM_SUPPORTED_VERSION
                });
            }
            catch (Exception ex)
            {
                wrapperLogger.LogWarning(ex, "Error reading process {ProcessId}", process.Id);
            }
        }

        return instances;
    }

    /// <summary>
    /// Checks if ETABS is currently running
    /// </summary>
    public static bool IsRunning()
    {
        return GetETABSProcesses().Any();
    }

    /// <summary>
    /// Checks if a supported version of ETABS (v22+) is running
    /// </summary>
    public static bool IsSupportedVersionRunning(ILogger? logger = null)
    {
        var wrapperLogger = logger ?? NullLogger.Instance;
        var processes = GetETABSProcesses();
        var activeProcess = FindActiveProcess(processes, wrapperLogger);
        return activeProcess != null && activeProcess.MajorVersion >= MINIMUM_SUPPORTED_VERSION;
    }

    /// <summary>
    /// Gets the version of the active ETABS instance
    /// </summary>
    public static string GetActiveVersion(ILogger? logger = null)
    {
        var wrapperLogger = logger ?? NullLogger.Instance;
        var processes = GetETABSProcesses();
        var activeProcess = FindActiveProcess(processes, wrapperLogger);
        return activeProcess?.FullVersion;
    }
}

/// <summary>
/// Internal class to store ETABS process information
/// </summary>
internal class ETABSProcessInfo
{
    public Process? Process { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int BuildVersion { get; set; }
    public string? FullVersion { get; set; }
    public string? ProcessName { get; set; }
}