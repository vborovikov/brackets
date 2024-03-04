namespace Brackets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parsing;
using Streaming;

public abstract partial class MarkupParser<TMarkupLexer> where TMarkupLexer : struct, IMarkupLexer
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
            if (CanSkip(token, parent))
                continue;

            if (parent.HasRawContent)
            {
                if (token.Category == TokenCategory.ClosingTag && this.lexer.ClosesTag(token, parent.Name))
                {
                    ParseClosingTag(token, parent, tree, toString: true);
                }
                else
                {
                    if (token.Category == TokenCategory.Section)
                    {
                        ParseSection(token, parent, toString: true);
                    }
                    else if (token.Category.HasFlag(TokenCategory.Discarded | TokenCategory.Section))
                    {
                        // we might need more data
                        return token.Offset - globalOffset;
                    }
                    else
                    {
                        ParseContent(token, parent, toString: true);
                    }
                }
            }
            else
            {
                // we might need more data
                if (token.Category.HasFlag(TokenCategory.Discarded) &&
                    (token.Category.HasFlag(TokenCategory.Section) ||
                     token.Category.HasFlag(TokenCategory.Comment) ||
                     token.Category.HasFlag(TokenCategory.Content)))
                {
                    return token.Offset - globalOffset;
                }

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

    private sealed class DocumentBuilder : IElementBuilder
    {
        private readonly MarkupParser<TMarkupLexer> parser;
        private readonly Stack<ParentTag> tree;
        private int contentLength;
        private bool encodingParsed;

        public DocumentBuilder(MarkupParser<TMarkupLexer> parser, Encoding encoding)
        {
            this.parser = parser;
            this.Encoding = encoding;
            this.Document = new Document(new EmptyDocumentRoot(this.parser.rootRef));
            this.tree = new Stack<ParentTag>();
        }

        public Encoding Encoding { get; private set; }
        public char Opener => this.parser.lexer.Opener;
        public char Closer => this.parser.lexer.Closer;
        public Document Document { get; }

        public ValueTask StartAsync()
        {
            this.tree.Push(this.Document.Root);
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken)
        {
            var charsParsed = this.parser.TryParseFragment(recordSpan, this.tree, this.contentLength);
            if (charsParsed <= 0)
                return ValueTask.FromResult(0);

            EnsureEncoding();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureEncoding()
        {
            if (this.encodingParsed)
                return;

            if (this.contentLength == 0 && this.parser.Language == MarkupLanguage.Xml)
            {
                if (this.Document.FirstOrDefault<Instruction>() is { Name: "xml", HasAttributes: true } xmlInstruction &&
                    xmlInstruction.Attributes["encoding"] is { Length: > 0 } encoding)
                {
                    SetEncoding(encoding.ToString());
                }
            }
            else if (this.contentLength < RecordBuffer.DefaultBufferLength && this.parser.Language == MarkupLanguage.Html)
            {
                if (this.Document.Find<ParentTag>(t => t.Name.Equals("head", StringComparison.OrdinalIgnoreCase)) is ParentTag head)
                {
                    if (head.FirstOrDefault<Tag>(IsMetaWithCharset) is Tag charsetMeta &&
                        charsetMeta.Attributes["charset"] is { Length: > 0 } charset)
                    {
                        SetEncoding(charset.ToString());
                    }
                    else if (head.FirstOrDefault<Tag>(IsMetaWithContentType) is Tag contentMeta &&
                        contentMeta.Attributes["content"] is { Length: > 0 } mimeType)
                    {
                        SetEncoding(mimeType[(mimeType.LastIndexOf('=') + 1)..].ToString());
                    }
                }
                if (this.Document.Find<ParentTag>(t => t.Name.Equals("body", StringComparison.OrdinalIgnoreCase)) is not null)
                {
                    // stop searching further
                    this.encodingParsed = true;
                }
            }

            static bool IsMetaWithCharset(Tag t) =>
                t.Name.Equals("meta", StringComparison.OrdinalIgnoreCase) && t.Attributes.Has("charset");

            static bool IsMetaWithContentType(Tag t) =>
                t.Name.Equals("meta", StringComparison.OrdinalIgnoreCase) &&
                t.Attributes.Has("http-equiv", "Content-Type") && t.Attributes.Has("content");
        }

        private void SetEncoding(string encodingName)
        {
            try
            {
                this.encodingParsed = true;
                this.Encoding = Encoding.GetEncoding(encodingName);
                Debug.WriteLine($"Updated {this.parser.Language} encoding: {encodingName}");
            }
            catch (Exception)
            {
                Debug.WriteLine($"Unrecognized {this.parser.Language} encoding: {encodingName}");
            }
        }
    }
}