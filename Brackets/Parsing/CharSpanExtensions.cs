namespace Brackets.Parsing;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

static class CharSpanExtensions
{
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
    /// Finds the last index of any character outside a quote.
    /// </summary>
    /// <param name="span">The input character span.</param>
    /// <param name="quoteChar">The quotation mark character.</param>
    /// <param name="insideQuotes">true if the search starts inside the quotes; otherwise, false.</param>
    /// <returns>The index of the last occurrence of any of the stop characters ouside a quote, or -1 if not found.</returns>
    public static int LastIndexOutsideQuotes(this ReadOnlySpan<char> span, char quoteChar, bool insideQuotes = false)
    {
        var len = span.Length;
        if (len == 0) return -1;

        ref char src = ref MemoryMarshal.GetReference(span);
        src = ref Unsafe.Add(ref src, len - 1);
        while (len > 0)
        {
            if (!insideQuotes)
            {
                return len - 1;
            }

            insideQuotes ^= src == quoteChar;
            src = ref Unsafe.Subtract(ref src, 1);
            --len;
        }

        return -1;
    }


    public static string Normalize(this string input, ReadOnlySpan<char> trimChars)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(Normalize(input.ToCharArray(), trimChars, []));
    }

    public static string Normalize(this string input, ReadOnlySpan<char> trimChars, ReadOnlySpan<char> fillChars)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(Normalize(input.ToCharArray(), trimChars, fillChars));
    }

    private static ReadOnlySpan<char> Normalize(Span<char> span, ReadOnlySpan<char> trimChars, ReadOnlySpan<char> fillChars)
    {
        var len = span.Length;
        if (len == 0) return [];

        ref char src = ref MemoryMarshal.GetReference(span);
        ref char dst = ref MemoryMarshal.GetReference(span);
        var trimmed = 0;
        var pos = 0;
        while (len > 0)
        {
            if (trimChars.Contains(src))
            {
                ++trimmed;
            }
            else
            {
                if (trimmed > 0 && pos > 0)
                {
                    if (fillChars.Length > 0)
                    {
                        ref char cur = ref MemoryMarshal.GetReference(fillChars);
                        ref char end = ref Unsafe.Add(ref cur, fillChars.Length);
                        while (Unsafe.IsAddressLessThan(ref cur, ref end) && trimmed > 0)
                        {
                            dst = cur;
                            cur = ref Unsafe.Add(ref cur, 1);
                            dst = ref Unsafe.Add(ref dst, 1);
                            ++pos; --trimmed;
                        }
                    }
                    else
                    {
                        dst = ' ';
                        dst = ref Unsafe.Add(ref dst, 1);
                        ++pos;
                    }
                }

                trimmed = 0;
                dst = src;
                dst = ref Unsafe.Add(ref dst, 1);
                ++pos;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return span[..pos];
    }

    private static ReadOnlySpan<char> Normalize(Span<char> span)
    {
        var len = span.Length;
        if (len == 0) return [];

        ref char src = ref MemoryMarshal.GetReference(span);
        ref char dst = ref MemoryMarshal.GetReference(span);
        var space = false;
        var pos = 0;
        while (len > 0)
        {
            if (char.IsWhiteSpace(src))
            {
                space = true;
            }
            else
            {
                if (space && pos > 0)
                {
                    dst = ' ';
                    dst = ref Unsafe.Add(ref dst, 1);
                    ++pos;
                }
                space = false;
                dst = src;
                dst = ref Unsafe.Add(ref dst, 1);
                ++pos;
            }

            src = ref Unsafe.Add(ref src, 1);
            --len;
        }

        return span[..pos];
    }
}
