namespace Brackets.Html;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Parsing;

public readonly struct HtmlLexer : IMarkupLexer
{
    private const StringComparison cmp = StringComparison.OrdinalIgnoreCase;
    private const char Opener = '<';
    private const char Closer = '>';
    private const char Terminator = '/';
    private const char AltOpener = '!';
    private const char DataOpener = '[';
    private const char ValueSeparator = '=';
    private const string QuotationMarks = "'\"";
    private const string Separators = " \r\n\t\xA0";
    private const string NameSeparators = "/" + Separators;
    private const string AttrSeparators = "=" + Separators;
    private const string CommentOpener = "<!--";
    private const string CommentCloser = "-->";
    private const string SectionOpener = "<![";
    private const string SectionCloser = "]]>";

    public StringComparison Comparison => cmp;
    char IMarkupLexer.Opener => Opener;
    char IMarkupLexer.Closer => Closer;

    public Token GetElementToken(ReadOnlySpan<char> text, int globalOffset)
    {
        if (text.IsEmpty)
            return default;

        var start = text.IndexOf(Opener);
        if (start < 0)
        {
            return new(TokenCategory.Content, text, globalOffset);
        }
        if (start > 0)
        {
            return new(TokenCategory.Content, text[..start], globalOffset);
        }
        var end = text.IndexOf(Closer) + 1;
        if (end <= 0)
        {
            return new(TokenCategory.Discarded, text, globalOffset);
        }

        var category = TokenCategory.Content;
        var span = text[start..end];
        if (span.Length <= 2)
        {
            return new(category, span, globalOffset);
        }
        var name = ReadOnlySpan<char>.Empty;
        var nameOffset = 0;
        var data = ReadOnlySpan<char>.Empty;
        var dataOffset = 0;

        if (span.StartsWith(SectionOpener, cmp))
        {
            // <![CDATA[...]]>
            if (!span.EndsWith(SectionCloser, cmp))
            {
                var sectionCloserPos = text[start..].IndexOf(SectionCloser, cmp);
                if (sectionCloserPos > 0)
                {
                    end = sectionCloserPos + SectionCloser.Length;
                }
                else
                {
                    // section is incorrect, let's try not to discard it all as a content
                    var anotherOpenerPos = text[(start + 1)..].IndexOf(Opener);
                    if (anotherOpenerPos > 0)
                    {
                        end = start + anotherOpenerPos + 1;
                        span = text[start..end];
                    }

                    return new(TokenCategory.Discarded, span, globalOffset + start);
                }
            }

            // section token
            // CDATA[...]]>
            span = text[start..end];
            var section = text[(start + SectionOpener.Length)..end];
            var dataPos = section.IndexOf(DataOpener);
            if (dataPos <= 0)
            {
                // section is incorrect
                return new(TokenCategory.Discarded, span, globalOffset + start);
            }

            category = TokenCategory.Section;
            name = section[..dataPos];
            nameOffset = SectionOpener.Length;
            data = section[(dataPos + 1)..^SectionCloser.Length];
            dataOffset = SectionOpener.Length + dataPos + 1;

            return new(category,
                span, globalOffset + start,
                name, globalOffset + nameOffset,
                data, globalOffset + dataOffset);
        }
        else if (span.StartsWith(CommentOpener, cmp))
        {
            // <!--...-->
            category = TokenCategory.Comment;
            if (!span.EndsWith(CommentCloser, cmp))
            {
                var commentCloserPos = text[start..].IndexOf(CommentCloser, cmp);
                if (commentCloserPos > 0)
                {
                    end = commentCloserPos + CommentCloser.Length;
                }
                else
                {
                    //todo: Discarded?
                    end = text.Length;
                }

                span = text[start..end];
            }

            // comment token
            return new(category, span, globalOffset + start);
        }
        else
        {
            // <?xml...?> | <tag .../> | <tag ...> | </tag>
            var tag = span[1..^1];
            var terminatorPos =
                tag[0] == Terminator ? 0 :
                tag[^1] == Terminator ? tag.Length - 1 :
                -1;
            var separatorPos = tag.IndexOfAny(Separators);
            var nameStart = terminatorPos == 0 ? 1 : 0;
            var nameEnd =
                separatorPos > 0 ? separatorPos :
                terminatorPos > 0 ? terminatorPos :
                ^0;
            var tagName = tag[nameStart..nameEnd];

            if (IsElementName(tagName) || (tagName[0] == AltOpener && IsElementName(tagName[1..])))
            {
                // tag name
                name = tagName;
                nameOffset = start + nameStart + 1;

                // token category
                category = terminatorPos switch
                {
                    0 => TokenCategory.ClosingTag,
                    > 0 => TokenCategory.UnpairedTag,
                    _ => TokenCategory.OpeningTag,
                };

                // tag attributes
                if (category != TokenCategory.ClosingTag && separatorPos > 0)
                {
                    var offsetAfterTagName = start + separatorPos + 2;
                    var attr = span[offsetAfterTagName..];
                    var attrEnd = attr.Length - (attr[^2] == Terminator ? 2 : 1);
                    attr = attr[..attrEnd];
                    var leadingSpaceLength = attr.IndexOfAnyExcept(Separators);
                    if (leadingSpaceLength > -1)
                    {
                        attrEnd = offsetAfterTagName + attr.LastIndexOfAnyExcept(Separators) + 1;
                        var attrStart = offsetAfterTagName + leadingSpaceLength;

                        data = span[attrStart..attrEnd];
                        dataOffset = attrStart;
                    }
                }

                // tag token
                return new(category,
                    span, globalOffset + start,
                    name, globalOffset + nameOffset,
                    data, globalOffset + dataOffset);
            }
            else
            {
                // the tag name is incorrect, let's try not to discard it all as a content
                var anotherOpenerPos = text[(start + 1)..].IndexOf(Opener);
                if (anotherOpenerPos >= 0)
                {
                    end = start + anotherOpenerPos + 1;
                    span = text[start..end];
                }

                // discarded content token
                return new(TokenCategory.Discarded, span, globalOffset + start);
            }
        }
    }

    public Token GetAttributeToken(ReadOnlySpan<char> text, int globalOffset)
    {
        if (text.IsEmpty)
            return default;

        var span = text;
        var offset = 0;

        // skip space
        var skip = text.IndexOfAnyExcept(Separators);
        if (skip > 0)
        {
            offset += skip;
            span = span[skip..];
        }
        // get name
        var nameEnd = span.IndexOfAny(AttrSeparators);
        if (nameEnd < 0)
        {
            // last flag attribute
            return new(TokenCategory.Attribute,
                span, globalOffset + offset,
                span, globalOffset + offset,
                default, default);
        }
        var name = span[..nameEnd];
        // skip space
        span = span[nameEnd..];
        var eqPos = span.IndexOfAnyExcept(Separators);
        if (eqPos < 0 || span[eqPos] != ValueSeparator)
        {
            // a flag attribute
            return new(TokenCategory.Attribute,
                name, globalOffset + offset,
                name, globalOffset + offset,
                default, default);
        }
        // get eq sign
        span = span[(eqPos + 1)..];
        // - skip space
        var valueStart = span.IndexOfAnyExcept(Separators);
        // - get value
        if (valueStart >= 0)
        {
            span = span[valueStart..];
            var valueEnd = span.IndexOfAnyAfterQuotes(Separators, QuotationMarks);
            if (valueEnd < 0)
            {
                valueEnd = span.Length;
            }

            // attribute with value
            var data = span[..valueEnd];
            var dataOffset = skip + nameEnd + eqPos + 1 + valueStart;

            span = text[offset..(dataOffset + data.Length)];
            return new(TokenCategory.Attribute,
                span, globalOffset + offset,
                name, globalOffset + offset,
                data, globalOffset + dataOffset);
        }
        else
        {
            // an attribute with missing value
            span = text[offset..(skip + nameEnd + eqPos + 1)];
            return new(TokenCategory.Attribute,
                span, globalOffset + offset,
                name, globalOffset + offset,
                default, default);
        }
    }

    public bool ClosesTag(ReadOnlySpan<char> tagSpan, ReadOnlySpan<char> tagName)
    {
        if (tagName.IsEmpty || tagSpan.IsEmpty)
            return false;

        var nameIdx = tagSpan.IndexOf(tagName, cmp);
        return nameIdx == 0 || (nameIdx == 2 && tagSpan[0] == Opener && tagSpan[1] == Terminator);

    }

    public bool TagIsClosed(ReadOnlySpan<char> tagSpan)
    {
        // <tag>...</tag>
        // <tag />
        // <tag/>

        if (tagSpan.IsEmpty || tagSpan[0] != Opener || tagSpan[^1] != Closer)
            return false;

        var trimTag = tagSpan[1..^1];
        var nameLength = trimTag.IndexOfAny(NameSeparators);
        if (nameLength <= 0)
            return false;

        var terminatorPos = trimTag.LastIndexOf(Terminator);
        if (terminatorPos <= 0)
            return false;
        if (terminatorPos == (trimTag.Length - 1))
            return true;

        return trimTag[..nameLength].Equals(trimTag[(terminatorPos + 1)..].Trim(NameSeparators), cmp);
    }

    public ReadOnlySpan<char> TrimName(ReadOnlySpan<char> tag)
    {
        if (tag.Length < 4 || tag[0] != Opener)
            return default;

        if (tag.StartsWith(SectionOpener, cmp))
        {
            // <![CDATA[...
            tag = tag[SectionOpener.Length..];
            var dataPos = tag.IndexOf(DataOpener);
            if (dataPos <= 0)
                return default;

            return tag[..dataPos];
        }

        // <tag...
        // <?xml...
        tag = tag[1..^1];
        var terminatorPos = tag[0] == Terminator ? 0 : tag[^1] == Terminator ? tag.Length - 1 : -1;
        var separatorPos = tag.IndexOfAny(NameSeparators);
        var nameStart = terminatorPos == 0 ? 1 : 0;
        var nameEnd = separatorPos > 0 ? separatorPos : terminatorPos > 0 ? terminatorPos : ^0;
        return tag[nameStart..nameEnd];
    }

    public ReadOnlySpan<char> TrimData(ReadOnlySpan<char> section)
    {
        if (section.StartsWith(SectionOpener, cmp))
        {
            // <![CDATA[...]]> -> ...
            section = section[SectionOpener.Length..^SectionCloser.Length];
            section = section[(section.IndexOf(DataOpener) + 1)..];
        }
        else if (section.StartsWith(CommentOpener, cmp))
        {
            // <!--...--> -> ...
            section = section[CommentOpener.Length..^CommentCloser.Length];
        }

        return section;
    }

    public ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> value)
    {
        if (value[0] == value[^1] && QuotationMarks.Contains(value[0]))
            value = value[1..^1];

        return value;
    }

    private static bool IsElementName(ReadOnlySpan<char> span)
    {
        var count = span.Length;
        if (count == 0)
            return false;

        ref char c = ref MemoryMarshal.GetReference(span);

        if ((uint)((c | 0x20) - 'a') > 'z' - 'a')
        {
            // c is not an ASCII letter
            return false;
        }

        c = ref Unsafe.Add(ref c, 1);
        count--;

        while (count > 0)
        {
            if (!(
                ((uint)((c | 0x20) - 'a') <= 'z' - 'a') ||
                ((uint)(c - '0') <= 9u) ||
                (c == '-') ||
                (c == '_')
               ))
            {
                // c is not an ASCII letter or digit or dash or underscrore
                return false;
            }

            c = ref Unsafe.Add(ref c, 1);
            count--;
        }

        return true;
    }
}
