namespace Brackets
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using Collections;
    using Parsing;

    /// <summary>
    /// Identifies the markup language.
    /// </summary>
    public enum MarkupLanguage
    {
        /// <summary>
        /// HTML
        /// </summary>
        Html,
        /// <summary>
        /// XML
        /// </summary>
        Xml,
        /// <summary>
        /// XHTML
        /// </summary>
        Xhtml,
    }

    /// <summary>
    /// Represents the markup capabilities.
    /// </summary>
    public interface IMarkup
    {
        /// <summary>
        /// Gets the supported markup language.
        /// </summary>
        MarkupLanguage Language { get; }

        Tag CreateTag(ReadOnlySpan<char> name);
        Attr CreateAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value);
        Content CreateContent(ReadOnlySpan<char> value);

        /// <summary>
        /// Changes the name of the tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="name">A new tag name.</param>
        void ChangeName(Tag tag, ReadOnlySpan<char> name);
    }

    interface ISyntaxReference : IMarkup
    {
        StringComparison Comparison { get; }

        ReadOnlySpan<char> TrimName(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimData(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> span);
    }

    interface IMarkupParser : IMarkup
    {
        void AddTagRef(TagRef reference);
        void AddAttrRef(AttrRef reference);
    }

    static class UnsafeAccessors
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "reference")]
        public extern static ref TagRef TagRef(Tag tag);
    }

    public abstract partial class MarkupParser<TMarkupLexer> : IMarkup, IMarkupParser, ISyntaxReference
        where TMarkupLexer : struct, IMarkupLexer
    {
        private static readonly SearchValues<char> LineSeparators = SearchValues.Create("\r\n");

        private readonly TMarkupLexer lexer;
        private readonly IStringSet<TagRef> tagRefs;
        private readonly IStringSet<AttrRef> attrRefs;
        private readonly RootRef rootRef;

        protected MarkupParser(MarkupLanguage language, IStringSet<TagRef> tagRefs, IStringSet<AttrRef> attrRefs)
        {
            this.lexer = new();
            this.tagRefs = tagRefs;
            this.attrRefs = attrRefs;
            this.rootRef = new RootRef(this);
            this.Language = language;
        }

        internal ref readonly TMarkupLexer Syntax => ref this.lexer;

        public MarkupLanguage Language { get; }

        public abstract StringComparison Comparison { get; }

        public Document Parse(string text)
        {
            var root = Parse(text.AsMemory());
            return new Document(root);
        }

        public Tag CreateTag(ReadOnlySpan<char> name)
        {
            return CreateTag(
                new Token(TokenCategory.OpeningTag, name, 0, name, 0, [], 0),
                toString: true);
        }

        public Attr CreateAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            var reference = CreateOrFindAttrRef(name);
            return new StringAttr(reference, value, 0, reference.Name.Length + value.Length + 3);
        }

        public Content CreateContent(ReadOnlySpan<char> value)
        {
            return CreateContent(
                new Token(TokenCategory.Content, value, 0),
                toString: true);
        }

        public void ChangeName(Tag tag, ReadOnlySpan<char> name)
        {
            if (tag.Reference.Syntax.Language != this.Language)
                throw new ArgumentOutOfRangeException(nameof(tag));

            if (name.IsEmpty || name.IsWhiteSpace())
                throw new ArgumentOutOfRangeException(nameof(name));

            if (name.Equals(tag.Name, this.Comparison))
                return;

            UnsafeAccessors.TagRef(tag) = CreateOrFindTagRef(name);
        }

        protected void AddTagRef(TagRef reference)
        {
            this.tagRefs.Add(reference.Name, reference);
        }

        protected void AddAttrRef(AttrRef reference)
        {
            this.attrRefs.Add(reference.Name, reference);
        }

        private DocumentRoot Parse(ReadOnlyMemory<char> text)
        {
            var span = text.Span;
            ParentTag parent = new TextDocumentRoot(text, this.rootRef);

            foreach (var token in Lexer.TokenizeElements(span, this.lexer))
            {
                if (CanSkip(token, parent))
                    continue;

                if (parent.HasRawContent)
                {
                    if (token.Category == TokenCategory.ClosingTag && this.lexer.ClosesTag(token, parent.Name))
                    {
                        ParseClosingTag(token, ref parent);
                    }
                    else
                    {
                        if (token.Category == TokenCategory.Section)
                        {
                            ParseSection(token, parent);
                        }
                        else
                        {
                            ParseContent(token, parent);
                        }
                    }
                }
                else
                {
                    switch (token.Category)
                    {
                        case TokenCategory.OpeningTag:
                        case TokenCategory.UnpairedTag:
                        case TokenCategory.Instruction:
                        case TokenCategory.Declaration:
                            ParseOpeningTag(token, ref parent);
                            break;

                        case TokenCategory.ClosingTag:
                            ParseClosingTag(token, ref parent);
                            break;

                        case TokenCategory.Section:
                            ParseSection(token, parent);
                            break;

                        case TokenCategory.Comment:
                        case TokenCategory.Comment | TokenCategory.Discarded:
                            ParseComment(token, parent);
                            break;

                        default:
                            ParseContent(token, parent);
                            break;
                    }
                }
            }

            // close unclosed tags
            var wellFormed = parent.Parent is null;
            while (parent.Parent is not null)
            {
                var unclosedTag = parent;
                parent = parent.Parent;
                if (!this.lexer.TagIsClosed(span[unclosedTag.Start..unclosedTag.End]))
                {
                    if (parent is DocumentRoot)
                        break;

                    parent.Graft(unclosedTag);
                }
            }

            var root = (DocumentRoot)parent;
            root.IsWellFormed = wellFormed;
            return root;
        }

        private static bool CanSkip(in Token token, ParentTag parent)
        {
            if (!token.IsEmpty)
                return false;

            if (parent.HasRawContent)
            {
                // skip empty content before and immediately after a single child (a section)
                return parent.Child is null || parent.Child.Next == parent.Child;
            }
            else if (parent.PreservesFormatting)
            {
                // keep source formatting, special handling for <pre> tag
                return false;
            }

            // don't skip the token,
            // it will be removed on the parent closing if the whitespace is the last child
            return token.Span.ContainsAny(LineSeparators);
        }

        private static void ParseComment(in Token token, ParentTag parent)
        {
            parent.Add(new Comment(token.Offset, token.Length));
        }

        private static void ParseSection(in Token token, ParentTag parent, bool toString = false)
        {
            parent.Add(toString ?
                new StringSection(token.Name.ToString(), token.Offset, token.Length, token.Data.ToString(), token.DataOffset) :
                new Section(token.Offset, token.Length, token.DataOffset, token.Data.Length));
        }

        private static void ParseContent(in Token token, ParentTag parent, bool toString = false)
        {
            parent.Add(CreateContent(token, toString));
        }

        private void ParseOpeningTag(in Token token, ref ParentTag parent, bool toString = false)
        {
            var tag = CreateTag(token, toString);
            parent.Add(tag);

            if (tag is ParentTag pairedTag)
                parent = pairedTag;
        }

        private void ParseClosingTag(in Token token, ref ParentTag parent, bool toString = false)
        {
            // close the current tag
            // also special handling for a misplaced closing tag
            var current = parent;
            for (var unclosedTag = parent; unclosedTag is not null; unclosedTag = unclosedTag.Parent)
            {
                if (this.lexer.ClosesTag(token, unclosedTag.Name))
                {
                    while (parent.Parent is not null)
                    {
                        var tag = parent;
                        parent = parent.Parent;

                        if (tag == unclosedTag)
                        {
                            if (tag != current)
                            {
                                parent.Graft(tag, token.End);
                            }

                            tag.CloseAt(token.End);
                            return;
                        }
                        else
                        {
                            parent.Graft(tag, token.End);
                        }
                    }
                }
            }

            // there was no opening tag for this closing tag, turn it into content
            parent.Add(CreateContent(token, toString));
        }

        private static Content CreateContent(in Token token, bool toString)
        {
            return toString ?
                new StringContent(token.Span.ToString(), token.Start) :
                new Content(token.Start, token.Length);
        }

        private Tag CreateTag(in Token token, bool toString)
        {
            var reference = CreateOrFindTagRef(token.Name);
            var tag = token.Category switch
            {
                TokenCategory.Instruction => new Instruction(reference, token.Start, token.Length),
                TokenCategory.Declaration => new Declaration(reference, token.Start, token.Length),
                TokenCategory.OpeningTag when reference.IsParent => new ParentTag(reference, token.Start, token.Length),
                _ => new Tag(reference, token.Start, token.Length),
            };

            ParseAttributes(tag, token, toString);

            return tag;
        }

        private void ParseAttributes(Tag tag, in Token token, bool toString)
        {
            if (token.Data.IsEmpty)
                return;

            foreach (var attr in Lexer.TokenizeAttributes(token, this.lexer))
            {
                var attribute = CreateAttribute(attr, toString);
                tag.AddAttribute(attribute);
            }
        }

        private Attr CreateAttribute(in Token token, bool toString)
        {
            var reference = CreateOrFindAttrRef(token.Name);
            return
                toString ?
                    new StringAttr(reference, token.Data, token.Offset, token.Length) :
                    token.Data.IsEmpty ?
                        new Attr(reference, token.Offset, token.Length) :
                        new ValueAttr(reference, token.Offset, token.Length, token.DataOffset, token.Data.Length);
        }

        private TagRef CreateOrFindTagRef(ReadOnlySpan<char> tagName)
        {
            if (!this.tagRefs.TryGetValue(tagName, out var reference))
            {
                reference = new TagRef(tagName.ToString(), this);
                AddTagRef(reference);
            }

            return reference;
        }

        private AttrRef CreateOrFindAttrRef(ReadOnlySpan<char> attributeName)
        {
            if (!this.attrRefs.TryGetValue(attributeName, out var reference))
            {
                reference = new AttrRef(attributeName.ToString(), this);
                AddAttrRef(reference);
            }

            return reference;
        }

        StringComparison ISyntaxReference.Comparison => this.Comparison;
        ReadOnlySpan<char> ISyntaxReference.TrimName(ReadOnlySpan<char> span) => this.lexer.TrimName(span);
        ReadOnlySpan<char> ISyntaxReference.TrimData(ReadOnlySpan<char> span) => this.lexer.TrimData(span);
        ReadOnlySpan<char> ISyntaxReference.TrimValue(ReadOnlySpan<char> span) => this.lexer.TrimValue(span);

        void IMarkupParser.AddTagRef(TagRef reference) => AddTagRef(reference);
        void IMarkupParser.AddAttrRef(AttrRef reference) => AddAttrRef(reference);
    }
}