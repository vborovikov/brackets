namespace Brackets.Primitives
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public static class Tags
    {
        public static TagEnumerator Parse(ReadOnlySpan<char> text, in MarkupSyntax syntax)
        {
            return new TagEnumerator(text, syntax);
        }

        public static AttributeEnumerator ParseAttributes(TagSpan tagSpan, in MarkupSyntax syntax)
        {
            return new AttributeEnumerator(tagSpan, syntax);
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

        public ref struct TagEnumerator
        {
            private readonly ReadOnlySpan<char> text;
            private readonly ref readonly MarkupSyntax stx;
            private ReadOnlySpan<char> span;
            private int index;

            public TagEnumerator(ReadOnlySpan<char> text, in MarkupSyntax syntax)
            {
                this.text = text;
                this.stx = ref syntax;
                this.span = text;
                this.index = 0;
                this.Current = default;
            }

            public TagSpan Current { get; private set; }

            public void Reset()
            {
                this.span = this.text;
                this.index = 0;
                this.Current = default;
            }

            public readonly TagEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (this.span.IsEmpty)
                    return false;

                // find next opener and closer
                var openerPos = this.span.IndexOf(this.stx.Opener);
                var closerPos = this.span.IndexOf(this.stx.Closer);
                if (openerPos < 0 || closerPos < 0)
                {
                    // no more tags
                    this.Current = new TagSpan(this.span, this.index, TagCategory.Content);
                    this.index += this.span.Length;
                    this.span = default;

                    return true;
                }

                if (openerPos > 0)
                {
                    // there is a content before the tag
                    this.Current = new TagSpan(this.span[0..openerPos], this.index, TagCategory.Content);
                    this.index += openerPos;
                    this.span = this.span.Slice(openerPos);

                    return true;
                }

                // a tag or content is right here
                var tag = this.span[1..closerPos];
                var category = TagCategory.Content;
                var name = ReadOnlySpan<char>.Empty;

                if (!tag.IsEmpty)
                {
                    var slashPos = tag[0] == this.stx.Terminator ? 0 : tag[^1] == this.stx.Terminator ? tag.Length - 1 : -1;
                    var spacePos = tag.IndexOfAny(this.stx.Separators);
                    var nameStartIdx = slashPos == 0 ? 1 : 0;
                    var nameEndIdx = spacePos > 0 ? spacePos : slashPos > 0 ? slashPos : ^0;
                    var tagName = tag[nameStartIdx..nameEndIdx];
                    var tagNameIsValid = true;

                    if (IsElementName(tagName) || (tagName[0] == this.stx.AltOpener && IsElementName(tagName[1..])))
                    {
                        // correct closing, opening or unpaired tag
                        category = slashPos switch
                        {
                            0 => TagCategory.Closing,
                            > 0 => TagCategory.Unpaired,
                            _ => TagCategory.Opening
                        };
                        name = tagName;
                    }
                    else if (tagName[0] == this.stx.AltOpener && tagName.Length >= 3)
                    {
                        if (tagName[1] == '-' && tagName[2] == '-')
                        {
                            // <!-- ... -->

                            category = TagCategory.Comment;

                            var commentCloserPos = this.span.IndexOf(this.stx.CommentCloser, StringComparison.Ordinal);
                            if (commentCloserPos > 0)
                            {
                                closerPos = commentCloserPos + this.stx.CommentCloser.Length - 1;
                            }
                            else
                            {
                                // treat the rest of the span as comment then
                                closerPos = this.span.Length - 1;
                            }
                        }
                        else if (tagName[1] == '[')
                        {
                            // <![###[ ... ]]>

                            var sectionCloserPos = this.span.IndexOf(this.stx.SectionCloser, StringComparison.Ordinal);
                            if (sectionCloserPos > 0)
                            {
                                closerPos = sectionCloserPos + this.stx.SectionCloser.Length - 1;
                                var nameEndPos = tagName[2..].IndexOf('[');
                                if (nameEndPos > 0)
                                {
                                    name = tagName[2..(nameEndPos + 2)];
                                    category = TagCategory.Section;
                                }
                            }
                            else
                            {
                                // section is incorrect
                                tagNameIsValid = false;
                            }
                        }
                    }
                    else
                    {
                        tagNameIsValid = false;
                    }

                    if (!tagNameIsValid)
                    {
                        // the tag name is incorrect, let's try not to discard it all as a content
                        var anotherOpenerPos = this.span[1..].IndexOf(this.stx.Opener);
                        if (anotherOpenerPos > 0)
                        {
                            // in the original span it's the position right before anotherOpenerPos
                            closerPos = anotherOpenerPos;
                        }
                    }
                }

                var afterCloserPos = closerPos + 1;
                this.Current = new TagSpan(this.span[openerPos..afterCloserPos], this.index, category) { Name = name };
                this.index += afterCloserPos;
                this.span = this.span.Slice(afterCloserPos);

                return true;
            }
        }

        public ref struct AttributeEnumerator
        {
            private readonly TagSpan tag;
            private readonly ref readonly MarkupSyntax stx;
            private ReadOnlySpan<char> span;
            private int index;

            public AttributeEnumerator(TagSpan tag, in MarkupSyntax syntax)
            {
                this.tag = tag;
                this.stx = ref syntax;
                this.span = tag;
                this.index = tag.Index;
                this.Current = default;
                StripTag();
            }

            public AttributeSpan Current { get; private set; }

            public void Reset()
            {
                this.span = this.tag;
                this.index = this.tag.Index;
                this.Current = default;
                StripTag();
            }

            public readonly AttributeEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (this.span.IsEmpty)
                    return false;

                var startPos = 0;
                var endPos = 0;
                var skipPos = 0;
                var category = this.span[0] == this.stx.EqSign ? AttributeCategory.Value : AttributeCategory.Name;

                if (category == AttributeCategory.Value)
                {
                    // the span starts from '=' followed by a value
                    var charsSkipped = this.span[1..].IndexOfAnyExcept(this.stx.Separators);
                    var nextPart = charsSkipped >= 0 ? this.span[(1 + charsSkipped)..] : default;
                    if (nextPart.IsEmpty)
                    {
                        this.span = default;
                        return false;
                    }
                    else
                    {
                        startPos = 1 + charsSkipped;
                        // find the last quotation mark
                        var sepPos = nextPart.IndexOfAnyAfterQuotes(this.stx.Separators, this.stx.QuotationMarks);
                        if (sepPos < 0)
                            sepPos = nextPart.Length;
                        endPos = startPos + sepPos;
                        skipPos = this.span[endPos..].IndexOfAnyExcept(this.stx.Separators);
                        if (skipPos < 0)
                        {
                            skipPos = this.span.Length;
                        }
                        else
                        {
                            skipPos += endPos;
                        }
                    }
                }
                else
                {
                    // the span starts from a name
                    endPos = this.span.IndexOfAny(this.stx.AttrSeparators);
                    if (endPos < 0)
                    {
                        // no value for sure, it's a flag
                        category = AttributeCategory.Flag;
                        endPos = skipPos = this.span.Length;
                    }
                    else
                    {
                        // check for '='
                        var charsSkipped = this.span[endPos..].IndexOfAnyExcept(this.stx.Separators);
                        var nextPart = charsSkipped >= 0 ? this.span[(endPos + charsSkipped)..] : default;
                        if (nextPart.IsEmpty || nextPart[0] != this.stx.EqSign)
                        {
                            // no value or the rest is just white-space, the attribute is a flag
                            category = AttributeCategory.Flag;
                        }

                        // offset the span properly
                        skipPos = endPos + charsSkipped;
                    }
                }

                this.Current = new AttributeSpan(this.span[startPos..endPos], this.index + startPos, category);
                this.span = this.span[skipPos..];
                this.index += skipPos;

                return true;
            }

            private void StripTag()
            {
                var spacePos = this.span.IndexOfAny(this.stx.Separators);
                if (spacePos < 0)
                {
                    this.span = ReadOnlySpan<char>.Empty;
                    return;
                }

                // skip the first space character
                var afterSpacePos = spacePos + 1;
                this.span = this.span[afterSpacePos..];
                this.index += afterSpacePos;

                // skip the rest of space characters if any
                afterSpacePos = this.span.IndexOfAnyExcept(this.stx.Separators);
                this.span = this.span[afterSpacePos..];
                this.index += afterSpacePos;

                if (this.span[^1] == this.stx.Closer)
                {
                    var endIdx = ^1;
                    if (this.span.Length > 1 && this.span[^2] == this.stx.Terminator)
                        endIdx = ^2;
                    this.span = this.span[..endIdx].TrimEnd(this.stx.Separators);
                }
            }
        }
    }
}
