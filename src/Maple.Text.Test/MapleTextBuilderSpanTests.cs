using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Tests for span overloads and missing method variants on <see cref="MapleTextBuilder"/>,
/// plus untested color/style codes in <see cref="MapleTextTables"/>.
/// </summary>
public sealed class MapleTextBuilderSpanTests
{
    // ── Color span overload ───────────────────────────────────────────────────

    [Test]
    public async Task Color_Span_NonEmpty_WithColor_WrapsCorrectly()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Blue, "text".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#btext#k");
    }

    [Test]
    public async Task Color_Span_None_WritesContentDirectly()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.None, "text".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("text");
    }

    // ── Style span overload ───────────────────────────────────────────────────

    [Test]
    public async Task Style_Span_Bold_WrapsBoldMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Bold, "text".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#etext#n");
    }

    [Test]
    public async Task Style_Span_Small_WrapsSmallMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Small, "text".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#ftext#n");
    }

    [Test]
    public async Task Style_Span_None_EmitsContentOnly()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.None, "text".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("text");
    }

    [Test]
    public async Task Style_Span_Normal_EmitsContentOnly()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Normal, "text".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("text");
    }

    // ── Bold span overload ────────────────────────────────────────────────────

    [Test]
    public async Task Bold_Span_NonEmpty_WrapsBoldMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Bold("Alert".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#eAlert#n");
    }

    // ── OpenStyle — Normal and Small ─────────────────────────────────────────

    [Test]
    public async Task OpenStyle_Normal_EmitsHashN()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenStyle(MapleTextStyle.Normal);
        await Assert.That(builder.Build()).IsEqualTo("#n");
    }

    [Test]
    public async Task OpenStyle_Small_EmitsHashF()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenStyle(MapleTextStyle.Small);
        await Assert.That(builder.Build()).IsEqualTo("#f");
    }

    // ── CanvasLoad span overload ──────────────────────────────────────────────

    [Test]
    public async Task CanvasLoad_Span_Filled_EmitsUpperF()
    {
        using var builder = new MapleTextBuilder();
        builder.CanvasLoad("UI/Gauge.img".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#FUI/Gauge.img");
    }

    [Test]
    public async Task CanvasLoad_Span_Outline_EmitsLowerF()
    {
        using var builder = new MapleTextBuilder();
        builder.CanvasLoad("UI/Gauge.img".AsSpan(), outline: true);
        await Assert.That(builder.Build()).IsEqualTo("#fUI/Gauge.img");
    }

    [Test]
    public async Task CanvasLoad_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.CanvasLoad(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── Gauge span overload ───────────────────────────────────────────────────

    [Test]
    public async Task Gauge_Span_EmitsHashBPath()
    {
        using var builder = new MapleTextBuilder();
        builder.Gauge("UI/GaugeBar.img".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#BUI/GaugeBar.img");
    }

    [Test]
    public async Task Gauge_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Gauge(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── Stat span overload ────────────────────────────────────────────────────

    [Test]
    public async Task Stat_Span_EmitsHashStatName()
    {
        using var builder = new MapleTextBuilder();
        builder.Stat("pad".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#pad");
    }

    [Test]
    public async Task Stat_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Stat(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── QuestGauge span overload ──────────────────────────────────────────────

    [Test]
    public async Task QuestGauge_Span_EmitsToken()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestGauge("killCount".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#jkillCount#");
    }

    [Test]
    public async Task QuestGauge_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestGauge(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── QuestTimer span overload ──────────────────────────────────────────────

    [Test]
    public async Task QuestTimer_Span_EmitsToken()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestTimer("timer1".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#Qtimer1#");
    }

    [Test]
    public async Task QuestTimer_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestTimer(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── QuestPlaytime string (null) and span overloads ────────────────────────

    [Test]
    public async Task QuestPlaytime_NullKey_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestPlaytime((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task QuestPlaytime_Span_EmitsToken()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestPlaytime("playtime".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#Dplaytime#");
    }

    [Test]
    public async Task QuestPlaytime_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestPlaytime(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── QuestRecord string (null) and span overloads ──────────────────────────

    [Test]
    public async Task QuestRecord_NullKey_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestRecord((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task QuestRecord_Span_EmitsToken()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestRecord("rec1".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#Rrec1#");
    }

    [Test]
    public async Task QuestRecord_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestRecord(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── QuestSummaryIcon string (null) and span overloads ─────────────────────

    [Test]
    public async Task QuestSummaryIcon_NullName_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestSummaryIcon((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task QuestSummaryIcon_Span_EmitsToken()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestSummaryIcon("icon1".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#Wicon1#");
    }

    [Test]
    public async Task QuestSummaryIcon_Span_Empty_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestSummaryIcon(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── ListEntry span overload ───────────────────────────────────────────────

    [Test]
    public async Task ListEntry_Span_NoColor_ProducesListStructure()
    {
        using var builder = new MapleTextBuilder();
        builder.ListEntry(0, "Option A".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("#L0#Option A#l");
    }

    [Test]
    public async Task ListEntry_Span_WithColor_WrapsInColor()
    {
        using var builder = new MapleTextBuilder();
        builder.ListEntry(1, "Option B".AsSpan(), MapleTextColor.Blue);
        await Assert.That(builder.Build()).IsEqualTo("#b#L1#Option B#l#k");
    }

    [Test]
    public async Task ListEntry_Span_EmptyText_ProducesEmptySlot()
    {
        using var builder = new MapleTextBuilder();
        builder.ListEntry(0, ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo("#L0##l");
    }

    // ── MapleTextTables: untested color codes ─────────────────────────────────

    [Test]
    public async Task Color_Cyan_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Cyan, "text");
        await Assert.That(builder.Build()).IsEqualTo("#ctext#k");
    }

    [Test]
    public async Task Color_Dark_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Dark, "text");
        await Assert.That(builder.Build()).IsEqualTo("#dtext#k");
    }

    [Test]
    public async Task Color_Gray_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Gray, "text");
        await Assert.That(builder.Build()).IsEqualTo("#gtext#k");
    }

    [Test]
    public async Task Color_Sky_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Sky, "text");
        await Assert.That(builder.Build()).IsEqualTo("#stext#k");
    }

    [Test]
    public async Task Color_Black_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Black, "text");
        await Assert.That(builder.Build()).IsEqualTo("#ktext#k");
    }

    // ── OpenColor for all untested colors ─────────────────────────────────────

    [Test]
    public async Task OpenColor_Cyan_EmitsCyan()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenColor(MapleTextColor.Cyan);
        await Assert.That(builder.Build()).IsEqualTo("#c");
    }

    [Test]
    public async Task OpenColor_Dark_EmitsDark()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenColor(MapleTextColor.Dark);
        await Assert.That(builder.Build()).IsEqualTo("#d");
    }

    [Test]
    public async Task OpenColor_Gray_EmitsGray()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenColor(MapleTextColor.Gray);
        await Assert.That(builder.Build()).IsEqualTo("#g");
    }

    [Test]
    public async Task OpenColor_Sky_EmitsSky()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenColor(MapleTextColor.Sky);
        await Assert.That(builder.Build()).IsEqualTo("#s");
    }

    [Test]
    public async Task OpenColor_Black_EmitsBlack()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenColor(MapleTextColor.Black);
        await Assert.That(builder.Build()).IsEqualTo("#k");
    }

    // ── MapleTextTables: StyleToCode Normal ───────────────────────────────────

    [Test]
    public async Task OpenStyle_Normal_EmitsHashN_ViaStyleToCode()
    {
        // OpenStyle(Normal) → Write('#') + Write(StyleToCode(Normal)) = '#n'.
        // This is the only path that calls StyleToCode(Normal).
        using var builder = new MapleTextBuilder();
        builder.OpenStyle(MapleTextStyle.Normal).Append("text");
        await Assert.That(builder.Build()).IsEqualTo("#ntext");
    }

    // ── Style(MapleTextStyle, ReadOnlySpan<char>) empty guard ────────────────

    [Test]
    public async Task Style_EmptySpan_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Bold, ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── MapleTextTables.StyleToCode default case ──────────────────────────────

    [Test]
    public async Task OpenStyle_UnknownEnumValue_FallsBackToDefaultN()
    {
        // StyleToCode(_) default arm returns 'n' for any unlisted MapleTextStyle value.
        using var builder = new MapleTextBuilder();
        builder.OpenStyle((MapleTextStyle)255);
        await Assert.That(builder.Build()).IsEqualTo("#n");
    }
}
