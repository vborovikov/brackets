namespace Brackets.Streaming;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

struct RecordBuffer : IDisposable
{
    public const int DefaultBufferLength = 4096;

    private char[] buffer;
    private int offset;

    public RecordBuffer()
        : this(DefaultBufferLength) { }

    public RecordBuffer(int bufferLength)
    {
        this.buffer = ArrayPool<char>.Shared.Rent(bufferLength);
        this.offset = 0;
    }

    public readonly bool CanMakeRecord => this.offset > 0;

    public readonly void Dispose()
    {
        var toReturn = this.buffer;
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    public void Clear()
    {
        this.offset = 0;
    }

    public int Offset(int requestedOffset)
    {
        if (requestedOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(requestedOffset));

        if (requestedOffset == 0)
            return this.offset;

        var newOffset = 0;
        if (requestedOffset > 0)
        {
            newOffset = this.offset + requestedOffset;
            if (newOffset >= this.buffer.Length)
            {
                Grow(DefaultBufferLength);
            }
        }

        this.offset = newOffset;
        return this.offset;
    }

    public readonly Span<char> AsSpan()
    {
        return this.buffer.AsSpan(this.offset..);
    }

    public readonly ReadOnlySpan<char> PreviewRecord(int length)
    {
        return this.buffer.AsSpan(..Math.Min(length + this.offset, this.buffer.Length));
    }

    public ReadOnlySpan<char> MakeRecord(int length = -1)
    {
        if (length < 0)
        {
            length = this.offset;
        }
        else
        {
            length += this.offset;
        }

        this.offset = 0;
        return this.buffer.AsSpan(..Math.Min(length, this.buffer.Length));
    }

    public static implicit operator Span<char>(RecordBuffer buffer) => buffer.AsSpan();

    /// <summary>
    /// Resize the internal buffer either by increasing current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondLength"/> to
    /// <see cref="offset"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondLength">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondLength)
    {
        Debug.Assert(additionalCapacityBeyondLength > 0);

        // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative
        var poolArray = ArrayPool<char>.Shared.Rent((int)Math.Max((uint)(this.buffer.Length + additionalCapacityBeyondLength),
            (uint)this.buffer.Length + DefaultBufferLength));

        Array.Copy(this.buffer, poolArray, this.buffer.Length);

        var toReturn = this.buffer;
        this.buffer = poolArray;
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }

        Debug.WriteLine($"Record buffer grown to {this.buffer.Length} characters.");
    }
}
