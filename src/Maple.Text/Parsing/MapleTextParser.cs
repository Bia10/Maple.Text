namespace Maple.Text.Parsing;

/// <summary>
/// Pure tokenizer for MapleText strings. Produces a list of <see cref="MapleTextToken"/>
/// structs that store only integer offsets into the source — no intermediate strings are
/// allocated during tokenization.
/// <para>
/// Lookup tables and character-classification helpers live in <see cref="MapleTextTables"/>.
/// Markup stripping and detection live in <see cref="MapleTextStripper"/>.
/// </para>
/// </summary>
public static class MapleTextParser
{
    /// <summary>
    /// Tokenises <paramref name="text"/> into a list of struct tokens.
    /// Each token stores only offsets — no intermediate strings are allocated.
    /// </summary>
    public static MapleTextParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, ushort.MaxValue, nameof(text));
        ReadOnlySpan<char> span = text.AsSpan();
        // Upper-bound estimate: each '#' produces at most one token.
        // MemoryExtensions.Count is SIMD-accelerated — near-zero cost on modern hardware.
        int estimatedTokens = span.Count('#');
        var tokens = new List<MapleTextToken>(Math.Max(4, estimatedTokens));
        bool hasErrors = false;
        int textStart = 0;
        int index = 0;

        while (index < span.Length)
        {
            if (span[index] != '#')
            {
                index++;
                continue;
            }

            if (index > textStart)
                EmitText(tokens, textStart, index - textStart);

            if (index + 1 >= span.Length)
            {
                // Trailing '#' at EOF — the client ignores it, so we discard it too.
                textStart = span.Length;
                break;
            }

            char next = span[index + 1];

            if (next == '#')
            {
                // ##<StyleCode> (e.g. ##k, ##n) — WZ authors frequently write this when they
                // mean a single style-code reset after a long-form entity token.  Treat it as
                // the style code rather than Escape so color/bold state is updated correctly.
                if (index + 2 < span.Length && MapleTextTables.StyleCodes.Contains(span[index + 2]))
                {
                    tokens.Add(
                        new MapleTextToken
                        {
                            Kind = MapleTextTokenKind.StyleCode,
                            Code = span[index + 2],
                            Start = (ushort)index,
                            Length = 3,
                        }
                    );
                    index += 3;
                    textStart = index;
                }
                else
                {
                    tokens.Add(
                        new MapleTextToken
                        {
                            Kind = MapleTextTokenKind.Escape,
                            Start = (ushort)index,
                            Length = 2,
                        }
                    );
                    index += 2;
                    textStart = index;
                }
                continue;
            }

            if (MapleTextTables.IsLiteralHashSequence(next))
            {
                tokens.Add(
                    new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.Text,
                        Start = (ushort)index,
                        Length = 1,
                    }
                );
                index++;
                textStart = index;
                continue;
            }

            if (MapleTextTables.EntityCodes.Contains(next))
            {
                if (TryParseEntity(span, index, out MapleTextToken token, out int ni))
                {
                    tokens.Add(token);
                    if (token.Kind == MapleTextTokenKind.UnknownCode)
                        hasErrors = true;
                    index = ni;
                    textStart = index;
                    continue;
                }
                tokens.Add(
                    new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.UnterminatedEntity,
                        Code = next,
                        Start = (ushort)index,
                        Length = 2,
                    }
                );
                hasErrors = true;
                index += 2;
                textStart = index;
                continue;
            }

            if (MapleTextTables.BlockCodes.Contains(next))
            {
                if (TryParseBlock(span, index, out MapleTextToken token, out int ni))
                {
                    tokens.Add(token);
                    index = ni;
                    textStart = index;
                    continue;
                }
                tokens.Add(
                    new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.UnterminatedBlock,
                        Code = next,
                        Start = (ushort)index,
                        Length = 2,
                    }
                );
                hasErrors = true;
                index += 2;
                textStart = index;
                continue;
            }

            if (MapleTextTables.StyleCodes.Contains(next))
            {
                tokens.Add(
                    new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.StyleCode,
                        Code = next,
                        Start = (ushort)index,
                        Length = 2,
                    }
                );
                index += 2;
                textStart = index;
                continue;
            }

            if (char.IsLetter(next))
            {
                int tokenEnd = index + 2;
                while (tokenEnd < span.Length && char.IsLetter(span[tokenEnd]))
                    tokenEnd++;

                // nameSpan: from the code char (index+1) to tokenEnd.
                ReadOnlySpan<char> nameSpan = span.Slice(index + 1, tokenEnd - index - 1);

                if (MapleTextTables.ContainsNonAsciiLetter(nameSpan))
                {
                    // MBCS particle token: Code='\0', payload covers the whole nameSpan.
                    tokens.Add(
                        new MapleTextToken
                        {
                            Kind = MapleTextTokenKind.ClientToken,
                            Code = '\0',
                            Start = (ushort)index,
                            Length = (ushort)(tokenEnd - index),
                            PayloadStart = (ushort)(index + 1),
                            PayloadLength = (ushort)(tokenEnd - index - 1),
                        }
                    );
                    index = tokenEnd;
                    textStart = index;
                    continue;
                }

                if (MapleTextTables.StatTokenLookup.Contains(nameSpan))
                {
                    tokens.Add(
                        new MapleTextToken
                        {
                            Kind = MapleTextTokenKind.StatToken,
                            Code = next,
                            Start = (ushort)index,
                            Length = (ushort)(tokenEnd - index),
                            PayloadStart = (ushort)(index + 1),
                            PayloadLength = (ushort)(tokenEnd - index - 1),
                        }
                    );
                    index = tokenEnd;
                    textStart = index;
                    continue;
                }

                if (MapleTextTables.ClientTokenCodes.Contains(next))
                {
                    ParseLongFormToken(span, index, out MapleTextToken token, out int ni);
                    tokens.Add(token);
                    index = ni;
                    // #L<n># uses '#' as a separator between index and option text — consume it.
                    if (next == 'L' && index < span.Length && span[index] == '#')
                        index++;
                    textStart = index;
                    continue;
                }

                // Unknown multi-letter token — consume only #X per client behaviour.
                tokens.Add(
                    new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.UnknownCode,
                        Code = next,
                        Start = (ushort)index,
                        Length = 2,
                    }
                );
                hasErrors = true;
                index += 2;
                textStart = index;
                continue;
            }

            if (MapleTextTables.ClientTokenCodes.Contains(next))
            {
                ParseLongFormToken(span, index, out MapleTextToken token, out int ni);
                tokens.Add(token);
                index = ni;
                textStart = index;
                continue;
            }

            // Completely unknown token — consume #X.
            tokens.Add(
                new MapleTextToken
                {
                    Kind = MapleTextTokenKind.UnknownCode,
                    Code = next,
                    Start = (ushort)index,
                    Length = 2,
                }
            );
            hasErrors = true;
            index += 2;
            textStart = index;
        }

        if (textStart < span.Length)
            EmitText(tokens, textStart, span.Length - textStart);

        return new MapleTextParseResult(tokens, hasErrors);
    }

    // ── internal parse helpers ────────────────────────────────────────────────

    private static void EmitText(List<MapleTextToken> tokens, int start, int length)
    {
        if (length > 0)
            tokens.Add(
                new MapleTextToken
                {
                    Kind = MapleTextTokenKind.Text,
                    Start = (ushort)start,
                    Length = (ushort)length,
                }
            );
    }

    private static bool TryParseEntity(ReadOnlySpan<char> span, int index, out MapleTextToken token, out int nextIndex)
    {
        token = default;
        nextIndex = index;
        char code = span[index + 1];
        int start = index + 2; // first char after #X
        int pos = start;

        // Scan digits.
        while (pos < span.Length && char.IsDigit(span[pos]))
            pos++;
        int digitEnd = pos;

        if (digitEnd == start)
        {
            // No digits after code — try letter sequence (stat / MBCS / long-form).
            int letterEnd = start;
            while (letterEnd < span.Length && char.IsLetter(span[letterEnd]))
                letterEnd++;

            if (letterEnd > start)
            {
                ReadOnlySpan<char> nameSpan = span.Slice(index + 1, letterEnd - index - 1);

                if (MapleTextTables.StatTokenLookup.Contains(nameSpan))
                {
                    token = new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.StatToken,
                        Code = code,
                        Start = (ushort)index,
                        Length = (ushort)(letterEnd - index),
                        PayloadStart = (ushort)(index + 1),
                        PayloadLength = (ushort)(letterEnd - index - 1),
                    };
                    nextIndex = letterEnd;
                    return true;
                }

                if (MapleTextTables.ContainsNonAsciiLetter(nameSpan))
                {
                    token = new MapleTextToken
                    {
                        Kind = MapleTextTokenKind.ClientToken,
                        Code = '\0',
                        Start = (ushort)index,
                        Length = (ushort)(letterEnd - index),
                        PayloadStart = (ushort)(index + 1),
                        PayloadLength = (ushort)(letterEnd - index - 1),
                    };
                    nextIndex = letterEnd;
                    return true;
                }
            }

            if (start < span.Length && char.IsLetter(span[start]) && MapleTextTables.ClientTokenCodes.Contains(code))
            {
                ParseLongFormToken(span, index, out token, out nextIndex);
                return true;
            }

            // #X with no valid continuation.
            if (start >= span.Length || span[start] == '#' || MapleTextTables.IsLiteralHashSequence(span[start]))
                token = new MapleTextToken
                {
                    Kind = MapleTextTokenKind.Text,
                    Start = (ushort)index,
                    Length = 2,
                };
            else
                token = new MapleTextToken
                {
                    Kind = MapleTextTokenKind.UnknownCode,
                    Code = code,
                    Start = (ushort)index,
                    Length = 2,
                };

            nextIndex = index + 2;
            return true;
        }

        // Colon variant: #i4000001:# or #i4000001::#
        if (digitEnd < span.Length && span[digitEnd] == ':')
        {
            int colonEnd = digitEnd;
            while (colonEnd < span.Length && span[colonEnd] == ':')
                colonEnd++;
            if (colonEnd < span.Length && span[colonEnd] == '#')
            {
                token = new MapleTextToken
                {
                    Kind = MapleTextTokenKind.EntityReference,
                    Code = code,
                    Start = (ushort)index,
                    Length = (ushort)(colonEnd - index + 1),
                    PayloadStart = (ushort)start,
                    PayloadLength = (ushort)(colonEnd - start), // digits + colons
                };
                nextIndex = colonEnd + 1;
                return true;
            }
        }

        // Standard #t2000001#
        if (digitEnd >= span.Length || span[digitEnd] != '#')
            return false;

        token = new MapleTextToken
        {
            Kind = MapleTextTokenKind.EntityReference,
            Code = code,
            Start = (ushort)index,
            Length = (ushort)(digitEnd - index + 1),
            PayloadStart = (ushort)start,
            PayloadLength = (ushort)(digitEnd - start),
        };
        nextIndex = digitEnd + 1;
        return true;
    }

    private static void ParseLongFormToken(
        ReadOnlySpan<char> span,
        int index,
        out MapleTextToken token,
        out int nextIndex
    )
    {
        char code = span[index + 1];
        int start = index + 2;
        int end = start;
        while (end < span.Length)
        {
            char c = span[end];
            if (c is '#' or '\\' or '\r' or '\n')
                break;
            end++;
        }

        bool hasCloser = end < span.Length && span[end] == '#';
        int length = end - index + (hasCloser ? 1 : 0);

        token = new MapleTextToken
        {
            Kind = MapleTextTokenKind.ClientToken,
            Code = code,
            Start = (ushort)index,
            Length = (ushort)length,
            PayloadStart = (ushort)start,
            PayloadLength = (ushort)(end - start),
        };
        nextIndex = hasCloser ? end + 1 : end;
    }

    private static bool TryParseBlock(ReadOnlySpan<char> span, int index, out MapleTextToken token, out int nextIndex)
    {
        token = default;
        nextIndex = index;
        int searchFrom = index + 2;
        int closeOffset = span.Slice(searchFrom).IndexOf('#');
        if (closeOffset < 0)
            return false;
        int blockEnd = searchFrom + closeOffset;
        token = new MapleTextToken
        {
            Kind = MapleTextTokenKind.Block,
            Code = span[index + 1],
            Start = (ushort)index,
            Length = (ushort)(blockEnd - index + 1),
            PayloadStart = (ushort)(index + 2),
            PayloadLength = (ushort)(blockEnd - index - 2),
        };
        nextIndex = blockEnd + 1;
        return true;
    }
}
