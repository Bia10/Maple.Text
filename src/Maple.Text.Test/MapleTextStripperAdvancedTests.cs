using Maple.Text.Parsing;

namespace Maple.Text.Test;

public sealed class MapleTextStripperAdvancedTests
{
    // ── ContainsMarkup ────────────────────────────────────────────────────────

    [Test]
    public async Task ContainsMarkup_String_TrueWhenHashPresent()
    {
        await Assert.That(MapleTextStripper.ContainsMarkup("#b")).IsTrue();
    }

    [Test]
    public async Task ContainsMarkup_String_FalseWhenNoHash()
    {
        await Assert.That(MapleTextStripper.ContainsMarkup("Hello")).IsFalse();
    }

    [Test]
    public async Task ContainsMarkup_String_Null_Throws()
    {
        await Assert.That(() => MapleTextStripper.ContainsMarkup(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ContainsMarkup_Span_TrueWhenHashPresent()
    {
        await Assert.That(MapleTextStripper.ContainsMarkup("#b".AsSpan())).IsTrue();
    }

    [Test]
    public async Task ContainsMarkup_Span_FalseWhenNoHash()
    {
        await Assert.That(MapleTextStripper.ContainsMarkup("plain".AsSpan())).IsFalse();
    }

    [Test]
    public async Task ContainsMarkup_Span_FalseForEmptySpan()
    {
        await Assert.That(MapleTextStripper.ContainsMarkup(ReadOnlySpan<char>.Empty)).IsFalse();
    }

    // ── StripMarkup(ReadOnlySpan<char>) overload ──────────────────────────────

    [Test]
    public async Task StripMarkup_Span_WithMarkup_StripsCorrectly()
    {
        await Assert.That(MapleTextStripper.StripMarkup("#bHello#k".AsSpan())).IsEqualTo("Hello");
    }

    [Test]
    public async Task StripMarkup_Span_NoMarkup_ReturnsNewStringWithSameContent()
    {
        // Span overload always allocates a new string (no fast-path reference return).
        await Assert.That(MapleTextStripper.StripMarkup("Hello".AsSpan())).IsEqualTo("Hello");
    }

    [Test]
    public async Task StripMarkup_Span_EmptySpan_ReturnsEmptyString()
    {
        await Assert.That(MapleTextStripper.StripMarkup(ReadOnlySpan<char>.Empty)).IsEqualTo(string.Empty);
    }

    // ── Block code stripping ──────────────────────────────────────────────────

    [Test]
    public async Task StripMarkup_BlockCode_D_EntirelyStripped()
    {
        // #Dkey# — the full block (code + payload + closing '#') is stripped.
        await Assert.That(MapleTextStripper.StripMarkup("Time: #Dkey#")).IsEqualTo("Time: ");
    }

    [Test]
    public async Task StripMarkup_BlockCode_Q_EntirelyStripped()
    {
        await Assert.That(MapleTextStripper.StripMarkup("Timer: #Qtimer#")).IsEqualTo("Timer: ");
    }

    [Test]
    public async Task StripMarkup_UnterminatedBlock_SkipsOnlyBlockPrefix()
    {
        // #D with no closing '#' — only "#D" is consumed; payload text remains.
        await Assert.That(MapleTextStripper.StripMarkup("#Dunterminated")).IsEqualTo("unterminated");
    }

    // ── Stat / letter token stripping ─────────────────────────────────────────

    [Test]
    public async Task StripMarkup_StatToken_pad_Stripped()
    {
        // '#pad' — 'p' is not an entity code, so SkipLetterToken strips the full stat name.
        await Assert.That(MapleTextStripper.StripMarkup("Costs #pad ATK")).IsEqualTo("Costs  ATK");
    }

    [Test]
    public async Task StripMarkup_TwoStatTokens_BothStripped()
    {
        // '#acc' ('a') and '#pad' ('p') — neither starts with an entity code or style code.
        await Assert.That(MapleTextStripper.StripMarkup("#acc / #pad")).IsEqualTo(" / ");
    }

    [Test]
    public async Task StripMarkup_StatToken_mpCon_EntityPrefixBehavior()
    {
        // '#m' is an entity-code prefix; the stripper's SkipEntity only consumes "#m"
        // when no digits follow, leaving "pCon" as plain text.
        await Assert.That(MapleTextStripper.StripMarkup("#mpCon")).IsEqualTo("pCon");
    }

    // ── Long-form client token stripping ──────────────────────────────────────

    [Test]
    public async Task StripMarkup_ClientToken_h0_Stripped()
    {
        await Assert.That(MapleTextStripper.StripMarkup("Hello #h0")).IsEqualTo("Hello ");
    }

    [Test]
    public async Task StripMarkup_ListEntry_TextPreserved_MarkersStripped()
    {
        // #L0#Option#l — L-marker and l-closer stripped; "Option" survives.
        await Assert.That(MapleTextStripper.StripMarkup("#L0#Option#l")).IsEqualTo("Option");
    }

    // ── Colon entity stripping ────────────────────────────────────────────────

    [Test]
    public async Task StripMarkup_ColonEntity_Stripped()
    {
        await Assert.That(MapleTextStripper.StripMarkup("Icon: #i4000001:#")).IsEqualTo("Icon: ");
    }

    // ── StripMarkupCore internal ──────────────────────────────────────────────

    [Test]
    public async Task StripMarkupCore_PlainText_CopiesAllChars()
    {
        var output = new char[16];
        int written = MapleTextStripper.StripMarkupCore("Hello".AsSpan(), output.AsSpan());
        await Assert.That(written).IsEqualTo(5);
        await Assert.That(new string(output, 0, written)).IsEqualTo("Hello");
    }

    [Test]
    public async Task StripMarkupCore_StyleCode_WritesZeroChars()
    {
        var output = new char[16];
        int written = MapleTextStripper.StripMarkupCore("#b".AsSpan(), output.AsSpan());
        await Assert.That(written).IsEqualTo(0);
    }

    [Test]
    public async Task StripMarkupCore_EscapeHash_WritesLiteralHash()
    {
        var output = new char[16];
        int written = MapleTextStripper.StripMarkupCore("##".AsSpan(), output.AsSpan());
        await Assert.That(written).IsEqualTo(1);
        await Assert.That(output[0]).IsEqualTo('#');
    }

    [Test]
    public async Task StripMarkupCore_TrailingHash_IsDiscarded()
    {
        var output = new char[8];
        int written = MapleTextStripper.StripMarkupCore("AB#".AsSpan(), output.AsSpan());
        await Assert.That(written).IsEqualTo(2);
        await Assert.That(new string(output, 0, written)).IsEqualTo("AB");
    }

    [Test]
    public async Task StripMarkupCore_MixedInput_OnlyTextCharsWritten()
    {
        var output = new char[32];
        int written = MapleTextStripper.StripMarkupCore("#bHello#k World".AsSpan(), output.AsSpan());
        await Assert.That(new string(output, 0, written)).IsEqualTo("Hello World");
    }
}
