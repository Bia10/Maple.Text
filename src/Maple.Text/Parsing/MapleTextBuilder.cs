using System.Buffers;

namespace Maple.Text.Parsing;

/// <summary>
/// Fluent builder for constructing well-formed MapleText strings.
/// Backed by an <see cref="ArrayPool{T}"/> rental — no <see cref="System.Text.StringBuilder"/>
/// allocation. Long values are formatted directly into the buffer via
/// <see cref="ISpanFormattable"/> with no intermediate string allocation.
/// Call <see cref="Build"/> exactly once; it returns the buffer to the pool.
/// </summary>
public sealed class MapleTextBuilder : IDisposable
{
    private const int InitialBufferSize = 128;
    private char[] _buf;
    private int _pos;
    private bool _built;

    public MapleTextBuilder()
    {
        _buf = ArrayPool<char>.Shared.Rent(InitialBufferSize);
        _pos = 0;
    }

    // ── plain text ────────────────────────────────────────────────────────────

    /// <summary>Appends a literal string. Null is silently ignored.</summary>
    public MapleTextBuilder Append(string text)
    {
        if (text is not null)
            Write(text.AsSpan());
        return this;
    }

    /// <summary>Appends a span of characters directly — no string allocation.</summary>
    public MapleTextBuilder Append(ReadOnlySpan<char> text)
    {
        Write(text);
        return this;
    }

    /// <summary>Appends a CRLF line break (\r\n).</summary>
    public MapleTextBuilder NewLine()
    {
        Write("\r\n");
        return this;
    }

    // ── style codes ───────────────────────────────────────────────────────────

    /// <summary>Wraps <paramref name="content"/> in a color open/close pair.</summary>
    public MapleTextBuilder Color(MapleTextColor color, string content)
    {
        if (content is null)
            return this;
        if (color == MapleTextColor.None)
        {
            Write(content.AsSpan());
            return this;
        }
        Write('#');
        Write(MapleTextTables.ColorToCode(color));
        Write(content.AsSpan());
        Write("#k");
        return this;
    }

    /// <inheritdoc cref="Color(MapleTextColor, string)"/>
    public MapleTextBuilder Color(MapleTextColor color, ReadOnlySpan<char> content)
    {
        if (content.IsEmpty)
            return this;
        if (color == MapleTextColor.None)
        {
            Write(content);
            return this;
        }
        Write('#');
        Write(MapleTextTables.ColorToCode(color));
        Write(content);
        Write("#k");
        return this;
    }

    /// <summary>Opens a color block without closing it.</summary>
    public MapleTextBuilder OpenColor(MapleTextColor color)
    {
        Write('#');
        Write(MapleTextTables.ColorToCode(color));
        return this;
    }

    /// <summary>Resets color (#k).</summary>
    public MapleTextBuilder ResetStyle()
    {
        Write("#k");
        return this;
    }

    /// <summary>Resets all styles including bold (#n).</summary>
    public MapleTextBuilder ResetAll()
    {
        Write("#n");
        return this;
    }

    /// <summary>
    /// Emits the opening style marker for <paramref name="style"/> without a closing marker.
    /// Use <see cref="ResetAll"/> to close a bold or small-font region.
    /// </summary>
    public MapleTextBuilder OpenStyle(MapleTextStyle style)
    {
        if (style == MapleTextStyle.None)
            return this;
        Write('#');
        Write(MapleTextTables.StyleToCode(style));
        return this;
    }

    /// <summary>
    /// Wraps <paramref name="content"/> in a style open/close pair.
    /// <list type="bullet">
    ///   <item><see cref="MapleTextStyle.Bold"/> — <c>#e</c>…<c>#n</c></item>
    ///   <item><see cref="MapleTextStyle.Small"/> — <c>#f</c>…<c>#n</c></item>
    ///   <item><see cref="MapleTextStyle.Normal"/> — emits only <c>#n</c> then content (reset, no open pair).</item>
    /// </list>
    /// </summary>
    public MapleTextBuilder Style(MapleTextStyle style, string content)
    {
        if (content is null)
            return this;
        return Style(style, content.AsSpan());
    }

    /// <inheritdoc cref="Style(MapleTextStyle, string)"/>
    public MapleTextBuilder Style(MapleTextStyle style, ReadOnlySpan<char> content)
    {
        if (content.IsEmpty)
            return this;
        if (style is MapleTextStyle.None or MapleTextStyle.Normal)
        {
            Write(content);
            return this;
        }
        Write('#');
        Write(MapleTextTables.StyleToCode(style));
        Write(content);
        Write("#n");
        return this;
    }

