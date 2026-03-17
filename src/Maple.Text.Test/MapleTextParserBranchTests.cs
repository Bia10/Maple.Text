using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Parser tests targeting branches not reached by the primary test suite,
/// including argument guards, MBCS paths, entity-code edge cases, long-form token terminators,
/// and the <see cref="ValueStringBuilder"/> Grow path.
/// </summary>
public sealed class MapleTextParserBranchTests
{
    // ── ArgumentOutOfRange guard ──────────────────────────────────────────────

    [Test]
    public async Task Parse_StringLongerThan65535_ThrowsArgumentOutOfRange()
    {
        string tooLong = new string('A', 65536);
        await Assert.That(() => MapleTextParser.Parse(tooLong)).Throws<ArgumentOutOfRangeException>();
    }

    // ── MBCS particle via main loop (non-entity, IsLetter → non-ASCII) ────────

    [Test]
    public async Task Parse_MbcsParticle_ViaMainLoop_ProducesClientToken()
    {
        // '#가나다' — '#' followed by Korean letters goes through
        // the char.IsLetter(next) branch → ContainsNonAsciiLetter → MBCS ClientToken.
        const string input = "#가나다";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(token.Code).IsEqualTo('\0');
    }

    // ── MBCS particle via TryParseEntity path ─────────────────────────────────

    [Test]
    public async Task Parse_EntityCode_FollowedByNonAsciiLetters_ProducesMbcsClientToken()
    {
        // '#m가나다' — 'm' is an EntityCode, but the payload is Korean → MBCS path inside TryParseEntity.
        const string input = "#m가나다";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(token.Code).IsEqualTo('\0');
    }

    // ── TryParseEntity: entity code + letters → ClientTokenCodes fallback ─────

    [Test]
    public async Task Parse_EntityCodeFollowedByLetters_WithClientCodeOverlap_ProducesClientToken()
    {
        // 'z' is in both EntityCodes and ClientTokenCodes.
        // "#zabc" → TryParseEntity sees 'z', no digits, 'a' is a letter,
        // ClientTokenCodes.Contains('z') → ParseLongFormToken → ClientToken.
        const string input = "#zabc";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(token.Code).IsEqualTo('z');
    }

    // ── TryParseEntity: span[start] == '#' → Text token ─────────────────────

