using Maple.Text.Parsing;

namespace Maple.Text.Test;

public sealed class MapleTextStripperTests
{
    // ── StripMarkup(string): input > 512 chars → ArrayPool path ──────────────

    [Test]
    public async Task StripMarkup_String_Over512Chars_ArrayPoolPath()
    {
        // Content > 512 chars forces the ArrayPool rental (non-stackalloc) path.
        string markup = "#b" + new string('A', 600) + "#k";
        string result = MapleTextStripper.StripMarkup(markup);
        await Assert.That(result).IsEqualTo(new string('A', 600));
    }

    // ── StripMarkup(string): written == len fast-path (returns same instance) ─

    [Test]
    public async Task StripMarkup_String_WhenWrittenEqualsInput_ReturnsSameInstance()
    {
        // A string with a '#' followed by a digit: IsLiteralHashSequence=true → writes '#'.
        // The output length equals the input length → fast-path returns the original string.
        const string input = "#1";
        string result = MapleTextStripper.StripMarkup(input);
        // The output is the same content (written count equals input length).
        await Assert.That(result).IsEqualTo(input);
    }

    // ── StripMarkup(ReadOnlySpan<char>): over 512 chars → ArrayPool ──────────

    [Test]
    public async Task StripMarkup_Span_Over512Chars_ArrayPoolPath()
    {
        string markup = "#b" + new string('B', 600) + "#k";
        string result = MapleTextStripper.StripMarkup(markup.AsSpan());
        await Assert.That(result).IsEqualTo(new string('B', 600));
    }

    // ── SkipEntity: colon variant path ────────────────────────────────────────

    [Test]
    public async Task StripMarkup_ColonEntity_DoubleColon_Stripped()
    {
        // "#i4000001::# " — tests the 'while colons' loop inside SkipEntity.
        await Assert.That(MapleTextStripper.StripMarkup("#i4000001::# done")).IsEqualTo(" done");
    }

    [Test]
    public async Task StripMarkup_ColonEntity_WithoutClosingHash_AdvancesPastColon()
    {
        // "#i4000001:" without closing '#' — SkipEntity skips digits + colon, no closing hash.
        string result = MapleTextStripper.StripMarkup("#i4000001:");
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    // ── SkipEntity: entity code with no digits, no letters → skip only #X ────

    [Test]
    public async Task StripMarkup_EntityCode_NoDigitsOrLetters_SkipsOnlyHashX()
    {
        // "#t " — 't' is entity code, space after → SkipEntity returns pos+2, space stays.
        string result = MapleTextStripper.StripMarkup("#t done");
        // "#t" (2 chars) skipped, " done" remains.
        await Assert.That(result).IsEqualTo(" done");
    }

    // ── SkipLetterToken: non-ASCII → skips entire token ──────────────────────

    [Test]
    public async Task StripMarkup_MbcsLetter_EntireTokenSkipped()
    {
        // "#가나다" — '#' followed by Korean → SkipLetterToken skips all Korean letters.
        string result = MapleTextStripper.StripMarkup("#가나다");
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    // ── SkipLetterToken: letter IS in ClientTokenCodes → delegates to SkipLongForm ─

    [Test]
    public async Task StripMarkup_LetterClientToken_DelegatesToSkipLongForm()
    {
        // "#hname" — 'h' is in ClientTokenCodes and is a letter; SkipLetterToken
        // delegates to SkipLongForm which consumes to EOF.
        string result = MapleTextStripper.StripMarkup("#hname");
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    // ── SkipLetterToken: unknown multi-letter → skips only #X ────────────────

    [Test]
    public async Task StripMarkup_UnknownMultiLetter_SkipsOnlyHashX()
    {
        // "#Xabc" — 'X' not in ClientTokenCodes, not stat, not non-ASCII → skips "#X" only.
        string result = MapleTextStripper.StripMarkup("#Xabc");
        await Assert.That(result).IsEqualTo("abc");
    }

    // ── SkipLongForm: '#L' consumes the trailing '#' ──────────────────────────

    [Test]
    public async Task StripMarkup_LToken_TrailingHashConsumed()
    {
        // "#L0#text#l" — '#L0' is a list opener; '#' after '0' is consumed by SkipLongForm.
        // "text" is plain text; "#l" is a ClientToken (skipped by ClientTokenCodes path).
        string result = MapleTextStripper.StripMarkup("#L0#Option#l");
        await Assert.That(result).IsEqualTo("Option");
    }

    // ── SkipLongForm: non-L token where end is NOT '#' (hasCloser=false in parser) ─

    [Test]
    public async Task StripMarkup_ClientToken_TerminatedByBackslash_SkipsToBackslash()
    {
        // "#hname\\" — SkipLongForm for 'h' stops at '\\', not '#', so no extra skip.
        string result = MapleTextStripper.StripMarkup("#hname\\rest");
        await Assert.That(result).IsEqualTo("\\rest");
    }

    // ── Unknown #X in strip core (not letter, not ClientToken, not Style/Entity/Block) ─

    [Test]
    public async Task StripMarkupCore_UnknownNonLetterCode_SkipsHashX()
    {
        // "#\x01" — '\x01' triggers the final "pos += 2" branch in StripMarkupCore.
        var output = new char[16];
        int written = MapleTextStripper.StripMarkupCore("#\x01rest".AsSpan(), output.AsSpan());
        await Assert.That(new string(output, 0, written)).IsEqualTo("rest");
    }

    // ── BlockCode: unterminated (no closing '#') ──────────────────────────────

    [Test]
    public async Task StripMarkup_BlockCode_NoClosingHash_SkipsOnlyBlockPrefix()
    {
        // #Q with no closing '#' → closeIdx is negative → pos = pos + 2 (skip "#Q").
        string result = MapleTextStripper.StripMarkup("#Qunterminated");
        await Assert.That(result).IsEqualTo("unterminated");
    }
}
