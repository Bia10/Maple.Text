using Maple.Text.Parsing;

namespace Maple.Text.Test;

public sealed class MapleTextParserEdgeCaseTests
{
    // ── Stat tokens ───────────────────────────────────────────────────────────

    [Test]
    public async Task Parse_StatToken_mpCon_ProducesStatToken()
    {
        var result = MapleTextParser.Parse("#mpCon");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StatToken);
    }

    [Test]
    public async Task Parse_StatToken_mpCon_PayloadIsStatName()
    {
        const string input = "#mpCon";
        var result = MapleTextParser.Parse(input);
        string payload = result.Tokens[0].GetPayload(input.AsSpan()).ToString();
        await Assert.That(payload).IsEqualTo("mpCon");
    }

    [Test]
    public async Task Parse_StatToken_SingleLetter_x_ProducesStatToken()
    {
        var result = MapleTextParser.Parse("#x");
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StatToken);
    }

    [Test]
    public async Task Parse_StatToken_InContext_PreservesTextTokens()
    {
        var result = MapleTextParser.Parse("Costs #mpCon MP");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).Contains(t => t.Kind == MapleTextTokenKind.StatToken);
        await Assert.That(result.Tokens).Contains(t => t.Kind == MapleTextTokenKind.Text);
    }

    [Test]
    public async Task Parse_TwoConsecutiveStatTokens_BothParsed()
    {
        const string input = "#mpCon#hpCon";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        var stats = result.Tokens.Where(t => t.Kind == MapleTextTokenKind.StatToken).ToList();
        await Assert.That(stats.Count).IsEqualTo(2);
        await Assert.That(stats[0].GetPayload(input.AsSpan()).ToString()).IsEqualTo("mpCon");
        await Assert.That(stats[1].GetPayload(input.AsSpan()).ToString()).IsEqualTo("hpCon");
    }

    // ── Client tokens ─────────────────────────────────────────────────────────

    [Test]
    public async Task Parse_ClientToken_h0_ProducesClientToken()
    {
        var result = MapleTextParser.Parse("#h0");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('h');
    }

    [Test]
    public async Task Parse_ClientToken_p100_ProducesClientToken()
    {
        var result = MapleTextParser.Parse("#p100");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('p');
    }

    [Test]
    public async Task Parse_ClientToken_q_ProducesClientToken()
    {
        var result = MapleTextParser.Parse("#q1001");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('q');
    }

    [Test]
    public async Task Parse_ClientToken_L_WithTrailingHash_ProducesClientToken()
    {
        // #L0# — the trailing '#' is consumed as the L-separator
        var result = MapleTextParser.Parse("#L0#");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.ClientToken);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('L');
    }

    [Test]
    public async Task Parse_ClientToken_at_IsLiteralHashSequence()
    {
        // '@' is OtherPunctuation → IsLiteralHashSequence('#@') = true.
        // '#' emits a literal Text token; '@1001' becomes a second Text token.
        var result = MapleTextParser.Parse("#@1001");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens.Count).IsEqualTo(2);
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Text);
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(MapleTextTokenKind.Text);
    }

    // ── Block codes ───────────────────────────────────────────────────────────

    [Test]
    public async Task Parse_BlockCode_D_WithPayload_ProducesBlockToken()
    {
        const string input = "#Dkey#";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.Block);
        await Assert.That(token.Code).IsEqualTo('D');
        await Assert.That(token.GetPayload(input.AsSpan()).ToString()).IsEqualTo("key");
    }

    [Test]
    public async Task Parse_BlockCode_Q_WithPayload_ProducesBlockToken()
    {
        var result = MapleTextParser.Parse("#Qtimer#");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Block);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('Q');
    }

    [Test]
    public async Task Parse_BlockCode_j_WithPayload_ProducesBlockToken()
    {
        var result = MapleTextParser.Parse("#jkillCount#");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Block);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('j');
    }

    // ── Colon entity variant ──────────────────────────────────────────────────

    [Test]
    public async Task Parse_ColonEntity_ProducesEntityReferenceToken()
    {
        // #i4000001:# — item icon slot variant
        var result = MapleTextParser.Parse("#i4000001:#");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.EntityReference);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('i');
    }

    // ── Double-hash style (##k) ───────────────────────────────────────────────

    [Test]
    public async Task Parse_DoubleHashStyle_k_ProducesStyleCodeToken()
    {
        // ##k — treated as style-code reset, not Escape; length = 3
        var result = MapleTextParser.Parse("##k");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        var token = result.Tokens[0];
        await Assert.That(token.Kind).IsEqualTo(MapleTextTokenKind.StyleCode);
        await Assert.That(token.Code).IsEqualTo('k');
        await Assert.That(token.Length).IsEqualTo((ushort)3);
    }

    [Test]
    public async Task Parse_DoubleHashStyle_b_ProducesStyleCodeToken()
    {
        var result = MapleTextParser.Parse("##b");
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StyleCode);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('b');
    }

    [Test]
    public async Task Parse_DoubleHashNonStyle_ProducesEscapeToken()
    {
        // ##1 — '1' is not a style code → Escape
        var result = MapleTextParser.Parse("##1");
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Escape);
    }

    // ── Literal hash sequences ────────────────────────────────────────────────

    [Test]
    public async Task Parse_HashFollowedByDigit_TreatedAsLiteralHash()
    {
        // "#1" — digit triggers IsLiteralHashSequence → '#' emits a Text(length=1) token,
        // '1' is then swept up as a second Text token. No UnknownCode, no errors.
        var result = MapleTextParser.Parse("#1");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens.All(t => t.Kind == MapleTextTokenKind.Text)).IsTrue();
    }

    [Test]
    public async Task Parse_HashFollowedBySpace_TreatedAsLiteralHash()
    {
        // ' ' triggers IsLiteralHashSequence → '#' emits a Text token; space becomes another.
        var result = MapleTextParser.Parse("# ");
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens.All(t => t.Kind == MapleTextTokenKind.Text)).IsTrue();
    }

    // ── Token accessor helpers ────────────────────────────────────────────────

    [Test]
    public async Task Token_GetRaw_ReturnsFullTokenText()
    {
        const string input = "#b";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens[0].GetRaw(input.AsSpan()).ToString()).IsEqualTo("#b");
    }

    [Test]
    public async Task Token_GetRawString_ReturnsString()
    {
        const string input = "#bHello#k";
        var result = MapleTextParser.Parse(input);
        // Tokens[1] = Text "Hello"
        await Assert.That(result.Tokens[1].GetRawString(input)).IsEqualTo("Hello");
    }

    [Test]
    public async Task Token_GetValue_OnTextToken_ReturnsEmpty()
    {
        // Text tokens have no markup payload — GetValue returns "" by design.
        const string input = "Hello";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens[0].GetValue(input)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Token_GetValue_OnEntityToken_ReturnsCodeColonPayload()
    {
        // GetValue on an EntityReference returns "code:payload" (e.g. "t:2000001").
        const string input = "#t2000001#";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens[0].GetValue(input)).IsEqualTo("t:2000001");
    }

    [Test]
    public async Task Token_GetValue_OnStatToken_ReturnsStatName()
    {
        // GetValue on a StatToken returns the stat name substring (no '#' prefix).
        const string input = "#mpCon";
        var result = MapleTextParser.Parse(input);
        await Assert.That(result.Tokens[0].GetValue(input)).IsEqualTo("mpCon");
    }

    // ── TokensSpan ────────────────────────────────────────────────────────────

    [Test]
    public async Task ParseResult_TokensSpan_MatchesTokensList()
    {
        var result = MapleTextParser.Parse("#bHello#k");
        // Snapshot span data before any await — ReadOnlySpan<T> can't cross await boundaries.
        var span = result.TokensSpan;
        int spanLength = span.Length;
        var spanKinds = new MapleTextTokenKind[spanLength];
        for (int i = 0; i < spanLength; i++)
            spanKinds[i] = span[i].Kind;

        await Assert.That(spanLength).IsEqualTo(result.Tokens.Count);
        for (int i = 0; i < spanLength; i++)
            await Assert.That(spanKinds[i]).IsEqualTo(result.Tokens[i].Kind);
    }
}
