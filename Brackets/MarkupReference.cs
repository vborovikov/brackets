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
        ReadOnlySpan<char> TrimName(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimData(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> span);
    }

    public abstract partial class MarkupReference<TMarkupLexer> : ISyntaxReference
        where TMarkupLexer : struct, IMarkupLexer
    {
        private readonly TMarkupLexer lexer;
        private readonly StringSet<TagReference> tagReferences;
        private readonly StringSet<AttributeReference> attributeReferences;
        private readonly RootReference rootReference;

        protected MarkupReference(MarkupLanguage language)
        {
            this.lexer = new();
            this.tagReferences = new(this.lexer.Comparison);
            this.attributeReferences = new(this.lexer.Comparison);
            this.rootReference = new RootReference(this);
            this.Language = language;
        }

        internal ref readonly TMarkupLexer Syntax => ref this.lexer;

        public MarkupLanguage Language { get; }

        public Document Parse(string text)
        {
            var root = Parse(text.AsMemory());
            return new Document(root);
        }

        protected void AddReference(TagReference reference)
        {
            this.tagReferences.Add(reference.Name, reference);
        }

        protected void AddReference(AttributeReference reference)
        {
            this.attributeReferences.Add(reference.Name, reference);
        }

        private DocumentRoot Parse(ReadOnlyMemory<char> text)
        {
            var span = text.Span;
            var tree = new Stack<ParentTag>();
            tree.Push(new TextDocumentRoot(text, this.rootReference));

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
                        ParseContent(token, parent);
                    }
                }
                else
                {
                    // skip empty content
                    if (token.IsEmpty)
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
                new StreamSection(token.Name.ToString(), token.Offset, token.Length, token.Data.ToString(), token.DataOffset) :
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
            var reference = CreateOrFindTagReference(token.Name);
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
                tag.Add(attribute);
            }
        }

        private Attribute CreateAttribute(in Token token, bool toString)
        {
            var reference = CreateOrFindAttributeReference(token.Name);
            return
                toString ?
                    new StringAttribute(reference, token.Data.ToString(), token.Offset, token.Length) :
                    token.Data.IsEmpty ?
                        new Attribute(reference, token.Offset, token.Length) :
                        new ValueAttribute(reference, token.Offset, token.Length, token.DataOffset, token.Data.Length);
        }

        private TagReference CreateOrFindTagReference(ReadOnlySpan<char> tagName)
        {
            if (!this.tagReferences.TryGetValue(tagName, out var reference))
            {
                reference = new TagReference(tagName.ToString(), this);
                AddReference(reference);
            }

            return reference;
        }

        private AttributeReference CreateOrFindAttributeReference(ReadOnlySpan<char> attributeName)
        {
            if (!this.attributeReferences.TryGetValue(attributeName, out var reference))
            {
                reference = new AttributeReference(attributeName.ToString(), this);
                AddReference(reference);
            }

            return reference;
        }

        ReadOnlySpan<char> ISyntaxReference.TrimName(ReadOnlySpan<char> span) => this.lexer.TrimName(span);
        ReadOnlySpan<char> ISyntaxReference.TrimData(ReadOnlySpan<char> span) => this.lexer.TrimData(span);
        ReadOnlySpan<char> ISyntaxReference.TrimValue(ReadOnlySpan<char> span) => this.lexer.TrimValue(span);
    }
}