namespace Maple.Text.Parsing;

/// <summary>
/// Static facade for MapleText parsing, decoding, encoding, and dialog construction.
/// All operations delegate to focused internal classes:
/// <list type="bullet">
/// <item><see cref="MapleTextParser"/> — tokenization</item>
/// <item><see cref="MapleTextStripper"/> — markup stripping and detection</item>
/// <item><see cref="MapleTextDecoder"/> — token-stream → plain-text decoding</item>
/// <item><see cref="MapleTextBuilder"/> — fluent string construction</item>
/// </list>
/// </summary>
public static class MapleText
{
    // =========================================================================
    // Parsing
    // =========================================================================

    /// <summary>Tokenises a raw MapleText string into its constituent parts.</summary>
    public static MapleTextParseResult Parse(string text) => MapleTextParser.Parse(text);

    /// <summary>
    /// Strips all MapleText markup and returns the plain-text content.
    /// Entity references remain absent from the output.
    /// Use <see cref="Decode"/> to resolve them.
    /// Uses a stack buffer for strings ≤512 chars; falls back to ArrayPool.
    /// The only heap allocation is the final string (or none when the input has no markup).
    /// </summary>
    public static string StripMarkup(string text) => MapleTextStripper.StripMarkup(text);

    /// <summary>
    /// Strips all MapleText markup from <paramref name="text"/> and returns the plain-text content.
    /// Always allocates a new string; prefer the <see cref="StripMarkup(string)"/> overload when the
    /// source is already a <see cref="string"/> to benefit from the no-markup fast-path.
    /// </summary>
    public static string StripMarkup(ReadOnlySpan<char> text) => MapleTextStripper.StripMarkup(text);

    /// <summary>Returns true when the string contains at least one '#' character.</summary>
    public static bool ContainsMarkup(string text) => MapleTextStripper.ContainsMarkup(text);

    /// <summary>Returns true when the span contains at least one '#' character.</summary>
    public static bool ContainsMarkup(ReadOnlySpan<char> text) => MapleTextStripper.ContainsMarkup(text);

    // =========================================================================
    // Decoding
    // =========================================================================

    /// <summary>
    /// Decodes a MapleText string into plain UTF-16 text using an optional resolver
    /// for entity references, stat tokens, client tokens, and block tokens.
    /// Output is accumulated into a <see cref="ValueStringBuilder"/> backed by a stack
    /// buffer; a single heap allocation produces the final string.
    /// <para>
    /// When <paramref name="resolver"/> is null:
    /// <list type="bullet">
    /// <item>Stat tokens are left as their raw form (e.g. "#mpCon").</item>
    /// <item>Entity references render as their value portion (e.g. "t:2000001") without allocating an intermediate string.</item>
    /// <item>Client tokens and block tokens produce no output.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static string Decode(string text, IMapleTextResolver? resolver = null) =>
        MapleTextDecoder.Decode(text, resolver);

    // =========================================================================
    // Encoding / Building
    // =========================================================================

    /// <summary>Returns a new fluent <see cref="MapleTextBuilder"/>.</summary>
    public static MapleTextBuilder Builder() => new();

    /// <summary>
    /// Wraps <paramref name="text"/> in bold (#e…#n) and returns the result as a new string.
    /// Uses <see cref="string.Create{TState}(int, TState, System.Buffers.SpanAction{char, TState})"/> — zero intermediate allocations.
    /// </summary>
    public static string Bold(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        // "#e" + text + "#n" = text.Length + 4 chars
        return string.Create(
            text.Length + 4,
            text,
            static (span, t) =>
            {
                span[0] = '#';
                span[1] = 'e';
                t.AsSpan().CopyTo(span[2..]);
                span[^2] = '#';
                span[^1] = 'n';
            }
        );
    }