    /// <summary>Wraps <paramref name="content"/> in bold (#e…#n).</summary>
    public MapleTextBuilder Bold(string content)
    {
        if (content is null)
            return this;
        Write("#e");
        Write(content.AsSpan());
        Write("#n");
        return this;
    }

    /// <inheritdoc cref="Bold(string)"/>
    public MapleTextBuilder Bold(ReadOnlySpan<char> content)
    {
        if (content.IsEmpty)
            return this;
        Write("#e");
        Write(content);
        Write("#n");
        return this;
    }

    // ── entity references (#t #z #i #o #m) ───────────────────────────────────

    /// <summary>Appends an item-name reference: #t&lt;id&gt;#.</summary>
    public MapleTextBuilder ItemName(long id)
    {
        Write("#t");
        WriteLong(id);
        Write('#');
        return this;
    }

    /// <summary>Appends an alternate item-name reference: #z&lt;id&gt;#.</summary>
    public MapleTextBuilder ItemNameAlt(long id)
    {
        Write("#z");
        WriteLong(id);
        Write('#');
        return this;
    }

    /// <summary>Appends an item-icon reference: #i&lt;id&gt;#.</summary>
    public MapleTextBuilder ItemIcon(long id)
    {
        Write("#i");
        WriteLong(id);
        Write('#');
        return this;
    }

    /// <summary>Appends an item-icon-slot reference used in demand-summary display: #i&lt;id&gt;:#.</summary>
    public MapleTextBuilder ItemIconSlot(long id)
    {
        Write("#i");
        WriteLong(id);
        Write(":#");
        return this;
    }

    /// <summary>Appends a mob-name reference: #o&lt;id&gt;#.</summary>
    public MapleTextBuilder MobName(long id)
    {
        Write("#o");
        WriteLong(id);
        Write('#');
        return this;
    }

    /// <summary>Appends a map-name reference: #m&lt;id&gt;#.</summary>
    public MapleTextBuilder MapName(long id)
    {
        Write("#m");
        WriteLong(id);
        Write('#');
        return this;
    }

    // ── client tokens ─────────────────────────────────────────────────────────

    /// <summary>Appends an NPC-name client token: #p&lt;id&gt;.</summary>
    public MapleTextBuilder NpcName(long id)
    {
        Write("#p");
        WriteLong(id);
        return this;
    }

    /// <summary>Appends a character-name client token with optional josa suffix: #h&lt;josa&gt;.</summary>
    public MapleTextBuilder CharacterName(int josa = 0)
    {
        Write("#h");
        WriteLong(josa);
        return this;
    }

    /// <summary>Appends a labeled-NPC-string client token: #@&lt;id&gt;.</summary>
    public MapleTextBuilder LabeledNpcString(long id)
    {
        Write("#@");
        WriteLong(id);
        return this;
    }

    /// <summary>Appends a quest-mob-name client token: #M&lt;id&gt;.</summary>
    public MapleTextBuilder QuestMobName(long id)
    {
        Write("#M");
        WriteLong(id);
        return this;
    }

    /// <summary>Appends a quest mob-count client token: #a.</summary>
    public MapleTextBuilder QuestMobCount()
    {
        Write("#a");
        return this;
    }

    /// <summary>Appends a skill-name client token: #q&lt;id&gt;.</summary>
    public MapleTextBuilder SkillRef(long id)
    {
        Write("#q");
        WriteLong(id);
        return this;
    }

    /// <summary>Appends a quest-state client token: #u.</summary>
    public MapleTextBuilder QuestState()
    {
        Write("#u");
        return this;
    }

    /// <summary>Appends a reward-toggle client token: #w.</summary>
    public MapleTextBuilder RewardToggle()
    {
        Write("#w");
        return this;
    }

    /// <summary>
    /// Appends a canvas-load token: <c>#f&lt;path&gt;</c> (outline) or <c>#F&lt;path&gt;</c> (filled).
    /// The client renders the image at the WZ path <paramref name="path"/>. Null is silently ignored.
    /// </summary>
    public MapleTextBuilder CanvasLoad(string path, bool outline = false)
    {
        if (path is null)
            return this;
        Write(outline ? "#f" : "#F");
        Write(path.AsSpan());
        return this;
    }

