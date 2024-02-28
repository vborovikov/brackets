namespace Brackets
{
    using System;
    using System.Collections.Generic;
    using Collections;
    using Parsing;

    public enum MarkupLanguage
    {
        Html,
        Xml,
        Xhtml,
    }

    public interface ISyntaxReference
    {
        StringComparison Comparison { get; }

        ReadOnlySpan<char> TrimName(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimData(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> span);

        Tag CreateTag(ReadOnlySpan<char> name);
        Attr CreateAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value);
        Content CreateContent(ReadOnlySpan<char> value);
    }

    public abstract partial class MarkupParser<TMarkupLexer> : ISyntaxReference
        where TMarkupLexer : struct, IMarkupLexer
    {
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

        public Document Parse(string text)
        {
            var root = Parse(text.AsMemory());
            return new Document(root);
        }

        public Tag CreateTag(ReadOnlySpan<char> name)
        {
            return CreateTag(
                new Token(TokenCategory.OpeningTag, ReadOnlySpan<char>.Empty, 0, name, 0, ReadOnlySpan<char>.Empty, 0),
                toString: true);
        }

        public Attr CreateAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            return CreateAttribute(
                new Token(TokenCategory.Attribute, ReadOnlySpan<char>.Empty, 0, name, 0, value, 0),
                toString: true);
        }

        public Content CreateContent(ReadOnlySpan<char> value)
        {
            return CreateContent(
                new Token(TokenCategory.Content, value, 0),
                toString: true);
        }

        protected void AddReference(TagRef reference)
        {
            this.tagRefs.Add(reference.Name, reference);
        }

        protected void AddReference(AttrRef reference)
        {
            this.attrRefs.Add(reference.Name, reference);
        }

        private DocumentRoot Parse(ReadOnlyMemory<char> text)
        {
            var span = text.Span;
            var tree = new Stack<ParentTag>();
            tree.Push(new TextDocumentRoot(text, this.rootRef));

            foreach (var token in Lexer.TokenizeElements(span, this.lexer))
            {
                var parent = tree.Peek();

                if (parent.HasRawContent)
                {
                    if (token.Category == TokenCategory.ClosingTag && this.lexer.ClosesTag(token, parent.Name))
                    {
                        ParseClosingTag(token, parent, tree);
                    }
                    else
                    {
                        // skip empty content before and immediately after a single child
                        if (token.IsEmpty && (parent.Child is null || parent.Child.Next == parent.Child))
                            continue;

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
                    // skip empty content if the parent doesn't allow phrasing content
                    if (token.IsEmpty && parent.Level != ElementLevel.Inline && !parent.PermittedContent.HasFlag(ContentCategory.Phrasing))
                        continue;

                    switch (token.Category)
                    {
                        case TokenCategory.OpeningTag:
                        case TokenCategory.UnpairedTag:
                        case TokenCategory.Instruction:
                        case TokenCategory.Declaration:
                            ParseOpeningTag(token, parent, tree);
                            break;

                        case TokenCategory.ClosingTag:
                            ParseClosingTag(token, parent, tree);
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
            var wellFormed = tree.Count > 0 && tree.Count == 1;
            while (tree.Count > 1)
            {
                var unclosedTag = tree.Pop();
                if (!this.lexer.TagIsClosed(span[unclosedTag.Start..unclosedTag.End]))
                {
                    var parentTag = tree.Peek();
                    if (parentTag is DocumentRoot)
                        continue;

                    parentTag.Graft(unclosedTag);
                }
            }

            var root = (DocumentRoot)tree.Pop();
            root.IsWellFormed = wellFormed;
            return root;
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

        private void ParseOpeningTag(in Token token, ParentTag parent, Stack<ParentTag> tree, bool toString = false)
        {
            var tag = CreateTag(token, toString);
            parent.Add(tag);

            if (tag is ParentTag pairedTag)
                tree.Push(pairedTag);
        }

        private void ParseClosingTag(in Token token, ParentTag parent, Stack<ParentTag> tree, bool toString = false)
        {
            // close the current tag
            // also special handling for a misplaced closing tag
            foreach (var unclosedTag in tree)
            {
                if (this.lexer.ClosesTag(token, unclosedTag.Name))
                {
                    while (tree.Count > 1)
                    {
                        var tag = tree.Pop();
                        if (tag == unclosedTag)
                        {
                            if (tag != parent)
                            {
                                var parentTag = tree.Peek();
                                parentTag.Graft(tag, token.End);
                            }

                            tag.CloseAt(token.End);
                            return;
                        }
                        else
                        {
                            var parentTag = tree.Peek();
                            parentTag.Graft(tag, token.End);
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
                AddReference(reference);
            }

            return reference;
        }

        private AttrRef CreateOrFindAttrRef(ReadOnlySpan<char> attributeName)
        {
            if (!this.attrRefs.TryGetValue(attributeName, out var reference))
            {
                reference = new AttrRef(attributeName.ToString(), this);
                AddReference(reference);
            }

            return reference;
        }

        public abstract StringComparison Comparison { get; }
        StringComparison ISyntaxReference.Comparison => this.Comparison;
        ReadOnlySpan<char> ISyntaxReference.TrimName(ReadOnlySpan<char> span) => this.lexer.TrimName(span);
        ReadOnlySpan<char> ISyntaxReference.TrimData(ReadOnlySpan<char> span) => this.lexer.TrimData(span);
        ReadOnlySpan<char> ISyntaxReference.TrimValue(ReadOnlySpan<char> span) => this.lexer.TrimValue(span);
    }
}