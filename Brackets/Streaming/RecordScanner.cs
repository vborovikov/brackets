// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license.

namespace Brackets.Streaming;

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// A scanner that reads from a reassignable instance of <see cref="ReadOnlySequence{T}"/>.
/// </summary>
sealed class RecordScanner
{
    /// <summary>
    /// A buffer of written characters that have not yet been encoded.
    /// The <see cref="charBufferPosition"/> field tracks how many characters are represented in this buffer.
    /// </summary>
    private readonly char[] charBuffer = new char[RecordBuffer.DefaultBufferLength];

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

    /// <summary>
    /// The encoding to use while decoding bytes into characters.
    /// </summary>
    private Encoding? encoding;

    /// <summary>
    /// The decoder.
    /// </summary>
    private Decoder? decoder;

    /// <summary>
    /// The preamble for the <see cref="encoding"/> in use.
    /// </summary>
    private byte[]? encodingPreamble;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordScanner"/> class
    /// without associating it with an initial <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <param name="encoding"></param>
    /// <remarks>
    /// When using this constructor, call <see cref="Scan(ReadOnlySequence{byte}, Encoding)"/>
    /// to associate the instance with the initial byte sequence to be read.
    /// </remarks>
    public RecordScanner(Encoding encoding)
    {
        if (encoding is null)
            throw new ArgumentNullException(nameof(encoding));

        InitializeEncoding(encoding);
    }

    public SequencePosition SequencePosition => this.sequencePosition;

    /// <summary>
    /// Initializes or reinitializes this instance to read from a given <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    /// <param name="sequence">The sequence to read from.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void Scan(ReadOnlySequence<byte> sequence, Encoding? encoding = null)
    {
        this.sequence = sequence;
        this.sequencePosition = sequence.Start;

        this.charBufferPosition = 0;
        this.charBufferLength = 0;

        InitializeEncoding(encoding);

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
    }

    [MemberNotNull(nameof(encoding), nameof(decoder), nameof(encodingPreamble))]
    private void InitializeEncoding(Encoding? encoding)
    {
        if (this.encoding?.Equals(encoding) ?? false)
        {
            this.decoder!.Reset();
        }
        else
        {
            this.encoding = encoding ?? Encoding.Default;
            this.decoder = this.encoding.GetDecoder();
            this.encodingPreamble = this.encoding.GetPreamble();
        }
    }

    /// <summary>
    /// Clears references to the <see cref="ReadOnlySequence{T}"/> set by a prior call to <see cref="Scan(ReadOnlySequence{byte}, Encoding)"/>.
    /// </summary>
    public void Reset()
    {
        this.sequence = default;
        this.sequencePosition = default;
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

    public RecordScanResult TryScanRecord(Span<char> buffer, char opener, char closer, out int length)
    {
        var pos = 0;
        var enclosed = false;
        while (pos < buffer.Length)
        {
            var ch = Read();
            if (ch == -1) break;
            buffer[pos++] = (char)ch;

            if (ch == opener)
                enclosed = true;

            if (enclosed && ch == closer)
            {
                if (pos > (buffer.Length >> 2) * 3)
                {
                    // buffer is filled for more than 75%
                    length = pos;
                    return RecordScanResult.EndOfRecord;
                }

                enclosed = false;
            }
        }

        length = pos;
        if (pos > 0 && pos == buffer.Length)
        {
            return RecordScanResult.EndOfData;
        }
        return RecordScanResult.Empty;
    }

    public RecordScanResult TryScanRecord(Span<char> buffer, char encloser, ref bool ignoreLineBreak, out int length)
    {
        var pos = 0;
        var enclosed = ignoreLineBreak;
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
                ignoreLineBreak = false;
                return RecordScanResult.EndOfRecord;
            }
            buffer[pos++] = (char)ch;
        }

        length = pos;
        ignoreLineBreak = enclosed;
        if (pos > 0 && pos == buffer.Length)
        {
            return RecordScanResult.EndOfData;
        }
        return RecordScanResult.Empty;
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

            if (MemoryMarshal.TryGetArray(memory, out var segment))
            {
                this.decoder!.Convert(segment.Array!, segment.Offset, segment.Count, this.charBuffer, this.charBufferLength, this.charBuffer.Length - this.charBufferLength, flush: false, out var bytesUsed, out var charsUsed, out var completed);
                this.charBufferLength += charsUsed;
                this.sequencePosition = this.sequence.GetPosition(bytesUsed, this.sequencePosition);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
