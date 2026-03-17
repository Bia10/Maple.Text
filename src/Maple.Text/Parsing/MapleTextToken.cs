using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Maple.Text.Parsing;

/// <summary>
/// Zero-allocation token descriptor — all positional data are integer offsets into the
/// source string. Use <see cref="GetRaw"/> / <see cref="GetPayload"/> for zero-alloc span
/// access, or <see cref="GetRawString"/> / <see cref="GetValue"/> to materialise strings
/// on demand.
/// </summary>
/// <remarks>
/// Field declaration order is intentional for minimal struct padding:
/// <code>
///   Code          char    offset  0  (2 B)
///   Kind          byte    offset  2  (1 B)
///   [padding]             offset  3  (1 B)
///   Start         ushort  offset  4  (2 B)
///   Length        ushort  offset  6  (2 B)
///   PayloadStart  ushort  offset  8  (2 B)
///   PayloadLength ushort  offset 10  (2 B)
///   ─────────────────────────────────────
///   Total                         12 B
/// </code>
/// The <c>ushort</c> offsets cap representable string length at 65 535 characters,
/// which matches the WZ format's own 2-byte string-length prefix.
/// </remarks>
public readonly struct MapleTextToken
{
    // Static constructor verifies struct size has not drifted from the intended layout.
    // Fires only in Debug builds; stripped entirely from Release.
    static MapleTextToken()
    {
        Debug.Assert(
            Unsafe.SizeOf<MapleTextToken>() == 12,
            "MapleTextToken layout changed unexpectedly — review field declaration order."
        );
    }

    /// <summary>
    /// Primary code character for #X tokens (e.g. 't', 'b', 'D').
    /// '\0' for Text, Escape, and MBCS ClientTokens whose payload starts at Start+1.
    /// Declared first to pair with <see cref="Kind"/> at the struct head with minimal padding.
    /// </summary>
    public char Code { get; init; }

    /// <summary>Token classification.</summary>
    public MapleTextTokenKind Kind { get; init; }

    /// <summary>Offset of the first character of this token in the source string.</summary>
    public ushort Start { get; init; }

    /// <summary>Number of characters consumed by this token (including delimiters).</summary>
    public ushort Length { get; init; }

    /// <summary>Start of the payload substring within source (digits / text after the code char).</summary>
    public ushort PayloadStart { get; init; }

    /// <summary>Length of the payload substring.</summary>
    public ushort PayloadLength { get; init; }

    // ── zero-alloc access ─────────────────────────────────────────────────────

    /// <summary>Returns a span over the raw token characters without allocating.</summary>
    public ReadOnlySpan<char> GetRaw(ReadOnlySpan<char> source) => source.Slice(Start, Length);

    /// <summary>Returns a span over the payload characters without allocating.</summary>
    public ReadOnlySpan<char> GetPayload(ReadOnlySpan<char> source) =>
        PayloadLength > 0 ? source.Slice(PayloadStart, PayloadLength) : ReadOnlySpan<char>.Empty;

    // ── allocating helpers ────────────────────────────────────────────────────

    /// <summary>Allocates and returns the raw token text from <paramref name="source"/>.</summary>
    public string GetRawString(string source) => source.Substring(Start, Length);

    /// <summary>
    /// Reconstructs the value string in the same format as the former record's <c>Value</c> field.
    /// <list type="bullet">
    ///   <item>Text → <c>""</c></item>
    ///   <item>Escape → <c>"#"</c></item>
    ///   <item>StyleCode / UnknownCode → single code-char string (e.g. <c>"b"</c>)</item>
    ///   <item>StatToken → stat-name substring (e.g. <c>"mpCon"</c>)</item>
    ///   <item>MBCS ClientToken (Code == '\0') → letter-span value (e.g. <c>"m\uXXXX"</c>)</item>
    ///   <item>EntityReference / Block / ClientToken with payload → <c>"code:payload"</c></item>
    ///   <item>ClientToken without payload → single code-char string (e.g. <c>"l"</c>)</item>
    /// </list>
    /// </summary>
    public string GetValue(string source)
    {
        switch (Kind)
        {
            case MapleTextTokenKind.Text:
                return string.Empty;

            case MapleTextTokenKind.Escape:
                return "#";

            case MapleTextTokenKind.StyleCode:
            case MapleTextTokenKind.UnknownCode:
                return Code.ToString();

            case MapleTextTokenKind.StatToken:
                return PayloadLength > 0 ? source.Substring(PayloadStart, PayloadLength) : Code.ToString();

            case MapleTextTokenKind.ClientToken when Code == '\0':
                // MBCS particle token: payload IS the full value (includes code char in span).
                return PayloadLength > 0 ? source.Substring(PayloadStart, PayloadLength) : string.Empty;

            default:
                if (PayloadLength == 0)
                    return Code.ToString();
                return string.Create(
                    PayloadLength + 2,
                    (Code, source, PayloadStart, PayloadLength),
                    static (span, s) =>
                    {
                        span[0] = s.Code;
                        span[1] = ':';
                        s.source.AsSpan(s.PayloadStart, s.PayloadLength).CopyTo(span.Slice(2));
                    }
                );
        }
    }
}
