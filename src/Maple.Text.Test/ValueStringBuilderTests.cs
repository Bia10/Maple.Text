using Maple.Text.Parsing;

namespace Maple.Text.Test;

/// <summary>
/// Tests for <see cref="ValueStringBuilder"/> covering the branches not reachable
/// through the public API: <c>Append(char)</c> with buffer growth, <c>Append(string?)</c>
/// with null, and the double-Grow path that returns the first rented array.
/// </summary>
public sealed class ValueStringBuilderTests
{
    // ── Append(char) triggers Grow ────────────────────────────────────────────

    [Test]
    public async Task AppendChar_ExceedsInitialBuffer_GrowsAndPreservesContent()
    {
        // Static local avoids async state-machine capture of ref struct.
        static string Build()
        {
            var vsb = new ValueStringBuilder(new char[4]);
            vsb.Append("ABCD"); // fills the 4-char initial buffer exactly
            vsb.Append('E'); // Length(4) >= _chars.Length(4) → Grow(1) executed
            return vsb.ToString();
        }

        await Assert.That(Build()).IsEqualTo("ABCDE");
    }

    // ── Append(string?) with null ─────────────────────────────────────────────

    [Test]
    public async Task AppendString_Null_IsIgnored()
    {
        static string Build()
        {
            var vsb = new ValueStringBuilder(new char[16]);
            vsb.Append("before");
            vsb.Append((string?)null); // early-return branch
            vsb.Append("after");
            return vsb.ToString();
        }

        await Assert.That(Build()).IsEqualTo("beforeafter");
    }

    // ── Grow called twice — second Grow returns the first rented array ────────

    [Test]
    public async Task Grow_CalledTwice_ReturnsPreviousPoolBuffer()
    {
        // First Grow: _arrayFromPool was null → toReturn is null → no Return call.
        // Second Grow: _arrayFromPool != null → toReturn != null → Return(toReturn) executed (line 76).
        static string Build()
        {
            var vsb = new ValueStringBuilder(new char[4]);
            vsb.Append("ABCD"); // fill initial buffer
            vsb.Append('E'); // first Grow(1) → rents pool buffer (≥8 chars)
            vsb.Append("FGHIJKLMNOPQR"); // 13 chars: 5+13=18 > 8 → second Grow triggers Return of first rental
            return vsb.ToString();
        }

        await Assert.That(Build()).IsEqualTo("ABCDEFGHIJKLMNOPQR");
    }
}
