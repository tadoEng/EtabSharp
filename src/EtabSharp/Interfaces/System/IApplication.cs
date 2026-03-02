using System;
using System.Collections.Generic;
using System.Text;

namespace EtabSharp.Interfaces.System;

/// <summary>
/// Wraps cOAPI — application-level control for ETABS.
/// Covers lifecycle (start/exit), visibility (hide/unhide/visible),
/// and ROT registration (SetAsActiveObject/UnsetAsActiveObject).
/// </summary>
public interface IApplication
{
    #region Lifecycle

    /// <summary>
    /// Starts the ETABS application UI.
    /// Not required when using CreateNew() — ETABSWrapper calls this automatically.
    /// Returns 0 on success.
    /// </summary>
    int ApplicationStart();

    /// <summary>
    /// Exits the ETABS application.
    /// </summary>
    /// <param name="savePrompt">
    /// If true, ETABS will prompt to save unsaved changes.
    /// If false, exits immediately without saving (use in Mode B / hidden instances).
    /// </param>
    /// <returns>0 on success.</returns>
    int ApplicationExit(bool savePrompt = false);

    #endregion

    #region Visibility

    /// <summary>
    /// Hides the ETABS application window and removes it from the Windows taskbar.
    /// Call immediately after ApplicationStart() in Mode B (hidden instance) flows.
    /// Returns 0 on success.
    /// </summary>
    int Hide();

    /// <summary>
    /// Makes a previously hidden ETABS application visible again.
    /// Returns 0 on success.
    /// </summary>
    int Unhide();

    /// <summary>
    /// Returns true if the ETABS application window is currently visible.
    /// </summary>
    bool Visible();

    #endregion

    #region Version

    /// <summary>
    /// Retrieves the OAPI version number implemented by the running ETABS GUI.
    /// Useful for verifying compatibility at runtime.
    /// </summary>
    double GetOAPIVersionNumber();

    #endregion

    #region ROT Registration

    /// <summary>
    /// Sets this ETABSObject as the active instance in the system
    /// Running Object Table (ROT), replacing any previous registration.
    /// Typically used when managing multiple ETABS instances.
    /// Returns 0 on success.
    /// </summary>
    int SetAsActiveObject();

    /// <summary>
    /// Removes this ETABSObject from the system Running Object Table (ROT).
    /// Call before releasing COM in multi-instance scenarios.
    /// Returns 0 on success.
    /// </summary>
    int UnsetAsActiveObject();

    #endregion
}

