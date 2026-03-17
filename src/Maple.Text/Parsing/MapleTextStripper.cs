using System.Buffers;

namespace Maple.Text.Parsing;

/// <summary>
/// Zero-allocation markup stripping and detection for MapleText strings.
/// Implements the strip path independently of the tokenizing path in
/// <see cref="MapleTextParser"/>; both classes share lookup data via
/// <see cref="MapleTextTables"/>.
/// </summary>
public static class MapleTextStripper
{
    /// <summary>Returns true when the string contains at least one '#' character.</summary>
    public static bool ContainsMarkup(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Contains('#');
    }

    /// <summary>Returns true when the span contains at least one '#' character.</summary>
    public static bool ContainsMarkup(ReadOnlySpan<char> text) => text.IndexOf('#') >= 0;

    /// <summary>
    /// Strips all MapleText markup and returns plain-text content.
    /// Entity references are stripped from the output; use <see cref="MapleTextDecoder.Decode"/> to resolve them to readable text.
    /// Uses a stack buffer for strings ≤512 chars; falls back to ArrayPool.
    /// The only heap allocation is the final string (or none when the input has no markup).
    /// </summary>
    public static string StripMarkup(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!ContainsMarkup(text))
            return text; // fast path — no alloc

        ReadOnlySpan<char> input = text.AsSpan();
        int len = input.Length;
        char[]? rented = null;
        Span<char> buf = len <= 512 ? stackalloc char[len] : (rented = ArrayPool<char>.Shared.Rent(len));
        try
        {
            int written = StripMarkupCore(input, buf);
            return written == len ? text : new string(buf.Slice(0, written));
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Strips all MapleText markup from <paramref name="text"/> and returns plain-text content.
    /// Always allocates a new string; use the <see cref="StripMarkup(string)"/> overload when you already
    /// hold a <see cref="string"/> to benefit from the no-markup fast-path that avoids allocation.
    /// Uses a stack buffer for spans ≤512 chars; falls back to ArrayPool.
    /// </summary>
    public static string StripMarkup(ReadOnlySpan<char> text)
    {
        if (!ContainsMarkup(text))
            return new string(text);

        int len = text.Length;
        char[]? rented = null;
        Span<char> buf = len <= 512 ? stackalloc char[len] : (rented = ArrayPool<char>.Shared.Rent(len));
        try
        {
            int written = StripMarkupCore(text, buf);
            return new string(buf.Slice(0, written));
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    // ── zero-alloc strip core ─────────────────────────────────────────────────

    internal static int StripMarkupCore(ReadOnlySpan<char> input, Span<char> output)
    {
        int pos = 0,
            written = 0;
        while (pos < input.Length)
        {
            char ch = input[pos];
            if (ch != '#')
            {
                output[written++] = ch;
                pos++;
                continue;
            }

            if (pos + 1 >= input.Length)
            {
                pos++;
                break;
            } // trailing '#' at EOF — discard

            char next = input[pos + 1];
            if (next == '#')
            {
                output[written++] = '#';
                pos += 2;
                continue;
            }
            if (MapleTextTables.IsLiteralHashSequence(next))
            {
                output[written++] = '#';
                pos++;
                continue;
            }

            if (MapleTextTables.EntityCodes.Contains(next))
            {
                pos = SkipEntity(input, pos);
                continue;
            }
            if (MapleTextTables.BlockCodes.Contains(next))
            {
                int closeIdx = input.Slice(pos + 2).IndexOf('#');
                pos = closeIdx >= 0 ? pos + 2 + closeIdx + 1 : pos + 2;
                continue;
            }
            if (MapleTextTables.StyleCodes.Contains(next))
            {
                pos += 2;
                continue;
            }
            if (char.IsLetter(next))
            {
                pos = SkipLetterToken(input, pos);
                continue;
            }
            if (MapleTextTables.ClientTokenCodes.Contains(next))
            {
                pos = SkipLongForm(input, pos, next);
                continue;
            }

            pos += 2; // unknown #X
        }
        return written;
    }

    // ── token-skip helpers ────────────────────────────────────────────────────

    private static int SkipEntity(ReadOnlySpan<char> input, int pos)
    {
        int start = pos + 2;
        int p = start;
        while (p < input.Length && char.IsDigit(input[p]))
            p++;
        if (p == start)
            return pos + 2; // no digits — skip only #X
        if (p < input.Length && input[p] == ':')
        {
            while (p < input.Length && input[p] == ':')
                p++;
            return p < input.Length && input[p] == '#' ? p + 1 : p;
        }
        return p < input.Length && input[p] == '#' ? p + 1 : p;
    }

    private static int SkipLetterToken(ReadOnlySpan<char> input, int pos)
    {
        char next = input[pos + 1];
        int end = pos + 2;
        while (end < input.Length && char.IsLetter(input[end]))
            end++;
        ReadOnlySpan<char> nameSpan = input.Slice(pos + 1, end - pos - 1);
        if (MapleTextTables.StatTokenLookup.Contains(nameSpan))
            return end;
        if (MapleTextTables.ContainsNonAsciiLetter(nameSpan))
            return end;
        if (MapleTextTables.ClientTokenCodes.Contains(next))
            return SkipLongForm(input, pos, next);
        return pos + 2; // unknown multi-letter — consume only #X
    }

    private static int SkipLongForm(ReadOnlySpan<char> input, int pos, char code)
    {
        int end = pos + 2;
        while (end < input.Length)
        {
            char c = input[end];
            if (c is '#' or '\\' or '\r' or '\n')
                break;
            end++;
        }
        // #L<n># — consume the trailing '#' separator.
        if (code == 'L' && end < input.Length && input[end] == '#')
            end++;
        return end;
    }
}
