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
    private int clear;

    public RecordBuffer()
        : this(DefaultBufferLength) { }

    public RecordBuffer(int bufferLength)
    {
        this.buffer = ArrayPool<char>.Shared.Rent(bufferLength);
        this.offset = 0;
        this.clear = 0;
    }

    public readonly bool CanMakeRecord => this.offset > this.clear;

    public readonly void Dispose()
    {
        var toReturn = this.buffer;
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    public void Offset(int requestedOffset, int requestedClear)
    {
        if (requestedClear < 0 || requestedOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(requestedClear));

        // clear + requestedClear .. offset + requestedOffset

        var newClear = this.clear + requestedClear;
        var newOffset = this.offset + requestedOffset;
        if (newClear > newOffset)
            throw new ArgumentOutOfRangeException(nameof(requestedClear));

        if (newClear == newOffset)
        {
            this.offset = 0;
            this.clear = 0;
        }
        else
        {
            Offset(requestedOffset);
            this.clear = newClear;
            Debug.WriteLine($"Record buffer start shifted to position {this.clear}");
        }
    }

    public void Offset(int requestedOffset)
    {
        if (requestedOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(requestedOffset));
        if (requestedOffset == 0)
            return;

        var newOffset = this.offset + requestedOffset;
        if (newOffset >= this.buffer.Length)
        {
            Grow(DefaultBufferLength);
        }

        this.offset = newOffset;
    }

    public readonly Span<char> AsSpan()
    {
        return this.buffer.AsSpan(this.offset..);
    }

    public readonly ReadOnlySpan<char> PreviewRecord(int length)
    {
        return this.buffer.AsSpan(this.clear..Math.Min(length + this.offset, this.buffer.Length));
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

        var start = this.clear;
        this.clear = 0;
        this.offset = 0;
        return this.buffer.AsSpan(start..Math.Min(length, this.buffer.Length));
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

        Debug.WriteLine($"Record buffer grown to {this.buffer.Length} characters");
    }
}
