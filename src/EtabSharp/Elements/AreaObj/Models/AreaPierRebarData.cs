namespace EtabSharp.Elements.AreaObj.Models;

/// <summary>
/// Represents rebar data for an area pier (shear wall pier).
/// </summary>
public class AreaPierRebarData
{
    /// <summary>
    /// Name of the area pier
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of rebar layers
    /// </summary>
    public int NumberOfLayers { get; set; }

    /// <summary>
    /// Rebar layer information
    /// </summary>
    public List<AreaRebarLayer> Layers { get; set; } = new();

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Pier {Name}: {NumberOfLayers} rebar layers";
    }
}