namespace Maple.Text.Parsing;

/// <summary>
/// A fully-resolved, display-ready token produced after parsing and entity resolution.
/// <para>
/// All state (color, bold) is pre-computed so consumers can render each span
/// without any stateful traversal. Entity names are also pre-resolved using
/// the catalog context passed at build time.
/// </para>
/// </summary>
/// <param name="Kind">Visual role of this span.</param>
/// <param name="Text">Display text (already resolved for link spans).</param>
/// <param name="EntityId">Template ID for navigable link kinds; 0 otherwise.</param>
/// <param name="Color">
/// Active color at this span's position. <see cref="MapleSpanColor.Default"/> means
/// no color override (inherit the surrounding foreground).
/// </param>
/// <param name="Bold">Whether this span is within a bold (#e) region.</param>
public sealed record MapleSpan(
    MapleSpanKind Kind,
    string Text,
    int EntityId = 0,
    MapleSpanColor Color = MapleSpanColor.Default,
    bool Bold = false
);