    [Test]
    public async Task Parse_EntityCode_ImmediatelyFollowedByHash_ProducesTextToken()
    {
        // "#t#" — entity code 't' followed immediately by '#'. TryParseEntity sees
        // span[start]=='#' and returns a Text token (neither entity nor error).
        const string input = "#t#";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Text);
    }

    // ── TryParseEntity: IsLiteralHashSequence(span[start]) → Text token ──────

    [Test]
    public async Task Parse_EntityCode_FollowedByLiteralHashSequenceChar_ProducesTextToken()
    {
        // "#t " — entity code 't' followed by a space (IsLiteralHashSequence → true).
        // TryParseEntity returns a Text token for "#t".
        const string input = "#t ";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        // Text("#t") + Text(" ")
        await Assert.That(result.Tokens.All(t => t.Kind == MapleTextTokenKind.Text)).IsTrue();
    }

    // ── TryParseEntity: truly unknown char after entity code → UnknownCode ────

    [Test]
    public async Task Parse_EntityCode_FollowedByControlChar_ProducesUnknownCode()
    {
        // "#t\x01" — '\x01' (SOH control) is not a digit, not a letter, not '#',
        // not IsLiteralHashSequence → UnknownCode token from TryParseEntity.
        // The '\x01' char is then emitted as a separate Text token.
        const string input = "#t\x01";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Tokens.Any(t => t.Kind == MapleTextTokenKind.UnknownCode)).IsTrue();
    }

    // ── ParseLongFormToken: terminator is backslash ───────────────────────────

    [Test]
    public async Task Parse_ClientToken_TerminatedByBackslash_HasCorrectPayload()
    {
        // "#hname\\rest" — ParseLongFormToken stops at '\'.
        const string input = "#hname\\rest";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        var clientToken = result.Tokens.FirstOrDefault(t => t.Kind == MapleTextTokenKind.ClientToken);
        await Assert.That(clientToken.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(clientToken.Code).IsEqualTo('h');
        // Payload = "name" (4 chars before the backslash).
        await Assert.That(clientToken.GetPayload(input.AsSpan()).ToString()).IsEqualTo("name");
    }

    // ── ParseLongFormToken: terminator is \r or \n ────────────────────────────

    [Test]
    public async Task Parse_ClientToken_TerminatedByNewline_HasCorrectPayload()
    {
        // "#hname\nrest" — ParseLongFormToken stops at '\n'.
        const string input = "#hname\nrest";
        var result = MapleTextParser.Parse(input);
        var clientToken = result.Tokens.FirstOrDefault(t => t.Kind == MapleTextTokenKind.ClientToken);
        await Assert.That(clientToken.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(clientToken.GetPayload(input.AsSpan()).ToString()).IsEqualTo("name");
    }

    [Test]
    public async Task Parse_ClientToken_TerminatedByCr_HasCorrectPayload()
    {
        const string input = "#hname\rrest";
        var result = MapleTextParser.Parse(input);
        var clientToken = result.Tokens.FirstOrDefault(t => t.Kind == MapleTextTokenKind.ClientToken);
        await Assert.That(clientToken.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(clientToken.GetPayload(input.AsSpan()).ToString()).IsEqualTo("name");
    }

    // ── Completely unknown non-letter code in main loop ───────────────────────

    [Test]
    public async Task Parse_CompletelyUnknownNonLetterCode_ProducesUnknownCode()
    {
        // "#\x01" — '\x01' is not a letter, not in any code table, not IsLiteralHashSequence.
        // Hits the final "Completely unknown token" branch in the main loop.
        const string input = "#\x01";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.UnknownCode);
    }

    // ── Double-colon entity variant without closing '#' → unterminated ────────

    [Test]
    public async Task Parse_ColonEntity_WithoutClosingHash_SetsHasErrors()
    {
        // "#i4000001::" without a trailing '#' — TryParseEntity returns false → UnterminatedEntity.
        const string input = "#i4000001::";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsTrue();
    }

    // ── Unknown multi-letter code not in ClientTokenCodes ─────────────────────

    [Test]
    public async Task Parse_MultiLetterUnknownCode_NotInClientTokens_ProducesUnknownCode()
    {
        // "#Xabc" — 'X' (uppercase) is not in ClientTokenCodes (only lowercase 'x' is).
        // Goes through IsLetter branch, not stat, not MBCS, not ClientTokenCode
        // → Unknown multi-letter token with hasErrors=true.
        const string input = "#Xabc";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Tokens.Any(t => t.Kind == MapleTextTokenKind.UnknownCode)).IsTrue();
    }

    // ── ValueStringBuilder Grow path (via > 512 char decoded output) ─────────

    [Test]
    public async Task Decode_OutputExceeds512Chars_GrowsBufferCorrectly()
    {
        // ValueStringBuilder starts with 512-char stackalloc.
        // Input: style wrapper around 600 'A's forces output > 512 chars → triggers Grow().
        string longText = new string('A', 600);
        string input = "#b" + longText + "#k";
        string decoded = MapleTextDecoder.Decode(input);
        await Assert.That(decoded).IsEqualTo(longText);
    }

    [Test]
    public async Task Decode_VeryLongOutput_GrowsTwice_ReturnsCorrectResult()
    {
        // Trigger Grow() more than once by going well beyond 512*2=1024 chars.
        string longText = new string('B', 2000);
        string input = "#b" + longText + "#k";
        string decoded = MapleTextDecoder.Decode(input);
        await Assert.That(decoded).IsEqualTo(longText);
    }

    // ── L token followed by consecutive '#' — index++ (line 220) ─────────────

    [Test]
    public async Task Parse_LToken_ConsecutiveHash_TriggersIndexIncrement()
    {
        // "#L0###k":
        // '#L0' with closer '#' at pos 3 → nextIndex=4.
        // span[4]='#' satisfies (next=='L' && span[index]=='#') → index++ → index=5.
        // '#k' at pos 5-6 is then parsed as a StyleCode token.
        const string input = "#L0###k";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens.Count).IsEqualTo(2);
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('L');
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(MapleTextTokenKind.StyleCode);
        await Assert.That(result.Tokens[1].Code).IsEqualTo('k');
    }
}
