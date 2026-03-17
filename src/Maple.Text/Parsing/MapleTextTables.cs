using System.Buffers;
using System.Collections.Frozen;

namespace Maple.Text.Parsing;

/// <summary>
/// Shared lookup tables and character-classification helpers used by both
/// <see cref="MapleTextParser"/> and <see cref="MapleTextStripper"/>.
/// All members are static and allocation-free at call time.
/// </summary>
internal static class MapleTextTables
{
    // SearchValues<char> uses SIMD-accelerated Contains() — replaces HashSet<char>.

    internal static readonly SearchValues<char> StyleCodes = SearchValues.Create([
        'b',
        'r',
        'c',
        'd',
        'g',
        'k',
        's',
        'e',
        'n',
        'f',
        'E',
        'I',
        'S',
        'K',
    ]);

    internal static readonly SearchValues<char> EntityCodes = SearchValues.Create(['t', 'z', 'i', 'o', 'm']);

    internal static readonly SearchValues<char> BlockCodes = SearchValues.Create(['D', 'Q', 'R', 'W', 'j']);

    // 'c','f','i','m','o','s','t' intentionally omitted — caught by StyleCodes/EntityCodes first.
    // 'E','I','S','K' intentionally omitted — caught by StyleCodes (2-char tokens, not long-form per GetPhrase_Sharp).
    internal static readonly SearchValues<char> ClientTokenCodes = SearchValues.Create([
        '@',
        'B',
        'F',
        'L',
        'M',
        '_',
        'a',
        'h',
        'l',
        'p',
        'q',
        'u',
        'v',
        'x',
        'y',
        'z',
    ]);

    // FrozenSet gives perfect-hash O(1) lookups; AlternateLookup avoids string allocation.
    internal static readonly FrozenSet<string> StatTokensFrozen = FrozenSet.Create(
        StringComparer.Ordinal,
        [
            "x",
            "y",
            "z",
            "u",
            "v",
            "w",
            "pad",
            "mad",
            "pdd",
            "mdd",
            "acc",
            "eva",
            "speed",
            "jump",
            "damage",
            "attackCount",
            "mobCount",
            "time",
            "mpCon",
            "hpCon",
            "moneyCon",
            "prop",
            "subProp",
            "mastery",
            "cr",
            "cdMin",
            "cdMax",
            "cooltime",
            "dot",
            "dotInterval",
            "dotTime",
            "hp",
            "mp",
            "fixdamage",
            "expR",
            "padR",
            "madR",
            "pdrX",
            "bulletCount",
            "bulletConsume",
            "itemCon",
            "itemConNo",
            "range",
            "emhp",
            "emmp",
        ]
    );

    /// <summary>
    /// Span-based lookup: avoids allocating a string just to test set membership.
    /// </summary>
    internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> StatTokenLookup =
        StatTokensFrozen.GetAlternateLookup<ReadOnlySpan<char>>();

    // ── character-classification helpers ──────────────────────────────────────

    /// <summary>
    /// Returns true when <paramref name="next"/> should cause a leading '#' to be
    /// treated as a literal character rather than a token prefix.
    /// Matches whitespace, punctuation, symbols, and digits.
    /// </summary>
    internal static bool IsLiteralHashSequence(char next)
    {
        return char.IsWhiteSpace(next) || char.IsPunctuation(next) || char.IsSymbol(next) || char.IsDigit(next);
    }

    /// <summary>
    /// Returns true when <paramref name="value"/> contains at least one non-ASCII letter
    /// (used to detect Korean MBCS particle tokens).
    /// </summary>
    internal static bool ContainsNonAsciiLetter(ReadOnlySpan<char> value)
    {
        foreach (char ch in value)
        {
            if (char.IsLetter(ch) && !char.IsAscii(ch))
                return true;
        }
        return false;
    }

    // ── markup-code mappers ───────────────────────────────────────────────────

    /// <summary>Returns the single MapleText code character for <paramref name="color"/>.</summary>
    internal static char ColorToCode(MapleTextColor color) =>
        color switch {
            MapleTextColor.Blue => 'b',
            MapleTextColor.Red => 'r',
            MapleTextColor.Cyan => 'c',
            MapleTextColor.Black => 'k',
            MapleTextColor.Dark => 'd',
            MapleTextColor.Gray => 'g',
            MapleTextColor.Sky => 's',
            _ => 'k',
        };

    /// <summary>Returns the single MapleText code character for <paramref name="style"/>.</summary>
    internal static char StyleToCode(MapleTextStyle style) =>
        style switch {
            MapleTextStyle.Bold => 'e',
            MapleTextStyle.Normal => 'n',
            MapleTextStyle.Small => 'f',
            _ => 'n',
        };
}
