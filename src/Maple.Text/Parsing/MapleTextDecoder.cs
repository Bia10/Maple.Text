namespace Maple.Text.Parsing;

/// <summary>
/// Decodes a MapleText string into plain UTF-16 text by walking the token stream
/// produced by <see cref="MapleTextParser"/> and delegating resolution to an optional
/// <see cref="IMapleTextResolver"/>.
/// <para>
/// Output is accumulated into a <see cref="ValueStringBuilder"/> backed by a stack
/// buffer; a single heap allocation produces the final string.
/// </para>
/// </summary>
public static class MapleTextDecoder
{
    private const int StackBufferSize = 512; // covers typical MapleText strings without a heap allocation; ValueStringBuilder falls back to the heap when exceeded

    /// <summary>
    /// Decodes <paramref name="text"/> into plain text.
    /// <para>
    /// When <paramref name="resolver"/> is <see langword="null"/>:
    /// <list type="bullet">
    /// <item>Stat tokens are left in their raw form (e.g. "#mpCon").</item>
    /// <item>Entity references render as their value portion (e.g. "t:2000001") without allocating an intermediate string.</item>
    /// <item>Client tokens and block tokens produce no output.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static string Decode(string text, IMapleTextResolver? resolver = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!MapleTextStripper.ContainsMarkup(text))
            return text; // fast path — no markup, no alloc
        MapleTextParseResult parsed = MapleTextParser.Parse(text);
        ReadOnlySpan<char> sourceSpan = text.AsSpan();
        // TokensSpan avoids IReadOnlyList<T> interface dispatch and enumerator boxing.
        ReadOnlySpan<MapleTextToken> tokens = parsed.TokensSpan;

        var vsb = new ValueStringBuilder(stackalloc char[StackBufferSize]);
        try
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                // ref readonly: avoids copying the 12-byte struct on every iteration.
                ref readonly MapleTextToken token = ref tokens[i];
                switch (token.Kind)
                {
                    case MapleTextTokenKind.Text:
                        vsb.Append(token.GetRaw(sourceSpan));
                        break;

                    case MapleTextTokenKind.Escape:
                        vsb.Append('#');
                        break;

                    case MapleTextTokenKind.StatToken:
                        if (resolver is not null)
                            // ResolveStatSpan avoids allocating a substring key string.
                            vsb.Append(resolver.ResolveStatSpan(token.GetPayload(sourceSpan)));
                        else
                            vsb.Append(token.GetRaw(sourceSpan));
                        break;

                    case MapleTextTokenKind.EntityReference:
                        if (resolver is not null)
                        {
                            vsb.Append(resolver.ResolveEntitySpan(token.Code, token.GetPayload(sourceSpan)));
                        }
                        else
                        {
                            // Write "code:payload" directly — no intermediate string.
                            vsb.Append(token.Code);
                            vsb.Append(':');
                            vsb.Append(token.GetPayload(sourceSpan));
                        }
                        break;

                    case MapleTextTokenKind.ClientToken:
                        if (resolver is not null)
                            vsb.Append(resolver.ResolveClientTokenSpan(token.Code, token.GetPayload(sourceSpan)));
                        break;

                    case MapleTextTokenKind.Block:
                        if (resolver is not null)
                            vsb.Append(resolver.ResolveBlockSpan(token.Code, token.GetPayload(sourceSpan)));
                        break;

                    // StyleCode, UnknownCode, Unterminated* → stripped in plain-text output
                    case MapleTextTokenKind.StyleCode:
                    case MapleTextTokenKind.UnknownCode:
                    case MapleTextTokenKind.UnterminatedEntity:
                    case MapleTextTokenKind.UnterminatedBlock:
                        break;
                }
            }

            return vsb.ToString();
        }
        finally
        {
            vsb.Dispose();
        }
    }
}
