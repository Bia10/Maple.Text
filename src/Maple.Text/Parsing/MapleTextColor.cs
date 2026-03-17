namespace Maple.Text.Parsing;

/// <summary>
/// Color codes for the MapleText format.
/// Pass <see cref="None"/> to suppress any color override.
/// </summary>
/// <remarks>
/// Bold, Normal, and Small are text-style modifiers, not colors — see <see cref="MapleTextStyle"/>.
/// </remarks>
public enum MapleTextColor : byte
{
    None,
    Blue,
    Red,
    Cyan,
    Black,
    Dark,
    Gray,
    Sky,
}
