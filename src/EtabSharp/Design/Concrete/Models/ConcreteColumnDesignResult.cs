namespace EtabSharp.Design.Concrete.Models;

/// <summary>
/// Represents concrete column design results for a single frame element location.
/// </summary>
public class ConcreteColumnDesignResult
{
    /// <summary>
    /// Frame element name.
    /// </summary>
    public string FrameName { get; set; } = string.Empty;

    /// <summary>
    /// Design option.
    /// </summary>
    public int Option { get; set; }

    /// <summary>
    /// Location along the frame (typically 0 to 1).
    /// </summary>
    public double Location { get; set; }

    #region PMM Interaction

    /// <summary>
    /// P-M-M (axial-moment) interaction controlling load combination.
    /// </summary>
    public string PMMCombo { get; set; } = string.Empty;

    /// <summary>
    /// P-M-M interaction reinforcement area.
    /// </summary>
    public double PMMArea { get; set; }

    /// <summary>
    /// P-M-M interaction ratio.
    /// </summary>
    public double PMMRatio { get; set; }

    #endregion

    #region Shear

    /// <summary>
    /// Major axis shear controlling load combination.
    /// </summary>
    public string VMajorCombo { get; set; } = string.Empty;

    /// <summary>
    /// Major axis shear reinforcement area.
    /// </summary>
    public double VMajorArea { get; set; }

    /// <summary>
    /// Minor axis shear controlling load combination.
    /// </summary>
    public string VMinorCombo { get; set; } = string.Empty;

    /// <summary>
    /// Minor axis shear reinforcement area.
    /// </summary>
    public double VMinorArea { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Error summary string.
    /// </summary>
    public string ErrorSummary { get; set; } = string.Empty;

    /// <summary>
    /// Warning summary string.
    /// </summary>
    public string WarningSummary { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if design passed (no errors and ratio smaller 1.0).
    /// </summary>
    public bool Passes => string.IsNullOrWhiteSpace(ErrorSummary) && PMMRatio <= 1.0;

    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{FrameName} @ {Location:F3}: Area={PMMArea:F2}, Ratio={PMMRatio:F3}, Status={(Passes ? "Pass" : "Fail")}";
    }
}