namespace Maple.Text.Parsing;

/// <summary>
/// Kinds of entity/reference links supported by the MapleText format.
/// </summary>
public enum MapleTextLinkType : byte
{
    // ── Entity references (EntityReference tokens, #X<digits>#) ────────────

    /// <summary>#t&lt;id&gt;# — item name.</summary>
    ItemName,

    /// <summary>#z&lt;id&gt;# — item name (alternate form).</summary>
    ItemNameAlt,

    /// <summary>#i&lt;id&gt;# — item icon.</summary>
    ItemIcon,

    /// <summary>#i&lt;id&gt;:# — item icon with colon suffix (demandSummary display format).</summary>
    ItemIconSlot,

    /// <summary>#o&lt;id&gt;# — mob name.</summary>
    MobName,

    /// <summary>#m&lt;id&gt;# — map name.</summary>
    MapName,

    // ── Long-form client tokens (ClientToken tokens, no closing #) ──────────

    /// <summary>#p&lt;id&gt; — NPC name (resolved via CNpcTemplate).</summary>
    NpcName,

    /// <summary>#h0 / #h1 / #h2 / #h3 — character name with optional josa suffix.</summary>
    CharacterName,

    /// <summary>#q&lt;id&gt; — skill name (resolved by skill ID).</summary>
    SkillName,

    /// <summary>#M&lt;id&gt; — quest mob name (resolved via CWvsContext::GetQuestMobName).</summary>
    QuestMobName,

    /// <summary>
    /// #@&lt;id&gt; — labeled NPC string (resolved via get_labeled_string with StringPool[1649]).
    /// The NPC template ID is stored in CT_INFO+60 (nNpcNo).
    /// </summary>
    LabeledNpcString,
}
