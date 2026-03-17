namespace Maple.Text.Parsing;

/// <summary>
/// Classifies a single <see cref="MapleTextToken"/> produced by <see cref="MapleTextParser"/>.
/// Backed by <see cref="byte"/> — 10 values fit well within its range and the narrow type
/// contributes to the 12-byte struct layout of <see cref="MapleTextToken"/>.
/// </summary>
public enum MapleTextTokenKind : byte
{
    Text,
    StyleCode,
    StatToken,
    EntityReference,
    Escape,
    Block,
    ClientToken,
    UnknownCode,
    UnterminatedEntity,
    UnterminatedBlock,
}
