namespace Brackets.Parsing;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

static class CharSpanExtensions
{
    private const string WhiteSpace = " ";

    /// <summary>
    /// Finds the index of any of the specified stop characters outside a quote.
    /// </summary>
    /// <param name="span">The input character span.</param>
    /// <param name="stopChars">The stop characters to search for.</param>
    /// <param name="quoteChars">The quotation mark characters to consider.</param>
    /// <returns>The index of the first occurrence of any of the stop characters outside a quote, or -1 if not found.</returns>
    public static int IndexOfAnyOutsideQuotes(this ReadOnlySpan<char> span, SearchValues<char> stopChars, ReadOnlySpan<char> quoteChars)
    {
        if (span.Length == 0) return -1;

        var quoteChar = quoteChars.Contains(span[0]) ? span[0] : quoteChars[0];
        return span.IndexOfAnyOutsideQuotes(stopChars, quoteChar);
    }

    /// <summary>
    /// Finds the index of any of the specified stop characters outside a quote.
    /// </summary>
    /// <param name="span">The input character span.</param>
    /// <param name="stopChars">The stop characters to search for.</param>
    /// <param name="quoteChar">The quotation mark character.</param>
    /// <param name="insideQuotes">true if the search starts inside the quotes; otherwise, false.</param>
    /// <returns>The index of the first occurrence of any of the stop characters outside a quote, or -1 if not found.</returns>
    public static int IndexOfAnyOutsideQuotes(this ReadOnlySpan<char> span, SearchValues<char> stopChars, char quoteChar, bool insideQuotes = false)
    {
        var len = span.Length;
        if (len == 0) return -1;

        ref char src = ref MemoryMarshal.GetReference(span);
        while (len > 0)
        {
            insideQuotes ^= src == quoteChar;
            if (!insideQuotes && stopChars.Contains(src))
            {
                return span.Length - len;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return -1;
    }

    /// <summary>
    /// Finds the last index of any of the specified stop characters outside a quote.
    /// </summary>
    /// <param name="span">The input character span.</param>
    /// <param name="stopChars">The stop characters to search for.</param>
    /// <param name="quoteChars">The quotation mark characters to consider.</param>
    /// <param name="insideQuotes">true if the search starts inside the quotes; otherwise, false.</param>
    /// <returns>The index of the last occurrence of any of the stop characters ouside a quote, or -1 if not found.</returns>
    public static int LastIndexOfAnyOutsideQuotes(this ReadOnlySpan<char> span, SearchValues<char> stopChars, ReadOnlySpan<char> quoteChars, bool insideQuotes = false)
    {
        if (span.Length == 0) return -1;

        var lastChar = span[^1];
        var quoteChar = quoteChars.Contains(lastChar) ? lastChar : quoteChars[0];
        if (insideQuotes && quoteChar == lastChar)
        {
            // change the quote char
            quoteChar = quoteChar == quoteChars[0] ? quoteChars[1] : quoteChars[0];
        }

        return span.LastIndexOfAnyOutsideQuotes(stopChars, quoteChar, insideQuotes);
    }

    /// <summary>
    /// Finds the last index of any of the specified stop characters outside a quote.
    /// </summary>
    /// <param name="span">The input character span.</param>
    /// <param name="stopChars">The stop characters to search for.</param>
    /// <param name="quoteChar">The quotation mark character.</param>
    /// <param name="insideQuotes">true if the search starts inside the quotes; otherwise, false.</param>
    /// <returns>The index of the last occurrence of any of the stop characters ouside a quote, or -1 if not found.</returns>
    public static int LastIndexOfAnyOutsideQuotes(this ReadOnlySpan<char> span, SearchValues<char> stopChars, char quoteChar, bool insideQuotes = false)
    {
        var len = span.Length;
        if (len == 0) return -1;

        ref char src = ref MemoryMarshal.GetReference(span);
        src = ref Unsafe.Add(ref src, len - 1);
        while (len > 0)
        {
            insideQuotes ^= src == quoteChar;
            if (!insideQuotes && stopChars.Contains(src))
            {
                return len - 1;
            }

            src = ref Unsafe.Subtract(ref src, 1);
            --len;
        }

        return -1;
    }

    /// <summary>
    ///  Any consecutive white-space (including tabs, newlines) is replaced with whatever is in normalizeTo.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="whiteSpace">White-space characters.</param>
    /// <param name="normalizeTo">Character which is replacing whitespace.</param>
    /// <remarks>Based on http://stackoverflow.com/a/25023688/897326 </remarks>
    public static string NormalizeWhiteSpace(this ReadOnlySpan<char> input,
        ReadOnlySpan<char> whiteSpace, ReadOnlySpan<char> normalizeTo)
    {
        if (input.IsEmpty)
        {
            return string.Empty;
        }

        var output = new StringBuilder();
        var skipped = false;

        for (var i = 0; i != input.Length; ++i)
        {
            if (whiteSpace.Contains(input[i]))
            {
                if (!skipped)
                {
                    output.Append(normalizeTo);
                    skipped = true;
                }
            }
            else
            {
                skipped = false;
                output.Append(input[i]);
            }
        }

        return output.ToString();
    }

    public static string NormalizeWhiteSpace(this string input,
        ReadOnlySpan<char> whiteSpace, string normalizeTo = WhiteSpace) =>
        NormalizeWhiteSpace(input.AsSpan(), whiteSpace, normalizeTo);
}
