namespace EtabSharp.Elements.PointObj.Models;

/// <summary>
/// Represents a point object (joint/node) in ETABS with its coordinates and properties.
/// Point objects are the fundamental connection points where frame and area elements connect,
/// loads are applied, and supports are defined.
/// </summary>
public class Point
{
    /// <summary>
    /// Unique name of the point object in ETABS
    /// </summary>
    public required string? Name { get; set; }

    /// <summary>
    /// X coordinate in current length units
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate in current length units
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z coordinate (elevation) in current length units
    /// </summary>
    public double Z { get; set; }

    /// <summary>
    /// Coordinate system used for this point
    /// </summary>
    public string CoordinateSystem { get; set; } = "Global";

    /// <summary>
    /// Label assigned to this point
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Story level where this point exists
    /// </summary>
    public string Story { get; set; } = "";

    /// <summary>
    /// Whether this is a special point (can exist without connections)
    /// </summary>
    public bool IsSpecial { get; set; }

    /// <summary>
    /// GUID identifier for the point
    /// </summary>
    public string GUID { get; set; } = "";

    /// <summary>
    /// Restraint conditions assigned to this point
    /// </summary>
    public PointRestraint? Restraint { get; set; }

    /// <summary>
    /// Spring properties assigned to this point
    /// </summary>
    public PointSpring? Spring { get; set; }

    /// <summary>
    /// Mass assigned to this point
    /// </summary>
    public PointMass? Mass { get; set; }

    /// <summary>
    /// Diaphragm assignment
    /// </summary>
    public string DiaphragmName { get; set; } = "";

    /// <summary>
    /// Panel zone properties for this point (if applicable)
    /// </summary>
    public PointPanelZone? PanelZone { get; set; }

    /// <summary>
    /// List of groups this point belongs to
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Connectivity information for this point
    /// </summary>
    public PointConnectivity? Connectivity { get; set; }

    /// <summary>
    /// Whether this point is currently selected
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Element name associated with this point (if applicable)
    /// </summary>
    public string ElementName { get; set; } = "";

    /// <summary>
    /// Gets the distance from origin
    /// </summary>
    public double DistanceFromOrigin => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Calculates distance to another point
    /// </summary>
    public double DistanceTo(Point other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Initializes a new instance of the Point class.
    /// </summary>
    public Point()
    {
    }

    /// <summary>
    /// Initializes a new instance of the Point class with coordinates.
    /// </summary>
    /// <param name="name">Point name</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <param name="coordinateSystem">Coordinate system (default: "Global")</param>
    public Point(string name, double x, double y, double z, string coordinateSystem = "Global")
    {
        Name = name;
        X = x;
        Y = y;
        Z = z;
        CoordinateSystem = coordinateSystem;
    }

    public override string ToString()
    {
        return $"Point {Name}: ({X:F3}, {Y:F3}, {Z:F3}) [{CoordinateSystem}]";
    }
}