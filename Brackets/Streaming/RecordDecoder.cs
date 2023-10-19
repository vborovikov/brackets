// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license.

namespace Brackets.Streaming;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Represents the result of scanning a record.
/// </summary>
public enum RecordReadResult
{
    /// <summary>
    /// Indicates that the internal buffer is empty and the record scanning might not be complete.
    /// </summary>
    Empty,
    /// <summary>
    /// Indicates that the record scanning is complete.
    /// </summary>
    EndOfRecord,
    /// <summary>
    /// Indicates that the end of a record buffer has been reached.
    /// </summary>
    EndOfData,
}

/// <summary>
/// A scanner that reads from a reassignable instance of <see cref="ReadOnlySequence{T}"/>.
/// </summary>
sealed class RecordDecoder : IDisposable
{
    /// <summary>
    /// A buffer of written characters that have not yet been encoded.
    /// The <see cref="charBufferPosition"/> field tracks how many characters are represented in this buffer.
    /// </summary>
    private readonly char[] charBuffer;

    /// <summary>
    /// The number of characters already read from <see cref="charBuffer"/>.
    /// </summary>
    private int charBufferPosition;

    /// <summary>
    /// The number of characters decoded into <see cref="charBuffer"/>.
    /// </summary>
    private int charBufferLength;

    /// <summary>
    /// The sequence to be decoded and read.
    /// </summary>
    private ReadOnlySequence<byte> sequence;

    /// <summary>
    /// The position of the next byte to decode in <see cref="sequence"/>.
    /// </summary>
    private SequencePosition sequencePosition;

    private SequencePosition redoPosition;

    /// <summary>
    /// The encoding to use while decoding bytes into characters.
    /// </summary>
    private Encoding encoding;

    /// <summary>
    /// The decoder.
    /// </summary>
    private Decoder decoder;

