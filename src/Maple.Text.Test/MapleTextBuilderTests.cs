using Maple.Text.Parsing;

namespace Maple.Text.Test;

public sealed class MapleTextBuilderTests
{
    // ── Append / NewLine ──────────────────────────────────────────────────────

    [Test]
    public async Task Build_EmptyBuilder_ReturnsEmptyString()
    {
        using var builder = new MapleTextBuilder();
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Append_String_AppendsText()
    {
        using var builder = new MapleTextBuilder();
        builder.Append("Hello");
        await Assert.That(builder.Build()).IsEqualTo("Hello");
    }

    [Test]
    public async Task Append_NullString_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Append("A").Append((string)null!).Append("B");
        await Assert.That(builder.Build()).IsEqualTo("AB");
    }

    [Test]
    public async Task Append_Span_AppendsText()
    {
        using var builder = new MapleTextBuilder();
        builder.Append("Hello".AsSpan());
        await Assert.That(builder.Build()).IsEqualTo("Hello");
    }

    [Test]
    public async Task NewLine_AppendsCrLf()
    {
        using var builder = new MapleTextBuilder();
        builder.Append("A").NewLine().Append("B");
        await Assert.That(builder.Build()).IsEqualTo("A\r\nB");
    }

    // ── Bold ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task Bold_String_WrapsBoldMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Bold("Hello");
        await Assert.That(builder.Build()).IsEqualTo("#eHello#n");
    }

    [Test]
    public async Task Bold_NullString_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Bold((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Bold_EmptySpan_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Bold(ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── Color ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Color_Blue_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Blue, "text");
        await Assert.That(builder.Build()).IsEqualTo("#btext#k");
    }

    [Test]
    public async Task Color_Red_WrapsColorMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Red, "text");
        await Assert.That(builder.Build()).IsEqualTo("#rtext#k");
    }

    [Test]
    public async Task Color_None_NoWrapping()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.None, "text");
        await Assert.That(builder.Build()).IsEqualTo("text");
    }

    [Test]
    public async Task Color_NullString_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Blue, (string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Color_Span_EmptySpan_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Color(MapleTextColor.Blue, ReadOnlySpan<char>.Empty);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task OpenColor_EmitsOpenMarkerOnly()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenColor(MapleTextColor.Red).Append("text").ResetStyle();
        await Assert.That(builder.Build()).IsEqualTo("#rtext#k");
    }

    [Test]
    public async Task ResetStyle_EmitsHashK()
    {
        using var builder = new MapleTextBuilder();
        builder.ResetStyle();
        await Assert.That(builder.Build()).IsEqualTo("#k");
    }

    [Test]
    public async Task ResetAll_EmitsHashN()
    {
        using var builder = new MapleTextBuilder();
        builder.ResetAll();
        await Assert.That(builder.Build()).IsEqualTo("#n");
    }

    // ── Style ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Style_Bold_WrapsBoldMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Bold, "text");
        await Assert.That(builder.Build()).IsEqualTo("#etext#n");
    }

    [Test]
    public async Task Style_Small_WrapsSmallMarkers()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Small, "text");
        await Assert.That(builder.Build()).IsEqualTo("#ftext#n");
    }

    [Test]
    public async Task Style_None_NoWrapping()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.None, "text");
        await Assert.That(builder.Build()).IsEqualTo("text");
    }

    [Test]
    public async Task Style_Normal_NoWrapping()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Normal, "text");
        await Assert.That(builder.Build()).IsEqualTo("text");
    }

    [Test]
    public async Task OpenStyle_None_NoOutput()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenStyle(MapleTextStyle.None);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task OpenStyle_Bold_EmitsHashE()
    {
        using var builder = new MapleTextBuilder();
        builder.OpenStyle(MapleTextStyle.Bold);
        await Assert.That(builder.Build()).IsEqualTo("#e");
    }

    // ── Entity references ─────────────────────────────────────────────────────

    [Test]
    public async Task ItemName_ProducesCorrectToken()
    {
        using var builder = new MapleTextBuilder();
        builder.ItemName(2000001L);
        await Assert.That(builder.Build()).IsEqualTo("#t2000001#");
    }

    [Test]
    public async Task ItemNameAlt_ProducesCorrectToken()
    {
        using var builder = new MapleTextBuilder();
        builder.ItemNameAlt(2000001L);
        await Assert.That(builder.Build()).IsEqualTo("#z2000001#");
    }

    [Test]
    public async Task ItemIcon_ProducesCorrectToken()
    {
        using var builder = new MapleTextBuilder();
        builder.ItemIcon(4000001L);
        await Assert.That(builder.Build()).IsEqualTo("#i4000001#");
    }

    [Test]
    public async Task ItemIconSlot_ProducesColonVariant()
    {
        using var builder = new MapleTextBuilder();
        builder.ItemIconSlot(4000001L);
        await Assert.That(builder.Build()).IsEqualTo("#i4000001:#");
    }

    [Test]
    public async Task MobName_ProducesCorrectToken()
    {
        using var builder = new MapleTextBuilder();
        builder.MobName(100100L);
        await Assert.That(builder.Build()).IsEqualTo("#o100100#");
    }

    [Test]
    public async Task MapName_ProducesCorrectToken()
    {
        using var builder = new MapleTextBuilder();
        builder.MapName(100000000L);
        await Assert.That(builder.Build()).IsEqualTo("#m100000000#");
    }

    // ── Client tokens ─────────────────────────────────────────────────────────

    [Test]
    public async Task NpcName_ProducesNoClosingHash()
    {
        using var builder = new MapleTextBuilder();
        builder.NpcName(9000001L);
        await Assert.That(builder.Build()).IsEqualTo("#p9000001");
    }

    [Test]
    public async Task CharacterName_DefaultJosa_ProducesHashH0()
    {
        using var builder = new MapleTextBuilder();
        builder.CharacterName();
        await Assert.That(builder.Build()).IsEqualTo("#h0");
    }

    [Test]
    public async Task CharacterName_CustomJosa_ProducesCorrectSuffix()
    {
        using var builder = new MapleTextBuilder();
        builder.CharacterName(1);
        await Assert.That(builder.Build()).IsEqualTo("#h1");
    }

    [Test]
    public async Task SkillRef_ProducesQToken()
    {
        using var builder = new MapleTextBuilder();
        builder.SkillRef(1001L);
        await Assert.That(builder.Build()).IsEqualTo("#q1001");
    }

    [Test]
    public async Task QuestState_ProducesHashU()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestState();
        await Assert.That(builder.Build()).IsEqualTo("#u");
    }

    [Test]
    public async Task QuestMobCount_ProducesHashA()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestMobCount();
        await Assert.That(builder.Build()).IsEqualTo("#a");
    }

    [Test]
    public async Task RewardToggle_ProducesHashW()
    {
        using var builder = new MapleTextBuilder();
        builder.RewardToggle();
        await Assert.That(builder.Build()).IsEqualTo("#w");
    }

    [Test]
    public async Task QuestMobName_ProducesMUpperToken()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestMobName(100100L);
        await Assert.That(builder.Build()).IsEqualTo("#M100100");
    }

    [Test]
    public async Task LabeledNpcString_ProducesAtToken()
    {
        using var builder = new MapleTextBuilder();
        builder.LabeledNpcString(1001L);
        await Assert.That(builder.Build()).IsEqualTo("#@1001");
    }

    [Test]
    public async Task CanvasLoad_Filled_ProducesHashFUpper()
    {
        using var builder = new MapleTextBuilder();
        builder.CanvasLoad("UI/Gauge.img");
        await Assert.That(builder.Build()).IsEqualTo("#FUI/Gauge.img");
    }

    [Test]
    public async Task CanvasLoad_Outline_ProducesHashFLower()
    {
        using var builder = new MapleTextBuilder();
        builder.CanvasLoad("UI/Gauge.img", outline: true);
        await Assert.That(builder.Build()).IsEqualTo("#fUI/Gauge.img");
    }

    [Test]
    public async Task CanvasLoad_NullPath_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.CanvasLoad((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Gauge_ProducesHashBPath()
    {
        using var builder = new MapleTextBuilder();
        builder.Gauge("UI/GaugeBar.img");
        await Assert.That(builder.Build()).IsEqualTo("#BUI/GaugeBar.img");
    }

    [Test]
    public async Task Gauge_NullPath_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Gauge((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── Stat token ────────────────────────────────────────────────────────────

    [Test]
    public async Task Stat_ProducesHashStatName()
    {
        using var builder = new MapleTextBuilder();
        builder.Stat("mpCon");
        await Assert.That(builder.Build()).IsEqualTo("#mpCon");
    }

    [Test]
    public async Task Stat_NullName_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Stat((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── Block tokens ──────────────────────────────────────────────────────────

    [Test]
    public async Task QuestGauge_ProducesHashJKeyHash()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestGauge("killCount");
        await Assert.That(builder.Build()).IsEqualTo("#jkillCount#");
    }

    [Test]
    public async Task QuestTimer_ProducesHashQKeyHash()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestTimer("timer1");
        await Assert.That(builder.Build()).IsEqualTo("#Qtimer1#");
    }

    [Test]
    public async Task QuestPlaytime_ProducesHashDKeyHash()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestPlaytime("playtime");
        await Assert.That(builder.Build()).IsEqualTo("#Dplaytime#");
    }

    [Test]
    public async Task QuestRecord_ProducesHashRKeyHash()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestRecord("record1");
        await Assert.That(builder.Build()).IsEqualTo("#Rrecord1#");
    }

    [Test]
    public async Task QuestSummaryIcon_ProducesHashWNameHash()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestSummaryIcon("icon1");
        await Assert.That(builder.Build()).IsEqualTo("#Wicon1#");
    }

    [Test]
    public async Task QuestGauge_NullKey_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestGauge((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task QuestTimer_NullKey_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.QuestTimer((string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }

    // ── List entries ──────────────────────────────────────────────────────────

    [Test]
    public async Task ListEntry_NoColor_ProducesListStructure()
    {
        using var builder = new MapleTextBuilder();
        builder.ListEntry(0, "Option A");
        await Assert.That(builder.Build()).IsEqualTo("#L0#Option A#l");
    }

    [Test]
    public async Task ListEntry_WithColor_WrapsInColor()
    {
        using var builder = new MapleTextBuilder();
        builder.ListEntry(1, "Option B", MapleTextColor.Blue);
        await Assert.That(builder.Build()).IsEqualTo("#b#L1#Option B#l#k");
    }

    // ── LiteralHash ───────────────────────────────────────────────────────────

    [Test]
    public async Task LiteralHash_ProducesDoubleHash()
    {
        using var builder = new MapleTextBuilder();
        builder.Append("100").LiteralHash();
        await Assert.That(builder.Build()).IsEqualTo("100##");
    }

    // ── Build / Dispose semantics ─────────────────────────────────────────────

    [Test]
    public async Task Build_CalledTwice_ThrowsObjectDisposedException()
    {
        var builder = new MapleTextBuilder();
        _ = builder.Build();
        await Assert.That(() => builder.Build()).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Dispose_BeforeBuild_ThrowsOnBuild()
    {
        var builder = new MapleTextBuilder();
        builder.Append("Hello");
        builder.Dispose();
        await Assert.That(() => builder.Build()).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task ToString_DoesNotFinalizeBuilder()
    {
        var builder = new MapleTextBuilder();
        builder.Append("Hello");
        string preview = builder.ToString();
        builder.Append(" World");
        string final = builder.Build();
        await Assert.That(preview).IsEqualTo("Hello");
        await Assert.That(final).IsEqualTo("Hello World");
    }

    [Test]
    public async Task FluentChain_ProducesCorrectOutput()
    {
        using var builder = new MapleTextBuilder();
        string result = builder.Bold("Warning").Append(": spend ").Stat("mpCon").Append(" MP").Build();
        await Assert.That(result).IsEqualTo("#eWarning#n: spend #mpCon MP");
    }

    [Test]
    public async Task BufferGrowth_LongAppend_ProducesCorrectOutput()
    {
        // Default buffer is 128 chars — force multiple resizes.
        string longText = new('A', 400);
        using var builder = new MapleTextBuilder();
        builder.Append(longText);
        await Assert.That(builder.Build()).IsEqualTo(longText);
    }

    // ── Write(ReadOnlySpan<char>) empty guard ─────────────────────────────────

    [Test]
    public async Task Append_EmptyString_IsIgnored()
    {
        // Append("") calls Write("".AsSpan()), taking the IsEmpty early-return in Write.
        using var builder = new MapleTextBuilder();
        builder.Append("A").Append("").Append("B");
        await Assert.That(builder.Build()).IsEqualTo("AB");
    }

    // ── MapleTextTables.ColorToCode default case ──────────────────────────────

    [Test]
    public async Task OpenColor_UnknownEnumValue_FallsBackToDefaultK()
    {
        // ColorToCode(_) default arm returns 'k' for any unlisted MapleTextColor value.
        using var builder = new MapleTextBuilder();
        builder.OpenColor((MapleTextColor)255);
        await Assert.That(builder.Build()).IsEqualTo("#k");
    }

    // ── Style(MapleTextStyle, string) null guard ──────────────────────────────

    [Test]
    public async Task Style_NullString_IsIgnored()
    {
        using var builder = new MapleTextBuilder();
        builder.Style(MapleTextStyle.Bold, (string)null!);
        await Assert.That(builder.Build()).IsEqualTo(string.Empty);
    }
}
