using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Tests for the default interface implementations on <see cref="IMapleTextResolver"/>.
/// Covers: all four simple string defaults, ResolveEntitySpan heap path, and both
/// payload-empty and heap paths for ResolveClientTokenSpan and ResolveBlockSpan.
/// </summary>
public sealed class IMapleTextResolverDefaultImplTests
{
    // ── Simple string-based defaults ──────────────────────────────────────────
    // These are only exercised when a concrete class uses the default without overriding.

    [Test]
    public async Task ResolveStat_Default_ReturnsInputUnchanged()
    {
        IMapleTextResolver resolver = new DefaultResolver();
        await Assert.That(resolver.ResolveStat("mpCon")).IsEqualTo("mpCon");
    }

    [Test]
    public async Task ResolveEntity_Default_ReturnsInputUnchanged()
    {
        IMapleTextResolver resolver = new DefaultResolver();
        await Assert.That(resolver.ResolveEntity("t:2000001")).IsEqualTo("t:2000001");
    }

    [Test]
    public async Task ResolveClientToken_Default_ReturnsEmpty()
    {
        IMapleTextResolver resolver = new DefaultResolver();
        await Assert.That(resolver.ResolveClientToken("h0")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolveBlock_Default_ReturnsEmpty()
    {
        IMapleTextResolver resolver = new DefaultResolver();
        await Assert.That(resolver.ResolveBlock("Dkey")).IsEqualTo(string.Empty);
    }

    // ── ResolveStatSpan default ───────────────────────────────────────────────

    [Test]
    public async Task ResolveStatSpan_Default_DelegatesToResolveStat()
    {
        IMapleTextResolver resolver = new DefaultResolver();
        // Default delegates to ResolveStat(statName.ToString()) which returns input unchanged.
        await Assert.That(resolver.ResolveStatSpan("pad".AsSpan())).IsEqualTo("pad");
    }

    // ── ResolveEntitySpan — heap allocation path (payload.Length > 126) ───────

    [Test]
    public async Task ResolveEntitySpan_ShortPayload_UsesStackalloc()
    {
        // payload.Length = 7, len = 9 ≤ 128 — stackalloc path.
        IMapleTextResolver resolver = new DefaultResolver();
        string result = resolver.ResolveEntitySpan('t', "2000001".AsSpan());
        await Assert.That(result).IsEqualTo("t:2000001");
    }

    [Test]
    public async Task ResolveEntitySpan_LongPayload_UsesHeapAllocation()
    {
        // payload.Length = 127, len = 129 > 128 — heap allocation path.
        IMapleTextResolver resolver = new DefaultResolver();
        string longPayload = new string('1', 127);
        string result = resolver.ResolveEntitySpan('t', longPayload.AsSpan());
        await Assert.That(result).IsEqualTo("t:" + longPayload);
    }

    // ── ResolveClientTokenSpan — all three branches ───────────────────────────

    [Test]
    public async Task ResolveClientTokenSpan_EmptyPayload_UsesCodeOnly()
    {
        // payload.IsEmpty → calls ResolveClientToken with just the code char.
        IMapleTextResolver resolver = new DefaultResolver();
        string result = resolver.ResolveClientTokenSpan('h', ReadOnlySpan<char>.Empty);
        // Default ResolveClientToken returns string.Empty regardless of input.
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolveClientTokenSpan_ShortPayload_UsesStackalloc()
    {
        // payload.Length = 1, len = 2 ≤ 64 — stackalloc path.
        IMapleTextResolver resolver = new DefaultResolver();
        string result = resolver.ResolveClientTokenSpan('h', "0".AsSpan());
        await Assert.That(result).IsEqualTo(string.Empty); // default ResolveClientToken returns ""
    }

    [Test]
    public async Task ResolveClientTokenSpan_LongPayload_UsesHeapAllocation()
    {
        // payload.Length = 64, len = 65 > 64 — heap allocation path.
        IMapleTextResolver resolver = new DefaultResolver();
        string longPayload = new string('x', 64);
        string result = resolver.ResolveClientTokenSpan('h', longPayload.AsSpan());
        await Assert.That(result).IsEqualTo(string.Empty); // default ResolveClientToken returns ""
    }

    // ── ResolveBlockSpan — all three branches ─────────────────────────────────

    [Test]
    public async Task ResolveBlockSpan_EmptyPayload_UsesCodeOnly()
    {
        // payload.IsEmpty → calls ResolveBlock with just the code char.
        IMapleTextResolver resolver = new DefaultResolver();
        string result = resolver.ResolveBlockSpan('D', ReadOnlySpan<char>.Empty);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolveBlockSpan_ShortPayload_UsesStackalloc()
    {
        // payload.Length = 3, len = 4 ≤ 64 — stackalloc path.
        IMapleTextResolver resolver = new DefaultResolver();
        string result = resolver.ResolveBlockSpan('D', "key".AsSpan());
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolveBlockSpan_LongPayload_UsesHeapAllocation()
    {
        // payload.Length = 64, len = 65 > 64 — heap allocation path.
        IMapleTextResolver resolver = new DefaultResolver();
        string longPayload = new string('k', 64);
        string result = resolver.ResolveBlockSpan('D', longPayload.AsSpan());
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    // ── Via Decode: default impls invoked for all four token kinds ────────────

    [Test]
    public async Task Decode_WithDefaultResolver_StatToken_ReturnsStatName()
    {
        // DefaultResolver.ResolveStat returns the stat name unchanged, just like no-resolver.
        string result = MapleTextDecoder.Decode("#mpCon", new DefaultResolver());
        await Assert.That(result).IsEqualTo("mpCon");
    }

    [Test]
    public async Task Decode_WithDefaultResolver_EntityRef_ReturnsCodeColonId()
    {
        // DefaultResolver.ResolveEntity returns its "code:payload" input unchanged.
        string result = MapleTextDecoder.Decode("#t2000001#", new DefaultResolver());
        await Assert.That(result).IsEqualTo("t:2000001");
    }

    [Test]
    public async Task Decode_WithDefaultResolver_ClientToken_ReturnsEmpty()
    {
        string result = MapleTextDecoder.Decode("Hello #h0", new DefaultResolver());
        await Assert.That(result).IsEqualTo("Hello ");
    }

    [Test]
    public async Task Decode_WithDefaultResolver_BlockToken_ReturnsEmpty()
    {
        string result = MapleTextDecoder.Decode("Time: #Dkey#", new DefaultResolver());
        await Assert.That(result).IsEqualTo("Time: ");
    }

    [Test]
    public async Task Decode_WithDefaultResolver_EmptyClientPayload_CallsEmptyPath()
    {
        // "#h#" parses to ClientToken(Code='h', PayloadLength=0).
        // ResolveClientTokenSpan('h', Empty) → payload.IsEmpty branch.
        string result = MapleTextDecoder.Decode("#h#", new DefaultResolver());
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Decode_WithDefaultResolver_EmptyBlockPayload_CallsEmptyPath()
    {
        // "#D#" parses to Block(Code='D', PayloadLength=0).
        // ResolveBlockSpan('D', Empty) → payload.IsEmpty branch.
        string result = MapleTextDecoder.Decode("#D#", new DefaultResolver());
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    // ── Test double ───────────────────────────────────────────────────────────

    /// <summary>Uses all default implementations — no overrides.</summary>
    private sealed class DefaultResolver : IMapleTextResolver { }
}
