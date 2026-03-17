using System.Runtime.InteropServices;

namespace Maple.Text.Parsing;

/// <summary>
/// The output of <see cref="MapleTextParser.Parse"/>: an ordered token sequence and an error flag.
/// </summary>
/// <remarks>
/// Obtain an instance only via <see cref="MapleTextParser.Parse"/>.
/// The <see cref="Tokens"/> list and <see cref="TokensSpan"/> span share the same backing array;
/// do not mutate the list after receiving this value.
/// </remarks>
public readonly struct MapleTextParseResult
{
    private readonly List<MapleTextToken> _tokens;

    internal MapleTextParseResult(List<MapleTextToken> tokens, bool hasErrors)
    {
        _tokens = tokens;
        HasErrors = hasErrors;
    }

    /// <summary>
    /// The parsed token sequence. Exposed as <see cref="IReadOnlyList{T}"/> to prevent external mutation.
    /// Prefer <see cref="TokensSpan"/> in performance-sensitive loops to avoid interface dispatch and iterator boxing.
    /// </summary>
    public IReadOnlyList<MapleTextToken> Tokens => _tokens;

    /// <summary>
    /// <see langword="true"/> when the input contained at least one unrecognised or malformed token.
    /// All tokens are still present in <see cref="Tokens"/>; error tokens carry
    /// <see cref="MapleTextTokenKind.UnknownCode"/>, <see cref="MapleTextTokenKind.UnterminatedEntity"/>,
    /// or <see cref="MapleTextTokenKind.UnterminatedBlock"/>.
    /// </summary>
    public bool HasErrors { get; }

    /// <summary>
    /// Zero-copy span over the underlying token list — no boxing, no enumerator allocation.
    /// Backed directly by the <see cref="List{T}"/>'s internal array via
    /// <see cref="CollectionsMarshal.AsSpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<MapleTextToken> TokensSpan => CollectionsMarshal.AsSpan(_tokens);
}
