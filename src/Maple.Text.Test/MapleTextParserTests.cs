using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Tests for <see cref="MapleTextParser"/>, <see cref="MapleTextStripper"/>,
/// and <see cref="MapleTextDecoder"/> covering common input patterns.
/// </summary>
public sealed class MapleTextParserTests
{
    // ── MapleTextParser.Parse ─────────────────────────────────────────────────

    [Test]
    public async Task Parse_EmptyString_ReturnsNoTokens()
    {
        var result = MapleTextParser.Parse(string.Empty);
        await Assert.That(result.Tokens).IsEmpty();
        await Assert.That(result.HasErrors).IsFalse();
    }

    [Test]
    public async Task Parse_PlainText_ReturnsSingleTextToken()
    {
        var result = MapleTextParser.Parse("Hello World");
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Text);
        await Assert.That(result.HasErrors).IsFalse();
    }

    [Test]
    public async Task Parse_StyleCode_Bold_ReturnsStyleCodeToken()
    {
        var result = MapleTextParser.Parse("#b");
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StyleCode);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('b');
    }

    [Test]
    public async Task Parse_StyleCode_Reset_ReturnsStyleCodeToken()
    {
        var result = MapleTextParser.Parse("#k");
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StyleCode);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('k');
    }

    [Test]
    public async Task Parse_TextSurroundedByStyleCodes_ReturnsThreeTokens()
    {
        var result = MapleTextParser.Parse("#bHello#k");
        await Assert.That(result.Tokens.Count).IsEqualTo(3);
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.StyleCode); // #b
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(MapleTextTokenKind.Text); // Hello
        await Assert.That(result.Tokens[2].Kind).IsEqualTo(MapleTextTokenKind.StyleCode); // #k
        await Assert.That(result.HasErrors).IsFalse();
    }

    [Test]
    public async Task Parse_EscapeSequence_ReturnsEscapeToken()
    {
        var result = MapleTextParser.Parse("##");
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Escape);
        await Assert.That(result.HasErrors).IsFalse();
    }

    [Test]
    public async Task Parse_TrailingHash_ProducesNoToken()
    {
        // A trailing '#' at EOF is discarded by the client — parser should too.
        var result = MapleTextParser.Parse("Hello#");
        await Assert.That(result.Tokens).HasSingleItem(); // just the "Hello" text token
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Text);
    }

    [Test]
    public async Task Parse_EntityCode_ReturnsEntityReferenceToken()
    {
        // #t<id># pattern — entity type 't' with payload "2000001"
        var result = MapleTextParser.Parse("#t2000001#");
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.EntityReference);
        await Assert.That(result.Tokens[0].Code).IsEqualTo('t');
        await Assert.That(result.HasErrors).IsFalse();
    }

    [Test]
    public async Task Parse_Null_Throws()
    {
        await Assert.That(() => MapleTextParser.Parse(null!)).Throws<ArgumentNullException>();
    }

    // ── Malformed / edge-case tokens ─────────────────────────────────────────

    [Test]
    public async Task Parse_EntityCode_WithoutClosingHash_SetsHasErrors()
    {
        // #t<id> with no closing '#' is malformed — parser should flag HasErrors
        // and still produce a token rather than silently discarding input.
        var result = MapleTextParser.Parse("#t2000001");
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Tokens).IsNotEmpty();
    }

    [Test]
    public async Task Parse_BlockCode_WithoutClosingHash_SetsHasErrors()
    {
        // #D... with no closing '#' — block is unterminated
        var result = MapleTextParser.Parse("#DUnterminated text");
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Tokens).IsNotEmpty();
    }

    [Test]
    public async Task Parse_MultipleTokens_MixedValid_ReturnsBothTokenKinds()
    {
        // "#bHello #t2000001# World#k" — bold + text + entity + text + reset
        var result = MapleTextParser.Parse("#bHello #t2000001# World#k");
        await Assert.That(result.HasErrors).IsFalse();
        // At minimum: StyleCode(b), Text("Hello "), EntityRef(t/2000001), Text(" World"), StyleCode(k)
        await Assert.That(result.Tokens.Count >= 5).IsTrue();
        await Assert.That(result.Tokens).Contains(t => t.Kind == MapleTextTokenKind.EntityReference);
    }

    [Test]
    public async Task Parse_UnknownCode_SetsHasErrors()
    {
        // #X where 'X' matches none of the known code tables
        var result = MapleTextParser.Parse("#X");
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.UnknownCode);
    }

    // ── MapleTextStripper.StripMarkup ─────────────────────────────────────────

    [Test]
    public async Task StripMarkup_NoMarkup_ReturnsSameInstance()
    {
        const string input = "Hello World";
        var result = MapleTextStripper.StripMarkup(input);
        await Assert.That(result).IsSameReferenceAs(input); // fast-path: exact same reference
    }

    [Test]
    public async Task StripMarkup_StyleCodes_RemovesMarkup()
    {
        var result = MapleTextStripper.StripMarkup("#bHello#k");
        await Assert.That(result).IsEqualTo("Hello");
    }

    [Test]
    public async Task StripMarkup_EscapedHash_KeepsLiteralHash()
    {
        var result = MapleTextStripper.StripMarkup("100##");
        await Assert.That(result).IsEqualTo("100#");
    }

    [Test]
    public async Task StripMarkup_EntityReference_RemovesEntity()
    {
        // Entity references are stripped (not resolved) by StripMarkup.
        var result = MapleTextStripper.StripMarkup("Item: #t2000001# acquired");
        await Assert.That(result).IsEqualTo("Item:  acquired");
    }

    [Test]
    public async Task StripMarkup_Null_Throws()
    {
        await Assert.That(() => MapleTextStripper.StripMarkup(null!)).Throws<ArgumentNullException>();
    }

    // ── MapleTextDecoder.Decode ───────────────────────────────────────────────

    [Test]
    public async Task Decode_PlainText_ReturnsSameInstance()
    {
        const string input = "Hello World";
        var result = MapleTextDecoder.Decode(input);
        await Assert.That(result).IsSameReferenceAs(input); // fast-path: no markup, same reference
    }

    [Test]
    public async Task Decode_StyleCodesOnly_ReturnsEmpty()
    {
        // Style codes strip to nothing when decoded without a resolver.
        var result = MapleTextDecoder.Decode("#b#k");
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Decode_EscapedHash_ReturnsLiteralHash()
    {
        var result = MapleTextDecoder.Decode("100##");
        await Assert.That(result).IsEqualTo("100#");
    }

    [Test]
    public async Task Decode_StatToken_WithoutResolver_KeepsRawForm()
    {
        // Without a resolver, stat tokens appear as-is in the output.
        var result = MapleTextDecoder.Decode("#x");
        await Assert.That(result).IsEqualTo("#x");
    }

    [Test]
    public async Task Decode_StatToken_WithResolver_UsesResolvedValue()
    {
        var resolver = new FixedResolver(stat => $"[{stat}]");
        var result = MapleTextDecoder.Decode("#mpCon MP", resolver);
        await Assert.That(result).IsEqualTo("[mpCon] MP");
    }

    [Test]
    public async Task Decode_Null_Throws()
    {
        await Assert.That(() => MapleTextDecoder.Decode(null!)).Throws<ArgumentNullException>();
    }

    // ── Edge-case: consecutive link structures ────────────────────────────────

    [Test]
    public async Task Parse_TwoConsecutiveEntityReferences_ParsesBoth()
    {
        // Two adjacent entity refs with no separator between them.
        var result = MapleTextParser.Parse("#t2000001##t4000000#");

        await Assert.That(result.HasErrors).IsFalse();
        var entityRefs = result.Tokens.Where(t => t.Kind == MapleTextTokenKind.EntityReference).ToList();
        await Assert.That(entityRefs).Count().IsEqualTo(2);
    }

    // ── Edge-case: malformed color / block code ───────────────────────────────

    [Test]
    public async Task Parse_BlockCode_EmptyPayload_ProducesBlockToken()
    {
        // #D# — a block code with zero-length payload is syntactically valid.
        var result = MapleTextParser.Parse("#D#");

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Tokens).HasSingleItem();
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(MapleTextTokenKind.Block);
    }

    // ── Test double ───────────────────────────────────────────────────────────

    private sealed class FixedResolver : IMapleTextResolver
    {
        private readonly Func<string, string> _statResolver;

        public FixedResolver(Func<string, string> statResolver) => _statResolver = statResolver;

        public string ResolveStat(string token) => _statResolver(token);

        public string ResolveEntity(string token) => token;

        public string ResolveClientToken(string token) => string.Empty;

        public string ResolveBlock(string token) => string.Empty;
    }
}
