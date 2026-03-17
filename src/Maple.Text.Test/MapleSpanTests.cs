using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Tests for <see cref="MapleSpan"/> record — constructors, property access, and value equality.
/// </summary>
public sealed class MapleSpanTests
{
    // ── Constructor / property access ─────────────────────────────────────────

    [Test]
    public async Task Constructor_DefaultParams_HaveExpectedDefaults()
    {
        var span = new MapleSpan(MapleSpanKind.Text, "Hello");
        await Assert.That(span.Kind).IsEqualTo(MapleSpanKind.Text);
        await Assert.That(span.Text).IsEqualTo("Hello");
        await Assert.That(span.EntityId).IsEqualTo(0);
        await Assert.That(span.Color).IsEqualTo(MapleSpanColor.Default);
        await Assert.That(span.Bold).IsFalse();
    }

    [Test]
    public async Task Constructor_AllParams_SetsAllProperties()
    {
        var span = new MapleSpan(MapleSpanKind.ItemLink, "Iron Sword", 2000001, MapleSpanColor.Blue, Bold: true);
        await Assert.That(span.Kind).IsEqualTo(MapleSpanKind.ItemLink);
        await Assert.That(span.Text).IsEqualTo("Iron Sword");
        await Assert.That(span.EntityId).IsEqualTo(2000001);
        await Assert.That(span.Color).IsEqualTo(MapleSpanColor.Blue);
        await Assert.That(span.Bold).IsTrue();
    }

    [Test]
    public async Task Constructor_LineBreakKind_Works()
    {
        var span = new MapleSpan(MapleSpanKind.LineBreak, "\r\n");
        await Assert.That(span.Kind).IsEqualTo(MapleSpanKind.LineBreak);
        await Assert.That(span.Text).IsEqualTo("\r\n");
    }

    [Test]
    public async Task Constructor_LinkKinds_StoreEntityId()
    {
        var mob = new MapleSpan(MapleSpanKind.MobLink, "Slime", EntityId: 100100);
        var map = new MapleSpan(MapleSpanKind.MapLink, "Henesys", EntityId: 100000000);
        var npc = new MapleSpan(MapleSpanKind.NpcLink, "Maple Administrator", EntityId: 9000001);
        var skill = new MapleSpan(MapleSpanKind.SkillLink, "Arrow Blow", EntityId: 1001004);
        var icon = new MapleSpan(MapleSpanKind.Icon, string.Empty, EntityId: 4000001);

        await Assert.That(mob.EntityId).IsEqualTo(100100);
        await Assert.That(map.EntityId).IsEqualTo(100000000);
        await Assert.That(npc.EntityId).IsEqualTo(9000001);
        await Assert.That(skill.EntityId).IsEqualTo(1001004);
        await Assert.That(icon.EntityId).IsEqualTo(4000001);
    }

    [Test]
    public async Task Constructor_AllColors_Stored()
    {
        foreach (MapleSpanColor color in Enum.GetValues<MapleSpanColor>())
        {
            var span = new MapleSpan(MapleSpanKind.Text, "x", Color: color);
            await Assert.That(span.Color).IsEqualTo(color);
        }
    }

    // ── Record equality ───────────────────────────────────────────────────────

    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        var a = new MapleSpan(MapleSpanKind.Text, "Hello");
        var b = new MapleSpan(MapleSpanKind.Text, "Hello");
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task Equality_DifferentKind_AreNotEqual()
    {
        var a = new MapleSpan(MapleSpanKind.Text, "Hello");
        var b = new MapleSpan(MapleSpanKind.ItemLink, "Hello");
        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task Equality_DifferentText_AreNotEqual()
    {
        var a = new MapleSpan(MapleSpanKind.Text, "Hello");
        var b = new MapleSpan(MapleSpanKind.Text, "World");
        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    public async Task Equality_DifferentBold_AreNotEqual()
    {
        var a = new MapleSpan(MapleSpanKind.Text, "Hello", Bold: false);
        var b = new MapleSpan(MapleSpanKind.Text, "Hello", Bold: true);
        await Assert.That(a).IsNotEqualTo(b);
    }

    // ── With expression ───────────────────────────────────────────────────────

    [Test]
    public async Task WithExpression_ChangesKind()
    {
        var original = new MapleSpan(MapleSpanKind.Text, "Click me", EntityId: 1234);
        var linked = original with { Kind = MapleSpanKind.ItemLink };
        await Assert.That(linked.Kind).IsEqualTo(MapleSpanKind.ItemLink);
        await Assert.That(linked.Text).IsEqualTo("Click me");
        await Assert.That(linked.EntityId).IsEqualTo(1234);
    }

    [Test]
    public async Task WithExpression_MakesBold()
    {
        var plain = new MapleSpan(MapleSpanKind.Text, "Warning");
        var bold = plain with { Bold = true };
        await Assert.That(bold.Bold).IsTrue();
        await Assert.That(plain.Bold).IsFalse(); // original unchanged
    }
}
