using Maple.Text.Parsing;

namespace Maple.Text.Test;

public sealed class MapleTextDecoderAdvancedTests
{
    // ── Entity references without resolver ────────────────────────────────────

    [Test]
    public async Task Decode_EntityRef_t_WithoutResolver_ProducesTColonId()
    {
        // Without resolver, entity refs render as "code:payload"
        await Assert.That(MapleTextDecoder.Decode("#t2000001#")).IsEqualTo("t:2000001");
    }

    [Test]
    public async Task Decode_EntityRef_i_WithoutResolver_ProducesIColonId()
    {
        await Assert.That(MapleTextDecoder.Decode("#i4000001#")).IsEqualTo("i:4000001");
    }

    [Test]
    public async Task Decode_EntityRef_o_WithoutResolver_ProducesOColonId()
    {
        await Assert.That(MapleTextDecoder.Decode("#o100100#")).IsEqualTo("o:100100");
    }

    [Test]
    public async Task Decode_EntityRef_m_WithoutResolver_ProducesMColonId()
    {
        await Assert.That(MapleTextDecoder.Decode("#m100000000#")).IsEqualTo("m:100000000");
    }

    [Test]
    public async Task Decode_EntityRef_z_WithoutResolver_ProducesZColonId()
    {
        await Assert.That(MapleTextDecoder.Decode("#z2000001#")).IsEqualTo("z:2000001");
    }

    // ── Entity references with resolver ───────────────────────────────────────

    [Test]
    public async Task Decode_EntityRef_WithResolver_UsesResolvedValue()
    {
        var resolver = new EntityResolver("Iron Sword");
        await Assert.That(MapleTextDecoder.Decode("#t2000001#", resolver)).IsEqualTo("Iron Sword");
    }

    [Test]
    public async Task Decode_EntityRef_WithResolver_InContext_PreservesText()
    {
        var resolver = new EntityResolver("Elixir");
        await Assert
            .That(MapleTextDecoder.Decode("Item: #t2000001# acquired", resolver))
            .IsEqualTo("Item: Elixir acquired");
    }

    // ── Stat tokens ───────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_StatToken_mpCon_WithoutResolver_KeepsRawForm()
    {
        await Assert.That(MapleTextDecoder.Decode("#mpCon")).IsEqualTo("#mpCon");
    }

    [Test]
    public async Task Decode_StatToken_x_WithoutResolver_KeepsRawForm()
    {
        await Assert.That(MapleTextDecoder.Decode("#x")).IsEqualTo("#x");
    }

    [Test]
    public async Task Decode_StatToken_WithResolver_UsesResolvedValue()
    {
        var resolver = new StatResolver("mpCon", "150");
        await Assert.That(MapleTextDecoder.Decode("#mpCon MP required", resolver)).IsEqualTo("150 MP required");
    }

    [Test]
    public async Task Decode_MultipleStatTokens_WithResolver_AllResolved()
    {
        var resolver = new StatResolver("x", "10");
        await Assert.That(MapleTextDecoder.Decode("#x / #x damage", resolver)).IsEqualTo("10 / 10 damage");
    }

