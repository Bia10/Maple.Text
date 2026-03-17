using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Tests for <see cref="MapleTextToken.GetValue"/> and <see cref="MapleTextToken.GetPayload"/>
/// edge cases not covered by parser-level tests.
/// </summary>
public sealed class MapleTextTokenValueTests
{
    // ── GetValue on Escape token ──────────────────────────────────────────────

    [Test]
    public async Task GetValue_EscapeToken_ReturnsHash()
    {
        const string input = "##";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Escape);
        await Assert.That(result.Tokens[0].GetValue(input)).IsEqualTo("#");
    }

    // ── GetValue on StyleCode / UnknownCode tokens ────────────────────────────

    [Test]
    public async Task GetValue_StyleCodeToken_ReturnsCodeChar()
    {
        const string input = "#b";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StyleCode);
        await Assert.That(result.Tokens[0].GetValue(input)).IsEqualTo("b");
    }

    [Test]
    public async Task GetValue_UnknownCodeToken_ReturnsCodeChar()
    {
        // '\x01' is a control char — not in any code set, not IsLiteralHashSequence, not a letter.
        // This reaches the "Completely unknown" branch in the parser and GetValue's StyleCode/UnknownCode case.
        const string input = "#\x01";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.UnknownCode);
        await Assert.That(result.Tokens[0].GetValue(input)).IsEqualTo("\x01");
    }

    // ── GetValue on StatToken — PayloadLength == 0 (struct-constructed directly) ──

    [Test]
    public async Task GetValue_StatToken_ZeroPayloadLength_ReturnsCodeChar()
    {
        // PayloadLength == 0 is unreachable through the parser (stat tokens always have ≥1 payload char),
        // but the branch exists in GetValue. Construct the struct directly to exercise it.
        var token = new MapleTextToken
        {
            Kind = MapleTextTokenKind.StatToken,
            Code = 'x',
            Start = 0,
            Length = 2,
            PayloadStart = 1,
            PayloadLength = 0,
        };
        await Assert.That(token.GetValue("#x")).IsEqualTo("x");
    }

    // ── GetValue on ClientToken (MBCS, Code == '\0') ──────────────────────────

    [Test]
    public async Task GetValue_MbcsClientToken_WithPayload_ReturnsPayloadString()
    {
        // "#가나다" — '#' followed by Korean letters routes through TryParseEntity (MBCS path).
        // Produces ClientToken with Code='\0', payload includes the entity-code char + Korean letters.
        const string input = "#가나다";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(token.Code).IsEqualTo('\0');
        // GetValue returns the payload substring (includes the Korean chars).
        string value = token.GetValue(input);
        await Assert.That(value).IsNotEmpty();
        await Assert.That(value.Any(c => !char.IsAscii(c))).IsTrue();
    }

    [Test]
    public async Task GetValue_MbcsClientToken_ZeroPayload_ReturnsEmpty()
    {
        // PayloadLength == 0 for a '\0'-code ClientToken is unreachable through the parser
        // but exists as a branch in GetValue. Construct directly.
        var token = new MapleTextToken
        {
            Kind = MapleTextTokenKind.ClientToken,
            Code = '\0',
            Start = 0,
            Length = 1,
            PayloadStart = 1,
            PayloadLength = 0,
        };
        await Assert.That(token.GetValue("X")).IsEqualTo(string.Empty);
    }

    // ── GetValue on Block/ClientToken — default case, PayloadLength == 0 ─────

    [Test]
    public async Task GetValue_BlockToken_ZeroPayload_ReturnsCodeChar()
    {
        // Default case with PayloadLength==0 → Code.ToString().
        // Construct directly since "#D#" produces PayloadLength=0 for a Block token.
        const string input = "#D#";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.Block);
        await Assert.That(token.PayloadLength).IsEqualTo((ushort)0);
        await Assert.That(token.GetValue(input)).IsEqualTo("D");
    }

    [Test]
    public async Task GetValue_ClientToken_ZeroPayload_ViaH_ReturnsCodeChar()
    {
        // "#h#" → ClientToken Code='h', PayloadLength=0.
        const string input = "#h#";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(token.Code).IsEqualTo('h');
        await Assert.That(token.PayloadLength).IsEqualTo((ushort)0);
        await Assert.That(token.GetValue(input)).IsEqualTo("h");
    }

    // ── GetPayload — empty case ───────────────────────────────────────────────

    [Test]
    public async Task GetPayload_ZeroPayloadLength_ReturnsEmptySpan()
    {
        const string input = "#b"; // StyleCode, no payload
        var result = MapleTextParser.Parse(input);
        var token = result.Tokens[0];
        await Assert.That(token.GetPayload(input.AsSpan()).IsEmpty).IsTrue();
    }
}