    /// <summary>
    /// Wraps <paramref name="text"/> in a color open/close pair and returns the result as a new string.
    /// Returns <paramref name="text"/> unchanged when <paramref name="color"/> is <see cref="MapleTextColor.None"/>.
    /// Uses <see cref="string.Create{TState}(int, TState, System.Buffers.SpanAction{char, TState})"/> — zero intermediate allocations.
    /// </summary>
    public static string Colorize(MapleTextColor color, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (color == MapleTextColor.None)
            return text;
        char code = MapleTextTables.ColorToCode(color);
        // "#X" + text + "#k" = text.Length + 4 chars
        return string.Create(
            text.Length + 4,
            (code, text),
            static (span, s) =>
            {
                span[0] = '#';
                span[1] = s.code;
                s.text.AsSpan().CopyTo(span[2..]);
                span[^2] = '#';
                span[^1] = 'k';
            }
        );
    }

    /// <summary>
    /// Wraps <paramref name="text"/> in a style open/close pair and returns the result as a new string.
    /// Returns <paramref name="text"/> unchanged when <paramref name="style"/> is <see cref="MapleTextStyle.None"/> or <see cref="MapleTextStyle.Normal"/>.
    /// Uses <see cref="string.Create{TState}(int, TState, System.Buffers.SpanAction{char, TState})"/> — zero intermediate allocations.
    /// </summary>
    public static string Stylize(MapleTextStyle style, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (style is MapleTextStyle.None or MapleTextStyle.Normal)
            return text;
        char code = MapleTextTables.StyleToCode(style);
        // "#X" + text + "#n" = text.Length + 4 chars
        return string.Create(
            text.Length + 4,
            (code, text),
            static (span, s) =>
            {
                span[0] = '#';
                span[1] = s.code;
                s.text.AsSpan().CopyTo(span[2..]);
                span[^2] = '#';
                span[^1] = 'n';
            }
        );
    }

    /// <summary>
    /// Inserts a typed link token into a MapleText string.
    /// All cases use stackalloc to format the result — no heap allocation beyond the output string.
    /// </summary>
    public static string InsertLink(MapleTextLinkType linkType, long templateId, int josaSuffix = 0) =>
        linkType switch {
            MapleTextLinkType.ItemName => FormatEntityToken('t', templateId),
            MapleTextLinkType.ItemNameAlt => FormatEntityToken('z', templateId),
            MapleTextLinkType.ItemIcon => FormatEntityToken('i', templateId),
            MapleTextLinkType.ItemIconSlot => FormatItemIconSlot(templateId),
            MapleTextLinkType.MobName => FormatEntityToken('o', templateId),
            MapleTextLinkType.MapName => FormatEntityToken('m', templateId),
            MapleTextLinkType.NpcName => FormatClientToken('p', templateId),
            MapleTextLinkType.CharacterName => FormatCharacterName(josaSuffix),
            MapleTextLinkType.SkillName => FormatClientToken('q', templateId),
            MapleTextLinkType.QuestMobName => FormatClientToken('M', templateId),
            MapleTextLinkType.LabeledNpcString => FormatClientToken('@', templateId),
            _ => throw new ArgumentOutOfRangeException(nameof(linkType), linkType, null),
        };

    // ── stackalloc helpers for InsertLink — no ArrayPool round-trip ───────────

    // '#' + code + up to 20 digits + '#' = 22 chars max
    private static string FormatEntityToken(char code, long id)
    {
        Span<char> buf = stackalloc char[22];
        buf[0] = '#';
        buf[1] = code;
        id.TryFormat(buf[2..], out int w, provider: CultureInfo.InvariantCulture);
        buf[2 + w] = '#';
        return new string(buf[..(3 + w)]);
    }

    // '#' + 'i' + up to 20 digits + ':' + '#' = 24 chars max
    private static string FormatItemIconSlot(long id)
    {
        Span<char> buf = stackalloc char[24];
        buf[0] = '#';
        buf[1] = 'i';
        id.TryFormat(buf[2..], out int w, provider: CultureInfo.InvariantCulture);
        buf[2 + w] = ':';
        buf[3 + w] = '#';
        return new string(buf[..(4 + w)]);
    }

