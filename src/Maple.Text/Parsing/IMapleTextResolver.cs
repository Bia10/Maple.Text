namespace Maple.Text.Parsing;

/// <summary>
/// Resolves MapleText token values to human-readable strings during decoding.
/// Implement this interface to provide game-data lookups (e.g. item names, stat formulas)
/// to <see cref="MapleTextDecoder.Decode"/>.
/// <para>
/// All methods have default implementations that return the raw value unchanged (or an empty
/// string for tokens that carry no display text). Override only the methods relevant to your use case.
/// </para>
/// <para>
/// For zero-allocation hot paths, override the <c>…Span</c> variants; the default implementations
/// delegate to the string overloads by building a short stack-allocated key with no heap allocation.
/// </para>
/// </summary>
public interface IMapleTextResolver
{
    // ── String-based overloads ────────────────────────────────────────────────

    /// <summary>
    /// Resolves a stat token name (e.g. <c>"mpCon"</c>) to its display value (e.g. <c>"100"</c>).
    /// Return <paramref name="statName"/> unchanged when no value is available.
    /// </summary>
    string ResolveStat(string statName) => statName;

    /// <summary>
    /// Resolves an entity reference key (formatted as <c>"code:id"</c>, e.g. <c>"t:2000001"</c>)
    /// to its display name. Return <paramref name="value"/> unchanged when no value is available.
    /// </summary>
    string ResolveEntity(string value) => value;

    /// <summary>
    /// Resolves a client token value to its display string.
    /// Return <see cref="string.Empty"/> to suppress the token from the output.
    /// </summary>
    string ResolveClientToken(string value) => string.Empty;

    /// <summary>
    /// Resolves a block token value to its display string.
    /// Return <see cref="string.Empty"/> to suppress the token from the output.
    /// </summary>
    string ResolveBlock(string value) => string.Empty;

    // ── Span-based overloads ──────────────────────────────────────────────────
    // Override these for zero-alloc resolution (e.g. FrozenDictionary.GetAlternateLookup).
    // Default implementations fall back to the string overloads with a single intermediate
    // string built on the stack — no heap allocation until the resolver itself is called.

    /// <summary>Override to resolve a stat name without allocating an intermediate string.</summary>
    string ResolveStatSpan(ReadOnlySpan<char> statName) => ResolveStat(statName.ToString());

    /// <summary>Override to resolve an entity reference without allocating an intermediate key string.</summary>
    string ResolveEntitySpan(char code, ReadOnlySpan<char> payload)
    {
        // "code:payload" — entity IDs are short (≤ 20 digits), so stackalloc is safe.
        int len = payload.Length + 2;
        if (len <= 128)
        {
            Span<char> scratch = stackalloc char[len];
            scratch[0] = code;
            scratch[1] = ':';
            payload.CopyTo(scratch[2..]);
            return ResolveEntity(new string(scratch));
        }
        // Rare very-long payload — single heap allocation.
        char[] arr = new char[len];
        arr[0] = code;
        arr[1] = ':';
        payload.CopyTo(arr.AsSpan(2));
        return ResolveEntity(new string(arr));
    }

    /// <summary>Override to resolve a client token without allocating an intermediate key string.</summary>
    string ResolveClientTokenSpan(char code, ReadOnlySpan<char> payload)
    {
        if (payload.IsEmpty)
        {
            Span<char> one = stackalloc char[1] { code };
            return ResolveClientToken(new string(one));
        }
        int len = payload.Length + 1;
        if (len <= 64)
        {
            Span<char> scratch = stackalloc char[len];
            scratch[0] = code;
            payload.CopyTo(scratch[1..]);
            return ResolveClientToken(new string(scratch));
        }
        char[] arr = new char[len];
        arr[0] = code;
        payload.CopyTo(arr.AsSpan(1));
        return ResolveClientToken(new string(arr));
    }

    /// <summary>Override to resolve a block token without allocating an intermediate key string.</summary>
    string ResolveBlockSpan(char code, ReadOnlySpan<char> payload)
    {
        if (payload.IsEmpty)
        {
            Span<char> one = stackalloc char[1] { code };
            return ResolveBlock(new string(one));
        }
        int len = payload.Length + 1;
        if (len <= 64)
        {
            Span<char> scratch = stackalloc char[len];
            scratch[0] = code;
            payload.CopyTo(scratch[1..]);
            return ResolveBlock(new string(scratch));
        }
        char[] arr = new char[len];
        arr[0] = code;
        payload.CopyTo(arr.AsSpan(1));
        return ResolveBlock(new string(arr));
    }
}
