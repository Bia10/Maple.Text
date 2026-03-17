namespace Maple.Text.Parsing;

/// <summary>
/// Text-style modifiers for the MapleText format.
/// These are orthogonal to <see cref="MapleTextColor"/> — they control weight and size,
/// not foreground color.
/// </summary>
public enum MapleTextStyle : byte
{
    /// <summary>No-op marker; passing this value to builder methods emits nothing.</summary>
    None,

    /// <summary>#e — enable bold weight.</summary>
    Bold,

    /// <summary>#n — reset bold and all other style overrides to client default.</summary>
    Normal,

    /// <summary>
    /// #f — compact / small font.
    /// Note: when followed by a path string (via <see cref="MapleTextBuilder.CanvasLoad(string, bool)"/>)
    /// the client treats #f as an inline image prefix rather than a font-size marker.
    /// </summary>
    Small,
}
