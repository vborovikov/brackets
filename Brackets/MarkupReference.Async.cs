namespace Brackets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parsing;
using Streaming;

public abstract partial class MarkupReference<TMarkupLexer> where TMarkupLexer : struct, IMarkupLexer
{
    public Task<Document> ParseAsync(Stream stream, CancellationToken cancellationToken) =>
        ParseAsync(stream, Encoding.UTF8, cancellationToken);

    public async Task<Document> ParseAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken)
    {
        var builder = new DocumentBuilder(this, encoding);
        await RecordScanner.ScanAsync(stream, builder, cancellationToken).ConfigureAwait(false);
        return builder.Document;
    }

    private int TryParseFragment(ReadOnlySpan<char> span, Stack<ParentTag> tree, int globalOffset)
    {
        foreach (var token in Lexer.TokenizeElements(span, this.lexer, globalOffset))
        {
            var parent = tree.Peek();

            if (parent.HasRawContent)
            {
                if (token.Category == TokenCategory.ClosingTag && this.lexer.ClosesTag(token, parent.Name))
                {
                    ParseClosingTag(token, parent, tree, toString: true);
                }
                else
                {
                    ParseContent(token, parent, toString: true);
                }
            }
            else
            {
                Debug.WriteLine("Token: {0}", token.Category);

                // we might need more data
                if (token.Category.HasFlag(TokenCategory.Discarded) &&
                    (token.Category.HasFlag(TokenCategory.Section) ||
                     token.Category.HasFlag(TokenCategory.Comment) ||
                     token.Category.HasFlag(TokenCategory.Content)))
                {
                    Debug.WriteLine("More data required");
                    return token.Offset - globalOffset;
                }

                // skip empty content
                if (token.IsEmpty)
                    continue;

                switch (token.Category)
                {
                    case TokenCategory.OpeningTag:
                    case TokenCategory.UnpairedTag:
                    case TokenCategory.Instruction:
                    case TokenCategory.Declaration:
                        ParseOpeningTag(token, parent, tree, toString: true);
                        break;

                    case TokenCategory.ClosingTag:
                        ParseClosingTag(token, parent, tree, toString: true);
                        break;

                    case TokenCategory.Section:
                        ParseSection(token, parent, toString: true);
                        break;

                    case TokenCategory.Comment:
                        ParseComment(token, parent);
                        break;

                    default:
                        ParseContent(token, parent, toString: true);
                        break;
                }
            }
        }

        return span.Length;
    }

    private class DocumentBuilder : IRecordBuilder
    {
        private readonly MarkupReference<TMarkupLexer> parser;
        private readonly Stack<ParentTag> tree;
        private int contentLength;

        public DocumentBuilder(MarkupReference<TMarkupLexer> parser, Encoding encoding)
        {
            this.parser = parser;
            this.Encoding = encoding;
            this.Document = new Document(new EmptyDocumentRoot(this.parser.rootReference));
            this.tree = new Stack<ParentTag>();
        }

        public Encoding Encoding { get; private set; }
        public char Opener => this.parser.lexer.Opener;
        public char Closer => this.parser.lexer.Closer;
        public char Encloser => '"';
        public Document Document { get; }

        public ValueTask StartAsync()
        {
            this.tree.Push(this.Document.Root);
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken)
        {
            var charsParsed = this.parser.TryParseFragment(recordSpan, this.tree, this.contentLength);

            if (this.contentLength == 0 && charsParsed > 0 &&
                this.Document.Root.Child is Instruction { Name: "xml", HasAttributes: true } xmlInstruction)
            {
                // update encoding if needed
                if (xmlInstruction.Attributes.FirstOrDefault(a => a is { Name: "encoding", HasValue: true }) is Attribute encodingAttr)
                {
                    try
                    {
                        this.Encoding = Encoding.GetEncoding(encodingAttr.ToString());
                    }
                    catch (Exception)
                    {
                        this.Encoding = Encoding.UTF8;
                    }
                }
            }

            this.contentLength += charsParsed;
            return ValueTask.FromResult(charsParsed);
        }

        public ValueTask StopAsync()
        {
            this.Document.Root.IsWellFormed = this.tree.Count == 1;
            this.Document.Root.CloseAt(this.contentLength);
            this.tree.Clear();
            return ValueTask.CompletedTask;
        }
    }
}