using Maple.Text.Parsing;

namespace Maple.Text.Test;

public sealed class MapleTextFacadeTests
{
    // ── Parse / StripMarkup / ContainsMarkup ──────────────────────────────────

    [Test]
    public async Task Parse_DelegatesToParser()
    {
        var result = MapleText.Parse("#bHello#k");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).IsNotEmpty();
    }

    [Test]
    public async Task StripMarkup_String_DelegatesToStripper()
    {
        await Assert.That(MapleText.StripMarkup("#bHello#k")).IsEqualTo("Hello");
    }

    [Test]
    public async Task StripMarkup_Span_DelegatesToStripper()
    {
        await Assert.That(MapleText.StripMarkup("#bHello#k".AsSpan())).IsEqualTo("Hello");
    }

    [Test]
    public async Task ContainsMarkup_String_TrueWhenHashPresent()
    {
        await Assert.That(MapleText.ContainsMarkup("#bHello#k")).IsTrue();
    }

    [Test]
    public async Task ContainsMarkup_String_FalseWhenNoHash()
    {
        await Assert.That(MapleText.ContainsMarkup("Hello World")).IsFalse();
    }

    [Test]
    public async Task ContainsMarkup_Span_TrueWhenHashPresent()
    {
        await Assert.That(MapleText.ContainsMarkup("#b".AsSpan())).IsTrue();
    }

    [Test]
    public async Task ContainsMarkup_Span_FalseForEmptySpan()
    {
        await Assert.That(MapleText.ContainsMarkup(ReadOnlySpan<char>.Empty)).IsFalse();
    }

    [Test]
    public async Task Decode_DelegatesToDecoder()
    {
        await Assert.That(MapleText.Decode("#bHello#k")).IsEqualTo("Hello");
    }

    [Test]
    public async Task Builder_ReturnsNewInstance()
    {
        using var b1 = MapleText.Builder();
        using var b2 = MapleText.Builder();
        await Assert.That(b1).IsNotSameReferenceAs(b2);
    }

    // ── Bold ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task Bold_WrapsInBoldMarkers()
    {
        await Assert.That(MapleText.Bold("Hello")).IsEqualTo("#eHello#n");
    }

    [Test]
    public async Task Bold_EmptyString_WrapsMarkers()
    {
        await Assert.That(MapleText.Bold(string.Empty)).IsEqualTo("#e#n");
    }

    [Test]
    public async Task Bold_Null_Throws()
    {
        await Assert.That(() => MapleText.Bold(null!)).Throws<ArgumentNullException>();
    }

    // ── Colorize ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Colorize_Blue_WrapsInBlueMarkers()
    {
        await Assert.That(MapleText.Colorize(MapleTextColor.Blue, "Hello")).IsEqualTo("#bHello#k");
    }

    [Test]
    public async Task Colorize_Red_WrapsInRedMarkers()
    {
        await Assert.That(MapleText.Colorize(MapleTextColor.Red, "Hello")).IsEqualTo("#rHello#k");
    }

    [Test]
    public async Task Colorize_None_ReturnsSameInstance()
    {
        const string input = "Hello";
        await Assert.That(MapleText.Colorize(MapleTextColor.None, input)).IsSameReferenceAs(input);
    }

    [Test]
    public async Task Colorize_Null_Throws()
    {
        await Assert.That(() => MapleText.Colorize(MapleTextColor.Blue, null!)).Throws<ArgumentNullException>();
    }

    // ── Stylize ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Stylize_Bold_WrapsInBoldMarkers()
    {
        await Assert.That(MapleText.Stylize(MapleTextStyle.Bold, "Hello")).IsEqualTo("#eHello#n");
    }

    [Test]
    public async Task Stylize_Small_WrapsInSmallMarkers()
    {
        await Assert.That(MapleText.Stylize(MapleTextStyle.Small, "Hello")).IsEqualTo("#fHello#n");
    }

    [Test]
    public async Task Stylize_None_ReturnsSameInstance()
    {
        const string input = "Hello";
        await Assert.That(MapleText.Stylize(MapleTextStyle.None, input)).IsSameReferenceAs(input);
    }

    [Test]
    public async Task Stylize_Normal_ReturnsSameInstance()
    {
        const string input = "Hello";
        await Assert.That(MapleText.Stylize(MapleTextStyle.Normal, input)).IsSameReferenceAs(input);
    }

    [Test]
    public async Task Stylize_Null_Throws()
    {
        await Assert.That(() => MapleText.Stylize(MapleTextStyle.Bold, null!)).Throws<ArgumentNullException>();
    }

    // ── InsertLink ────────────────────────────────────────────────────────────

    [Test]
    public async Task InsertLink_ItemName_ProducesEntityToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.ItemName, 2000001)).IsEqualTo("#t2000001#");
    }

    [Test]
    public async Task InsertLink_ItemNameAlt_ProducesZToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.ItemNameAlt, 2000001)).IsEqualTo("#z2000001#");
    }

    [Test]
    public async Task InsertLink_ItemIcon_ProducesIToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.ItemIcon, 4000001)).IsEqualTo("#i4000001#");
    }

    [Test]
    public async Task InsertLink_ItemIconSlot_ProducesColonVariant()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.ItemIconSlot, 4000001)).IsEqualTo("#i4000001:#");
    }

    [Test]
    public async Task InsertLink_MobName_ProducesOToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.MobName, 100100)).IsEqualTo("#o100100#");
    }

    [Test]
    public async Task InsertLink_MapName_ProducesMToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.MapName, 100000000)).IsEqualTo("#m100000000#");
    }

    [Test]
    public async Task InsertLink_NpcName_ProducesPToken_NoClosingHash()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.NpcName, 9000001)).IsEqualTo("#p9000001");
    }

    [Test]
    public async Task InsertLink_SkillName_ProducesQToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.SkillName, 1001)).IsEqualTo("#q1001");
    }

    [Test]
    public async Task InsertLink_QuestMobName_ProducesMUpperToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.QuestMobName, 100100)).IsEqualTo("#M100100");
    }

    [Test]
    public async Task InsertLink_LabeledNpcString_ProducesAtToken()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.LabeledNpcString, 1001)).IsEqualTo("#@1001");
    }

    [Test]
    public async Task InsertLink_CharacterName_DefaultJosa_ProducesH0()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.CharacterName, 0, josaSuffix: 0)).IsEqualTo("#h0");
    }

    [Test]
    public async Task InsertLink_CharacterName_JosaSuffix1_ProducesH1()
    {
        await Assert.That(MapleText.InsertLink(MapleTextLinkType.CharacterName, 0, josaSuffix: 1)).IsEqualTo("#h1");
    }

    [Test]
    public async Task InsertLink_InvalidLinkType_Throws()
    {
        await Assert.That(() => MapleText.InsertLink((MapleTextLinkType)99, 0)).Throws<ArgumentOutOfRangeException>();
    }

    // ── Dialog builders ───────────────────────────────────────────────────────

    [Test]
    public async Task InvokeOkDialog_ReturnsMessageUnchanged()
    {
        const string message = "Hello, adventurer!";
        await Assert.That(MapleText.InvokeOkDialog(message)).IsSameReferenceAs(message);
    }

    [Test]
    public async Task InvokeOkDialog_Null_Throws()
    {
        await Assert.That(() => MapleText.InvokeOkDialog(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task InvokeYesNoDialog_ContainsBothOptions()
    {
        string result = MapleText.InvokeYesNoDialog("Continue?");
        await Assert.That(result.Contains("Continue?")).IsTrue();
        await Assert.That(result.Contains("Yes")).IsTrue();
        await Assert.That(result.Contains("No")).IsTrue();
        await Assert.That(result.Contains("#L0#")).IsTrue();
        await Assert.That(result.Contains("#L1#")).IsTrue();
    }

    [Test]
    public async Task InvokeYesNoDialog_YesOptionIsBlue()
    {
        string result = MapleText.InvokeYesNoDialog("Fight?");
        // Yes entry is colored blue — the blue open marker precedes #L0#
        await Assert.That(result.Contains("#b")).IsTrue();
    }

    [Test]
    public async Task InvokeYesNoDialog_CustomOptions_UsesProvidedText()
    {
        string result = MapleText.InvokeYesNoDialog("Fight?", yesText: "Attack", noText: "Flee");
        await Assert.That(result.Contains("Attack")).IsTrue();
        await Assert.That(result.Contains("Flee")).IsTrue();
    }

    [Test]
    public async Task InvokeYesNoDialog_Null_Throws()
    {
        await Assert.That(() => MapleText.InvokeYesNoDialog(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task InvokeSelectDialog_BuildsListEntries()
    {
        string result = MapleText.InvokeSelectDialog("Choose:", "Alpha", "Beta", "Gamma");
        await Assert.That(result.Contains("Choose:")).IsTrue();
        await Assert.That(result.Contains("Alpha")).IsTrue();
        await Assert.That(result.Contains("Beta")).IsTrue();
        await Assert.That(result.Contains("Gamma")).IsTrue();
        await Assert.That(result.Contains("#L0#")).IsTrue();
        await Assert.That(result.Contains("#L1#")).IsTrue();
        await Assert.That(result.Contains("#L2#")).IsTrue();
    }

    [Test]
    public async Task InvokeSelectDialog_EmptyOptions_ReturnsJustQuestion()
    {
        string result = MapleText.InvokeSelectDialog("Question?");
        await Assert.That(result).IsEqualTo("Question?");
    }

    [Test]
    public async Task InvokeSelectDialog_Null_Throws()
    {
        await Assert.That(() => MapleText.InvokeSelectDialog(null!, "A")).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task InvokeMenuDialog_BuildsFromDictionary()
    {
        var entries = new Dictionary<int, string> { { 0, "Start" }, { 1, "Exit" } };
        string result = MapleText.InvokeMenuDialog("Menu:", entries);
        await Assert.That(result.Contains("Menu:")).IsTrue();
        await Assert.That(result.Contains("Start")).IsTrue();
        await Assert.That(result.Contains("Exit")).IsTrue();
    }

    [Test]
    public async Task InvokeMenuDialog_NullQuestion_Throws()
    {
        await Assert.That(() => MapleText.InvokeMenuDialog(null!, [])).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task InvokeMenuDialog_NullEntries_Throws()
    {
        await Assert.That(() => MapleText.InvokeMenuDialog("Q", null!)).Throws<ArgumentNullException>();
    }
}
