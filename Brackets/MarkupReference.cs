namespace Brackets
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Collections;
    using Parsing;
    using Streaming;

    public interface ISyntaxReference
    {
        ReadOnlySpan<char> TrimName(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimData(ReadOnlySpan<char> span);
        ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> span);
    }

    public abstract class MarkupReference<TMarkupLexer> : ISyntaxReference
        where TMarkupLexer : struct, IMarkupLexer
    {
        private readonly TMarkupLexer lexer;
        private readonly StringDir<TagReference> tagReferences;
        private readonly StringDir<AttributeReference> attributeReferences;
        private readonly RootReference rootReference;

        protected MarkupReference()
        {
            this.lexer = new();
            this.tagReferences = new(this.lexer.Comparison);
            this.attributeReferences = new(this.lexer.Comparison);
            this.rootReference = new RootReference(this);
        }

        internal ref readonly TMarkupLexer Syntax => ref this.lexer;

        public Document Parse(string text)
        {
            var root = Parse(text.AsMemory());
            return new Document(root);
        }

        public async Task<Document> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            var builder = new DocumentBuilder(this);
            await RecordScanner.ScanAsync(stream, builder, cancellationToken).ConfigureAwait(false);
            return builder.Document;
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

            ParsePartial(span, tree);

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

        private void ParsePartial(ReadOnlySpan<char> span, Stack<ParentTag> tree)
        {
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
                        case TokenCategory.Discarded:
                        case TokenCategory.Content:
                            ParseContent(token, parent);
                            break;

                        case TokenCategory.OpeningTag:
                        case TokenCategory.UnpairedTag:
                            ParseOpeningTag(token, parent, tree);
                            break;

                        case TokenCategory.ClosingTag:
                            ParseClosingTag(token, parent, tree);
                            break;

                        case TokenCategory.Section:
                            ParseSection(token, parent);
                            break;

                        case TokenCategory.Comment:
                            ParseComment(token, parent);
                            break;
                    }
                }
            }
        }

        private static void ParseComment(in Token token, ParentTag parent)
        {
            parent.Add(new Comment(token.Offset, token.Length));
        }

        private static void ParseSection(in Token token, ParentTag parent)
        {
            parent.Add(new Section(token.Offset, token.Length, token.DataOffset, token.Data.Length));
        }

        private static void ParseContent(in Token token, ParentTag parent)
        {
            parent.Add(CreateContent(token));
        }

        private void ParseOpeningTag(in Token token, ParentTag parent, Stack<ParentTag> tree)
        {
            var tag = CreateTag(token);
            parent.Add(tag);

            if (tag is ParentTag pairedTag)
                tree.Push(pairedTag);
        }

        private void ParseClosingTag(in Token token, ParentTag parent, Stack<ParentTag> tree)
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
            parent.Add(CreateContent(token));
        }

        private static Content CreateContent(in Token token)
        {
            return new Content(token.Start, token.Length);
        }

        private Tag CreateTag(in Token token)
        {
            var reference = CreateOrFindTagReference(token.Name);
            var tag = token.Category != TokenCategory.UnpairedTag && reference.IsParent ?
                new ParentTag(reference, token.Start, token.Length) :
                new Tag(reference, token.Start, token.Length);

            ParseAttributes(tag, token);

            return tag;
        }

        private void ParseAttributes(Tag tag, in Token token)
        {
            if (token.Data.IsEmpty)
                return;

            foreach (var attr in Lexer.TokenizeAttributes(token, this.lexer))
            {
                var attribute = CreateAttribute(attr);
                tag.Add(attribute);
            }
        }

        private Attribute CreateAttribute(in Token token)
        {
            var reference = CreateOrFindAttributeReference(token.Name);
            return token.Data.IsEmpty ?
                new Attribute(reference, token.Offset, token.Length) :
                new Attribute(reference, token.Offset, token.Length, token.DataOffset, token.Data.Length);
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

        private class DocumentBuilder : IRecordBuilder
        {
            private readonly MarkupReference<TMarkupLexer> parser;
            private readonly Stack<ParentTag> tree;
            private int contentLength;

            public DocumentBuilder(MarkupReference<TMarkupLexer> parser)
            {
                this.parser = parser;
                this.Document = new Document(new EmptyDocumentRoot(this.parser.rootReference));
                this.tree = new Stack<ParentTag>();
            }

            public Encoding Encoding => Encoding.UTF8;
            public char Opener => this.parser.lexer.Opener;
            public char Closer => this.parser.lexer.Closer;
            public char Encloser => '"';
            public Document Document { get; }

            public ValueTask StartAsync()
            {
                this.tree.Push(this.Document.Root);
                return ValueTask.CompletedTask;
            }

            public ValueTask BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken)
            {
                this.parser.ParsePartial(recordSpan, this.tree);
                this.contentLength += recordSpan.Length;
                return ValueTask.CompletedTask;
            }

            public ValueTask StopAsync()
            {
                this.Document.Root.IsWellFormed = this.tree.Count > 0 && this.tree.Count == 1;
                this.Document.Root.CloseAt(this.contentLength);
                this.tree.Clear();
                return ValueTask.CompletedTask;
            }
        }
    }
}