    /// <summary>
    /// The preamble for the <see cref="encoding"/> in use.
    /// </summary>
    private byte[] encodingPreamble;

    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordDecoder"/> class
    /// without associating it with an initial <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <param name="encoding"></param>
    /// <param name="bufferLength"></param>
    /// <remarks>
    /// When using this constructor, call <see cref="Decode(ReadOnlySequence{byte}, Encoding)"/>
    /// to associate the instance with the initial byte sequence to be read.
    /// </remarks>
    public RecordDecoder(Encoding encoding, int bufferLength)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        this.charBuffer = ArrayPool<char>.Shared.Rent(bufferLength);
        this.encoding = encoding;
        this.decoder = encoding.GetDecoder();
        this.encodingPreamble = encoding.GetPreamble();
    }

    public SequencePosition SequencePosition => this.sequencePosition;

    public void Dispose()
    {
        if (!this.isDisposed)
        {
            ArrayPool<char>.Shared.Return(this.charBuffer);
            this.isDisposed = true;
        }
    }

    /// <summary>
    /// Initializes or reinitializes this instance to read from a given <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <param name="sequence">The sequence to read from.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void Decode(ReadOnlySequence<byte> sequence, Encoding? encoding = null)
    {
        this.sequence = sequence;
        this.sequencePosition = sequence.Start;

        this.charBufferPosition = 0;
        this.charBufferLength = 0;

        TryChangeEncoding(encoding);

        // Skip a preamble if we encounter one.
        if (this.encodingPreamble.Length > 0 && sequence.Length >= this.encodingPreamble.Length)
        {
            Span<byte> provisionalRead = stackalloc byte[this.encodingPreamble.Length];
            sequence.Slice(0, this.encodingPreamble.Length).CopyTo(provisionalRead);
            var match = true;
            for (var i = 0; match && i < this.encodingPreamble.Length; i++)
            {
                match = this.encodingPreamble[i] == provisionalRead[i];
            }

            if (match)
            {
                // We encountered a preamble. Skip it.
                this.sequencePosition = this.sequence.GetPosition(this.encodingPreamble.Length, this.sequence.Start);
            }
        }

        this.redoPosition = this.sequencePosition;
    }

    public void TryChangeEncoding(Encoding? encoding, bool forceDecode = false)
    {
        if (encoding is null || this.encoding.Equals(encoding))
            return;

        this.encoding = encoding;
        this.decoder = this.encoding.GetDecoder();
        this.encodingPreamble = this.encoding.GetPreamble();

        // force re-decoding the current buffer
        if (forceDecode)
        {
            var currentCharBufferPosition = this.charBufferPosition;
            this.charBufferPosition = 0;
            this.charBufferLength = 0;
            this.sequencePosition = this.redoPosition;
            DecodeCharsIfNecessary();
            this.charBufferPosition = currentCharBufferPosition;
        }
    }

    /// <summary>
    /// Clears references to the <see cref="ReadOnlySequence{T}"/> set by a prior call to <see cref="Decode(ReadOnlySequence{byte}, Encoding)"/>.
    /// </summary>
    public void Reset()
    {
        this.sequence = default;
        this.sequencePosition = default;
        this.redoPosition = default;
    }

    private int Peek()
    {
        DecodeCharsIfNecessary();
        if (this.charBufferPosition == this.charBufferLength)
        {
            return -1;
        }

        return this.charBuffer[this.charBufferPosition];
    }

    private int Read()
    {
        var result = Peek();
        if (result != -1)
        {
            this.charBufferPosition++;
        }

        return result;
    }

    public int Read(Span<char> buffer)
    {
        DecodeCharsIfNecessary();

        var copied = Math.Min(buffer.Length, this.charBufferLength - this.charBufferPosition);
        this.charBuffer.AsSpan(this.charBufferPosition, copied).CopyTo(buffer);
        this.charBufferPosition += copied;
        return copied;
    }

    public RecordReadResult TryReadRecord(Span<char> buffer, char opener, char closer, ref bool enclosed, out int length)
    {
        DecodeCharsIfNecessary();

        length = Math.Min(buffer.Length, this.charBufferLength - this.charBufferPosition);
        if (length == 0)
            return RecordReadResult.Empty;

        var data = this.charBuffer.AsSpan(this.charBufferPosition, length);
        if (enclosed)
        {
            length = data.IndexOf(closer);
            if (length >= 0)
            {
                enclosed = false;
                data[..++length].CopyTo(buffer);
                this.charBufferPosition += length;

                return RecordReadResult.EndOfRecord;
            }
        }
        else
        {
            length = data.IndexOf(opener);
            if (length >= 0)
            {
                enclosed = true;
                var closerPos = data[length..].IndexOf(closer);
                if (closerPos > 0)
                {
                    enclosed = false;
                    length += closerPos;
                    data[..++length].CopyTo(buffer);
                    this.charBufferPosition += length;

                    return RecordReadResult.EndOfRecord;
                }
            }
        }

        data.CopyTo(buffer);
        length = data.Length;
        this.charBufferPosition += length;

        return RecordReadResult.EndOfData;
    }

    public RecordReadResult TryReadLine(Span<char> buffer, char encloser, ref bool enclosed, out int length)
    {
        var pos = 0;
        while (pos < buffer.Length)
        {
            var ch = Read();
            if (ch == -1) break;
            enclosed ^= ch == encloser;
            if (!enclosed && (ch == '\r' || ch == '\n'))
            {
                if (ch == '\r' && Peek() == '\n')
                {
                    Read();
                }

                length = pos;
                return RecordReadResult.EndOfRecord;
            }
            buffer[pos++] = (char)ch;
        }

        length = pos;
        if (pos > 0 && pos == buffer.Length)
        {
            return RecordReadResult.EndOfData;
        }
        return RecordReadResult.Empty;
    }

    private void DecodeCharsIfNecessary()
    {
        if (this.charBufferPosition == this.charBufferLength && !this.sequence.End.Equals(this.sequencePosition))
        {
            DecodeChars();
        }
    }

    private void DecodeChars()
    {
        if (this.charBufferPosition == this.charBufferLength)
        {
            // Reset to consider our character buffer empty.
            this.charBufferPosition = 0;
            this.charBufferLength = 0;
            this.redoPosition = this.sequencePosition;
        }

        // Continue to decode characters for as long as we have room for TWO chars, since Decoder.Convert throws
        // if we provide a char[1] for output when it encounters a surrogate pair.
        while (this.charBuffer.Length - this.charBufferLength >= 2)
        {
            this.sequence.TryGet(ref this.sequencePosition, out var memory, advance: false);
            if (memory.IsEmpty)
            {
                this.sequencePosition = this.sequence.End;
                break;
            }

            this.decoder.Convert(memory.Span, this.charBuffer.AsSpan(this.charBufferLength), flush: false, out var bytesUsed, out var charsUsed, out _);
            this.charBufferLength += charsUsed;
            this.sequencePosition = this.sequence.GetPosition(bytesUsed, this.sequencePosition);
        }
    }
}
