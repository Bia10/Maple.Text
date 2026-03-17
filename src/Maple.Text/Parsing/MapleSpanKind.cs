namespace Maple.Text.Parsing;

/// <summary>
/// Classifies the visual role of a <see cref="MapleSpan"/>.
/// </summary>
public enum MapleSpanKind : byte
{
    /// <summary>Plain text run.</summary>
    Text,

    /// <summary>Hard line break (\r\n or \n in the source string).</summary>
    LineBreak,

    /// <summary>#t / #z — item name, navigable to the Items browser.</summary>
    ItemLink,

    /// <summary>#o — mob name, navigable to the Mobs browser.</summary>
    MobLink,

    /// <summary>#m — map name, navigable to the Maps browser.</summary>
    MapLink,

    /// <summary>#p — NPC name, navigable to the NPCs browser.</summary>
    NpcLink,

    /// <summary>#q — skill name, navigable to the Skills browser.</summary>
    SkillLink,

    /// <summary>
    /// #i — item icon placeholder. Consumers may suppress this, render a thumbnail,
    /// or fall back to a text link.
    /// </summary>
    Icon,
}
