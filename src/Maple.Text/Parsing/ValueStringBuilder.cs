using System.Buffers;
using System.Runtime.CompilerServices;

namespace Maple.Text.Parsing;

/// <summary>
/// Stack-allocated, ArrayPool-backed string builder for zero-allocation intermediate string assembly.
/// Uses a caller-supplied initial <see cref="Span{T}"/> (typically <c>stackalloc char[N]</c>);
/// graduates to a rented <see cref="ArrayPool{T}"/> buffer when the span is exhausted.
/// </summary>
internal ref struct ValueStringBuilder
{
    private char[]? _arrayFromPool;
    private Span<char> _chars;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        Length = 0;
    }

    public int Length { get; private set; }

    public void Append(char c)
    {
        if (Length >= _chars.Length)
            Grow(1);
        _chars[Length++] = c;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return;
        if (Length + value.Length > _chars.Length)
            Grow(value.Length);
        value.CopyTo(_chars[Length..]);
        Length += value.Length;
    }

    public void Append(string? value)
    {
        if (value is null)
            return;
        Append(value.AsSpan());
    }

    /// <summary>
    /// Materializes the accumulated characters into a new <see cref="string"/> and
    /// releases any rented buffer back to <see cref="ArrayPool{T}"/>.
    /// </summary>
    public override string ToString()
    {
        string s = new(_chars[..Length]);
        Dispose();
        return s;
    }

    public void Dispose()
    {
        char[]? toReturn = _arrayFromPool;
        _arrayFromPool = null;
        if (toReturn is not null)
            ArrayPool<char>.Shared.Return(toReturn);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        int newCapacity = Math.Max(Length + additionalCapacityBeyondPos, _chars.Length * 2);
        char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);
        _chars[..Length].CopyTo(newArray);
        char[]? toReturn = _arrayFromPool;
        _chars = _arrayFromPool = newArray;
        if (toReturn is not null)
            ArrayPool<char>.Shared.Return(toReturn);
    }
}
