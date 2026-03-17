namespace Maple.Text.Parsing;

/// <summary>
/// Color state for a <see cref="MapleSpan"/>, derived from MapleText style codes.
/// Consumers map these to theme-appropriate brushes; <see cref="Default"/> means
/// inherit the surrounding foreground (no override).
/// </summary>
public enum MapleSpanColor : byte
{
    Default,
    Blue,
    Red,
    Cyan,
    Dark,
    Gray,
    Sky,
    Black,
}