    // ── Block tokens ──────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_BlockToken_WithoutResolver_ProducesNoOutput()
    {
        await Assert.That(MapleTextDecoder.Decode("#Dkey#")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Decode_BlockToken_WithResolver_UsesResolvedValue()
    {
        var resolver = new BlockResolver("42:30");
        await Assert.That(MapleTextDecoder.Decode("Time: #Dkey#", resolver)).IsEqualTo("Time: 42:30");
    }

    // ── Client tokens ─────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_ClientToken_WithoutResolver_ProducesNoOutput()
    {
        await Assert.That(MapleTextDecoder.Decode("Hello #h0")).IsEqualTo("Hello ");
    }

    [Test]
    public async Task Decode_ClientToken_WithResolver_UsesResolvedValue()
    {
        var resolver = new ClientResolver("Bia");
        await Assert.That(MapleTextDecoder.Decode("Hello #h0", resolver)).IsEqualTo("Hello Bia");
    }

    // ── Style codes ───────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_StyleCodes_AreStrippedFromOutput()
    {
        await Assert.That(MapleTextDecoder.Decode("#bBold text#k")).IsEqualTo("Bold text");
    }

    [Test]
    public async Task Decode_AllStyleCodes_ProduceOnlyText()
    {
        await Assert.That(MapleTextDecoder.Decode("#e#n#b#k#r#k")).IsEqualTo(string.Empty);
    }

    // ── Escape ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_EscapeHash_ProducesLiteralHash()
    {
        await Assert.That(MapleTextDecoder.Decode("100##")).IsEqualTo("100#");
    }

    // ── Mixed content ─────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_Mixed_StyleEntityText_DecodesCorrectly()
    {
        // Without resolver: "#bHello #t2000001# World#k" → "Hello t:2000001 World"
        await Assert.That(MapleTextDecoder.Decode("#bHello #t2000001# World#k")).IsEqualTo("Hello t:2000001 World");
    }

    [Test]
    public async Task Decode_Mixed_StatAndText_WithResolver_DecodesCorrectly()
    {
        // StatResolver("mpCon","80") resolves mpCon→"80"; for hpCon it returns the stat name ("hpCon").
        // The resolver receives the stat name without '#', so the unresolved stat appears as "hpCon".
        var resolver = new StatResolver("mpCon", "80");
        await Assert
            .That(MapleTextDecoder.Decode("Costs #mpCon MP and #hpCon HP", resolver))
            .IsEqualTo("Costs 80 MP and hpCon HP");
    }

    // ── Fast-path ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Decode_PlainText_ReturnsSameInstance()
    {
        const string input = "No markup here";
        await Assert.That(MapleTextDecoder.Decode(input)).IsSameReferenceAs(input);
    }

    [Test]
    public async Task Decode_Null_Throws()
    {
        await Assert.That(() => MapleTextDecoder.Decode(null!)).Throws<ArgumentNullException>();
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class EntityResolver : IMapleTextResolver
    {
        private readonly string _resolved;

        public EntityResolver(string resolved) => _resolved = resolved;

        public string ResolveEntity(string value) => _resolved;
    }

    private sealed class StatResolver : IMapleTextResolver
    {
        private readonly string _matchName;
        private readonly string _resolved;

        public StatResolver(string matchName, string resolved)
        {
            _matchName = matchName;
            _resolved = resolved;
        }

        public string ResolveStat(string statName) => statName == _matchName ? _resolved : statName;
    }

    private sealed class BlockResolver : IMapleTextResolver
    {
        private readonly string _resolved;

        public BlockResolver(string resolved) => _resolved = resolved;

        public string ResolveBlock(string value) => _resolved;
    }

    private sealed class ClientResolver : IMapleTextResolver
    {
        private readonly string _resolved;

        public ClientResolver(string resolved) => _resolved = resolved;

        public string ResolveClientToken(string value) => _resolved;
    }

    // ── UnterminatedEntity ────────────────────────────────────────────────────

    [Test]
    public async Task Decode_UnterminatedEntity_IsStripped()
    {
        // "#t0" — entity code 't' with digit but no closing '#' → UnterminatedEntity(#t) + Text(0).
        // The UnterminatedEntity token is stripped; only the trailing digit survives as Text.
        await Assert.That(MapleTextDecoder.Decode("#t0")).IsEqualTo("0");
    }

    [Test]
    public async Task Decode_UnterminatedBlock_IsStripped()
    {
        // "#D" alone — block code 'D' with no closing '#' → UnterminatedBlock only, no payload.
        // The UnterminatedBlock token is stripped and there is no remaining content.
        await Assert.That(MapleTextDecoder.Decode("#D")).IsEqualTo(string.Empty);
    }
}
