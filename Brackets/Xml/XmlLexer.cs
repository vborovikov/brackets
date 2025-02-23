namespace Brackets.Xml;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Parsing;

public readonly struct XmlLexer : IMarkupLexer
{
    private const StringComparison cmp = StringComparison.Ordinal;
    private const char Opener = '<';
    private const char Closer = '>';
    private const char Terminator = '/';
    private const char DataOpener = '[';
    private const char ValueSeparator = '=';
    private const string QuotationMarks = "'\"";
    private const string SeparatorsTrim = " \r\n\t\xA0";
    private const string NameSeparatorsTrim = "/" + SeparatorsTrim;
    private const string AttrSeparatorsTrim = "=" + SeparatorsTrim;
    private static readonly SearchValues<char> Separators = SearchValues.Create(SeparatorsTrim);
    private static readonly SearchValues<char> NameSeparators = SearchValues.Create(NameSeparatorsTrim);
    private static readonly SearchValues<char> AttrSeparators = SearchValues.Create(AttrSeparatorsTrim);
    private const string TermOpener = "</";
    private const string TermCloser = "/>";
    private const string CommentOpener = "<!--";
    private const string CommentCloser = "-->";
    private const string SectionOpener = "<![";
    private const string SectionCloser = "]]>";
    private const string InstrOpener = "<?";
    private const string InstrCloser = "?>";
    private const string DeclOpener = "<!";

    public static StringComparison Comparison => cmp;
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
            return new(TokenCategory.Discarded | TokenCategory.Content, text, globalOffset);
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

                    return new(TokenCategory.Discarded | TokenCategory.Section, span, globalOffset + start);
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
                return new(TokenCategory.Discarded | TokenCategory.Section, span, globalOffset + start);
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
                    end = text.Length;
                    category |= TokenCategory.Discarded;
                }

                span = text[start..end];
            }

            // comment token
            return new(category, span, globalOffset + start);
        }
        else
        {
            // <?tag...?> | <!tag ...> | <tag .../> | <tag ...> | </tag>

            // token category
            category = span.Length > 3 ? (
                    span.StartsWith(DeclOpener, cmp) ? TokenCategory.Declaration :
                    span.StartsWith(InstrOpener, cmp) && span.EndsWith(InstrCloser, cmp) ? TokenCategory.Instruction :
                    span.StartsWith(TermOpener, cmp) ? TokenCategory.ClosingTag :
                    span.EndsWith(TermCloser, cmp) ? TokenCategory.UnpairedTag :
                    TokenCategory.OpeningTag
                ) : TokenCategory.OpeningTag;

            var tag = category switch
            {
                TokenCategory.Declaration or TokenCategory.ClosingTag => span[2..^1],
                TokenCategory.Instruction => span[2..^2],
                TokenCategory.UnpairedTag => span[1..^2],
                _ => span[1..^1],
            };

            var separatorPos = tag.IndexOfAny(Separators);
            var tagName = separatorPos > 0 ? tag[..separatorPos] : tag;
            if (IsElementName(tagName))
            {
                // tag name
                name = tagName;
                nameOffset = start + category switch
                {
                    TokenCategory.Declaration or TokenCategory.Instruction or TokenCategory.ClosingTag => 2,
                    _ => 1,
                };

                // tag attributes
                if (category != TokenCategory.ClosingTag && separatorPos > 0)
                {
                    var offsetAfterTagName = start + separatorPos + 2;
                    var attr = span[offsetAfterTagName..];
                    var attrEnd = attr.Length - category switch
                    {
                        TokenCategory.Instruction or TokenCategory.UnpairedTag => 2,
                        _ => 1,
                    };
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

                // discarded tag token
                return new(TokenCategory.Discarded | category, span, globalOffset + start);
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
            var valueEnd = span.IndexOfAnyUnquoted(Separators, QuotationMarks);
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
        return nameIdx == 0 || (nameIdx == 2 && tagSpan.StartsWith(TermOpener, cmp));

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

        return trimTag[..nameLength].Equals(trimTag[(terminatorPos + 1)..].Trim(NameSeparatorsTrim), cmp);
    }

    public ReadOnlySpan<char> TrimName(ReadOnlySpan<char> tag)
    {
        if (tag.Length < 4 || tag[0] != Opener || tag[^1] != Closer)
            return default;

        if (tag.StartsWith(SectionOpener, cmp) && tag.EndsWith(SectionCloser, cmp))
        {
            // <![CDATA[...
            tag = tag[SectionOpener.Length..];
            var dataPos = tag.IndexOf(DataOpener);
            if (dataPos <= 0)
                return default;

            return tag[..dataPos];
        }

        if (tag.StartsWith(DeclOpener, cmp) || tag.StartsWith(TermOpener, cmp))
        {
            tag = tag[2..^1];
        }
        else if (tag.StartsWith(InstrOpener, cmp) && tag.EndsWith(InstrCloser, cmp))
        {
            tag = tag[2..^2];
        }
        else if (tag.EndsWith(TermCloser, cmp))
        {
            tag = tag[1..^2];
        }
        else
        {
            tag = tag[1..^1];
        }

        var separatorPos = tag.IndexOfAny(NameSeparators);
        var nameEnd = separatorPos > 0 ? separatorPos : ^0;
        return tag[..nameEnd];
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
        if (value.Length > 1 && value[0] == value[^1] && QuotationMarks.Contains(value[0]))
            value = value[1..^1];

        return value;
    }

    // https://www.w3.org/TR/xml/#sec-common-syn
    private static bool IsElementName(ReadOnlySpan<char> span)
    {
        var count = span.Length;
        if (count == 0)
            return false;

        ref char c = ref MemoryMarshal.GetReference(span);

        //note: no check for [#x10000-#xEFFFF]

        if (!(
            ((uint)((c | 0x20) - 'a') <= 'z' - 'a') ||
            (c == ':') ||
            (c == '_') ||
            (c is >= '\u00C0' and <= '\u00D6') ||
            (c is >= '\u00D8' and <= '\u00F6') ||
            (c is >= '\u00F8' and <= '\u02FF') ||
            (c is >= '\u0370' and <= '\u037D') ||
            (c is >= '\u037F' and <= '\u1FFF') ||
            (c is >= '\u200C' and <= '\u200D') ||
            (c is >= '\u2070' and <= '\u218F') ||
            (c is >= '\u2C00' and <= '\u2FEF') ||
            (c is >= '\u3001' and <= '\uD7FF') ||
            (c is >= '\uF900' and <= '\uFDCF') ||
            (c is >= '\uFDF0' and <= '\uFFFD')
            ))
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
                (c == '.') ||
                (c == ':') ||
                (c == '_') ||
                (c == '\u00B7') ||
                (c is >= '\u00C0' and <= '\u00D6') ||
                (c is >= '\u00D8' and <= '\u00F6') ||
                (c is >= '\u00F8' and <= '\u02FF') ||
                (c is >= '\u0300' and <= '\u036F') ||
                (c is >= '\u0370' and <= '\u037D') ||
                (c is >= '\u037F' and <= '\u1FFF') ||
                (c is >= '\u200C' and <= '\u200D') ||
                (c is >= '\u203F' and <= '\u2040') ||
                (c is >= '\u2070' and <= '\u218F') ||
                (c is >= '\u2C00' and <= '\u2FEF') ||
                (c is >= '\u3001' and <= '\uD7FF') ||
                (c is >= '\uF900' and <= '\uFDCF') ||
                (c is >= '\uFDF0' and <= '\uFFFD')
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