    /// <summary>Appends a canvas-load token from a span — no string allocation.</summary>
    public MapleTextBuilder CanvasLoad(ReadOnlySpan<char> path, bool outline = false)
    {
        if (!path.IsEmpty)
        {
            Write(outline ? "#f" : "#F");
            Write(path);
        }
        return this;
    }

    /// <summary>
    /// Appends a gauge token: <c>#B&lt;path&gt;</c>.
    /// The client renders a progress bar using the image at the WZ path <paramref name="path"/>. Null is silently ignored.
    /// </summary>
    public MapleTextBuilder Gauge(string path)
    {
        if (path is null)
            return this;
        Write("#B");
        Write(path.AsSpan());
        return this;
    }

    /// <summary>Appends a gauge token from a span — no string allocation.</summary>
    public MapleTextBuilder Gauge(ReadOnlySpan<char> path)
    {
        if (!path.IsEmpty)
        {
            Write("#B");
            Write(path);
        }
        return this;
    }

    // ── stat tokens ───────────────────────────────────────────────────────────

    /// <summary>Appends a stat token: #&lt;statName&gt; (e.g. <c>#mpCon</c>).</summary>
    public MapleTextBuilder Stat(string statName)
    {
        if (statName is null)
            return this;
        Write('#');
        Write(statName.AsSpan());
        return this;
    }

    /// <summary>Appends a stat token directly from a span — no string allocation.</summary>
    public MapleTextBuilder Stat(ReadOnlySpan<char> statName)
    {
        if (!statName.IsEmpty)
        {
            Write('#');
            Write(statName);
        }
        return this;
    }

    // ── block tokens ──────────────────────────────────────────────────────────

    /// <summary>Appends a quest gauge block token: #j&lt;key&gt;#.</summary>
    public MapleTextBuilder QuestGauge(string key)
    {
        if (key is not null)
        {
            Write("#j");
            Write(key.AsSpan());
            Write('#');
        }
        return this;
    }

    /// <inheritdoc cref="QuestGauge(string)"/>
    public MapleTextBuilder QuestGauge(ReadOnlySpan<char> key)
    {
        if (!key.IsEmpty)
        {
            Write("#j");
            Write(key);
            Write('#');
        }
        return this;
    }

    /// <summary>Appends a quest timer block token: #Q&lt;key&gt;#.</summary>
    public MapleTextBuilder QuestTimer(string key)
    {
        if (key is not null)
        {
            Write("#Q");
            Write(key.AsSpan());
            Write('#');
        }
        return this;
    }

    /// <inheritdoc cref="QuestTimer(string)"/>
    public MapleTextBuilder QuestTimer(ReadOnlySpan<char> key)
    {
        if (!key.IsEmpty)
        {
            Write("#Q");
            Write(key);
            Write('#');
        }
        return this;
    }

    /// <summary>Appends a quest playtime block token: #D&lt;key&gt;#.</summary>
    public MapleTextBuilder QuestPlaytime(string key)
    {
        if (key is not null)
        {
            Write("#D");
            Write(key.AsSpan());
            Write('#');
        }
        return this;
    }

    /// <inheritdoc cref="QuestPlaytime(string)"/>
    public MapleTextBuilder QuestPlaytime(ReadOnlySpan<char> key)
    {
        if (!key.IsEmpty)
        {
            Write("#D");
            Write(key);
            Write('#');
        }
        return this;
    }

    /// <summary>Appends a quest summary-icon block token: #W&lt;name&gt;#.</summary>
    public MapleTextBuilder QuestSummaryIcon(string name)
    {
        if (name is not null)
        {
            Write("#W");
            Write(name.AsSpan());
            Write('#');
        }
        return this;
    }

    /// <inheritdoc cref="QuestSummaryIcon(string)"/>
    public MapleTextBuilder QuestSummaryIcon(ReadOnlySpan<char> name)
    {
        if (!name.IsEmpty)
        {
            Write("#W");
            Write(name);
            Write('#');
        }
        return this;
    }

    /// <summary>Appends a quest record block token: #R&lt;key&gt;#.</summary>
    public MapleTextBuilder QuestRecord(string key)
    {
        if (key is not null)
        {
            Write("#R");
            Write(key.AsSpan());
            Write('#');
        }
        return this;
    }

    /// <inheritdoc cref="QuestRecord(string)"/>
    public MapleTextBuilder QuestRecord(ReadOnlySpan<char> key)
    {
        if (!key.IsEmpty)
        {
            Write("#R");
            Write(key);
            Write('#');
        }
        return this;
    }

