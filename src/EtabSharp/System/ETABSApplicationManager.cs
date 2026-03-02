using EtabSharp.Interfaces.System;
using ETABSv1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EtabSharp.System;

/// <summary>
/// Wraps cOAPI — the top-level ETABS COM object.
/// All application-level operations go through here.
/// </summary>
internal sealed class ETABSApplicationManager : IApplication
{
    private readonly cOAPI _api;
    private readonly ILogger _logger;

    internal ETABSApplicationManager(cOAPI api, ILogger? logger = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? NullLogger.Instance;
    }

    #region Lifecycle

    /// <inheritdoc />
    public int ApplicationStart()
    {
        _logger.LogDebug("ApplicationStart called");
        int ret = _api.ApplicationStart();
        if (ret != 0)
            _logger.LogWarning("ApplicationStart returned non-zero: {ReturnValue}", ret);
        return ret;
    }

    /// <inheritdoc />
    public int ApplicationExit(bool savePrompt = false)
    {
        _logger.LogDebug("ApplicationExit called (savePrompt={SavePrompt})", savePrompt);
        try
        {
            int ret = _api.ApplicationExit(savePrompt);
            if (ret != 0)
                _logger.LogWarning("ApplicationExit returned non-zero: {ReturnValue}", ret);
            return ret;
        }
        catch (Exception ex)
        {
            // COM may be gone already — swallow so finally blocks don't throw
            _logger.LogWarning(ex, "ApplicationExit threw (instance may already be closed)");
            return -1;
        }
    }

    #endregion

    #region Visibility

    /// <inheritdoc />
    public int Hide()
    {
        _logger.LogDebug("Hiding ETABS window");
        int ret = _api.Hide();
        if (ret != 0)
            _logger.LogWarning("Hide returned non-zero: {ReturnValue}", ret);
        return ret;
    }

    /// <inheritdoc />
    public int Unhide()
    {
        _logger.LogDebug("Unhiding ETABS window");
        int ret = _api.Unhide();
        if (ret != 0)
            _logger.LogWarning("Unhide returned non-zero: {ReturnValue}", ret);
        return ret;
    }

    /// <inheritdoc />
    public bool Visible()
    {
        return _api.Visible();
    }

    #endregion

    #region Version

    /// <inheritdoc />
    public double GetOAPIVersionNumber()
    {
        try
        {
            return _api.GetOAPIVersionNumber();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetOAPIVersionNumber failed");
            return 0.0;
        }
    }

    #endregion

    #region ROT Registration

    /// <inheritdoc />
    public int SetAsActiveObject()
    {
        _logger.LogDebug("SetAsActiveObject called");
        int ret = _api.SetAsActiveObject();
        if (ret != 0)
            _logger.LogWarning("SetAsActiveObject returned non-zero: {ReturnValue}", ret);
        return ret;
    }

    /// <inheritdoc />
    public int UnsetAsActiveObject()
    {
        _logger.LogDebug("UnsetAsActiveObject called");
        int ret = _api.UnsetAsActiveObject();
        if (ret != 0)
            _logger.LogWarning("UnsetAsActiveObject returned non-zero: {ReturnValue}", ret);
        return ret;
    }

    #endregion
}