    // '#' + code + up to 20 digits = 22 chars max (no trailing '#')
    private static string FormatClientToken(char code, long id)
    {
        Span<char> buf = stackalloc char[22];
        buf[0] = '#';
        buf[1] = code;
        id.TryFormat(buf[2..], out int w, provider: CultureInfo.InvariantCulture);
        return new string(buf[..(2 + w)]);
    }

    // '#' + 'h' + josa digit(s) = up to 6 chars
    private static string FormatCharacterName(int josaSuffix)
    {
        Span<char> buf = stackalloc char[6];
        buf[0] = '#';
        buf[1] = 'h';
        josaSuffix.TryFormat(buf[2..], out int w, provider: CultureInfo.InvariantCulture);
        return new string(buf[..(2 + w)]);
    }

    // =========================================================================
    // Dialog builders
    // =========================================================================

    /// <summary>
    /// Builds a Yes/No selection dialog string.
    /// <para>
    /// Produces: &lt;question&gt;\r\n#b#L0#&lt;yesText&gt;#l#k\n#L1#&lt;noText&gt;#l
    /// </para>
    /// </summary>
    public static string InvokeYesNoDialog(string question, string yesText = "Yes", string noText = "No")
    {
        ArgumentNullException.ThrowIfNull(question);
        using MapleTextBuilder builder = Builder();
        return builder
            .Append(question)
            .NewLine()
            .ListEntry(0, yesText, MapleTextColor.Blue)
            .NewLine()
            .ListEntry(1, noText)
            .Build();
    }

    /// <summary>
    /// Returns <paramref name="message"/> unchanged after a null guard.
    /// Exists for API symmetry with the other <c>Invoke*Dialog</c> builders — an OK
    /// dialog is plain text with no special markup.
    /// </summary>
    public static string InvokeOkDialog(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message;
    }

    /// <summary>
    /// Builds a selection dialog with an arbitrary number of options.
    /// When called with a fixed argument list the <see langword="params"/>
    /// <see cref="ReadOnlySpan{T}"/> overload avoids a heap array allocation.
    /// <para>
    /// Options are rendered as #L0# … #l, #L1# … #l, etc., each on a new line.
    /// </para>
    /// </summary>
    public static string InvokeSelectDialog(string question, params ReadOnlySpan<string> options)
    {
        ArgumentNullException.ThrowIfNull(question);

        using MapleTextBuilder builder = Builder();
        builder.Append(question);
        for (int i = 0; i < options.Length; i++)
            builder.NewLine().ListEntry(i, options[i], MapleTextColor.Blue);

        return builder.Build();
    }

    /// <summary>
    /// Builds a selection dialog from an integer-keyed entry collection.
    /// Use this overload when selection indices are not contiguous or are assigned by the caller
    /// (e.g. server menu dictionaries with negative sentinel keys for navigation).
    /// <para>
    /// Each entry is rendered as <c>#L{key}#{value}#l</c>, preceded by a newline.
    /// The iteration order of <paramref name="entries"/> determines the display order.
    /// </para>
    /// <para>
    /// Replaces the common server-side pattern:
    /// <code>
    /// text + "\r\n#b" + string.Join("\r\n", dict.Select(p => "#L" + p.Key + "#" + p.Value + "#l"))
    /// </code>
    /// </para>
    /// </summary>
    public static string InvokeMenuDialog(
        string question,
        IEnumerable<KeyValuePair<int, string>> entries,
        MapleTextColor color = MapleTextColor.Blue
    )
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(entries);

        using MapleTextBuilder builder = Builder();
        builder.Append(question);
        foreach (KeyValuePair<int, string> entry in entries)
            builder.NewLine().ListEntry(entry.Key, entry.Value, color);

        return builder.Build();
    }
}