    // ── list / dialog entries ─────────────────────────────────────────────────

    /// <summary>Appends a list entry: #L&lt;index&gt;#&lt;text&gt;#l, optionally wrapped in a color.</summary>
    public MapleTextBuilder ListEntry(int index, string text, MapleTextColor color = MapleTextColor.None)
    {
        if (color != MapleTextColor.None)
        {
            Write('#');
            Write(MapleTextTables.ColorToCode(color));
        }
        Write("#L");
        WriteLong(index);
        Write('#');
        if (text is not null)
            Write(text.AsSpan());
        Write("#l");
        if (color != MapleTextColor.None)
            Write("#k");
        return this;
    }

    /// <inheritdoc cref="ListEntry(int, string, MapleTextColor)"/>
    public MapleTextBuilder ListEntry(int index, ReadOnlySpan<char> text, MapleTextColor color = MapleTextColor.None)
    {
        if (color != MapleTextColor.None)
        {
            Write('#');
            Write(MapleTextTables.ColorToCode(color));
        }
        Write("#L");
        WriteLong(index);
        Write('#');
        if (!text.IsEmpty)
            Write(text);
        Write("#l");
        if (color != MapleTextColor.None)
            Write("#k");
        return this;
    }

    // ── escape ────────────────────────────────────────────────────────────────

    /// <summary>Appends a literal hash character using the ## escape sequence.</summary>
    public MapleTextBuilder LiteralHash()
    {
        Write("##");
        return this;
    }

    // ── output ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Materialises the accumulated text into a new <see cref="string"/> and
    /// returns the internal buffer to <see cref="ArrayPool{T}"/>.
    /// The builder must not be used after calling <see cref="Build"/>.
    /// </summary>
    public string Build()
    {
        ObjectDisposedException.ThrowIf(_built, this);
        string result = new(_buf, 0, _pos);
        ArrayPool<char>.Shared.Return(_buf);
        _buf = [];
        _pos = 0;
        _built = true;
        return result;
    }

    /// <summary>
    /// Returns the accumulated text as a new string without finalising the builder.
    /// Unlike <see cref="Build"/>, this does not return the buffer to the pool and the builder
    /// remains usable. Use <see cref="Build"/> when you are done building.
    /// Throws <see cref="ObjectDisposedException"/> if called after <see cref="Build"/> or <see cref="Dispose"/>.
    /// </summary>
    public override string ToString()
    {
        ObjectDisposedException.ThrowIf(_built, this);
        return new string(_buf, 0, _pos);
    }

    /// <summary>
    /// Returns the internal buffer to <see cref="ArrayPool{T}"/> without materialising the string.
    /// Safe to call if <see cref="Build"/> was already called (no-op). Use <see cref="Build"/> instead
    /// when the result string is needed. Prefer <c>using var builder = new MapleTextBuilder();</c>
    /// to guarantee disposal when an exception may prevent <see cref="Build"/> from being reached.
    /// </summary>
    public void Dispose()
    {
        if (_built)
            return;
        if (_buf.Length > 0)
            ArrayPool<char>.Shared.Return(_buf);
        _buf = [];
        _pos = 0;
        _built = true;
    }

    // ── primitive write helpers ───────────────────────────────────────────────

    private void Write(char c)
    {
        EnsureCapacity(1);
        _buf[_pos++] = c;
    }

    private void Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return;
        EnsureCapacity(value.Length);
        value.CopyTo(_buf.AsSpan(_pos));
        _pos += value.Length;
    }

    private void WriteLong(long value)
    {
        EnsureCapacity(20); // max digits for Int64
        value.TryFormat(_buf.AsSpan(_pos), out int written, provider: CultureInfo.InvariantCulture);
        _pos += written;
    }

    private void WriteLong(int value) => WriteLong((long)value);

    private void EnsureCapacity(int needed)
    {
        ObjectDisposedException.ThrowIf(_built, this);
        if (_pos + needed <= _buf.Length)
            return;
        int newSize = Math.Max(_pos + needed, _buf.Length * 2);
        char[] newBuf = ArrayPool<char>.Shared.Rent(newSize);
        _buf.AsSpan(0, _pos).CopyTo(newBuf);
        ArrayPool<char>.Shared.Return(_buf);
        _buf = newBuf;
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    // ColorCode/StyleCode are now in MapleTextTables to be shared with the facade.
}
