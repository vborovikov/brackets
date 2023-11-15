# Brackets
Resilient markup parser library

[![Downloads](https://img.shields.io/nuget/dt/Brackets.svg)](https://www.nuget.org/packages/Brackets)
[![NuGet](https://img.shields.io/nuget/v/Brackets.svg)](https://www.nuget.org/packages/Brackets)
[![BSD-3-Clause](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](https://github.com/vborovikov/brackets/blob/main/LICENSE)

The library is used to parse XML and HTML files. The parser produces a tree of nodes that represent the structure of the document. The parse tree is very simple by design and doesn't try to replicate the document object model (DOM) in any way.

Ill-structured documents will be parsed without errors. The parser will try to detect and correct stray tags, broken tags, etc.

## Usage

Both HTML and XML parsers are derived from the `MarkupParser<TMarkupLexer>` class and are used in the same way. You can access the parsers using the `Document.Html` and the `Document.Xml` static properties or by instantiating the `HtmlParser` and the `XmlParser` classes. The parsers provided by the static properties of the `Document` class are thread-safe and can be used in multiple threads simultaneously. The parsers instantiated directly are not thread-safe but can be slightly faster.

To parse a document from a string, use the `Parse` method of the `MarkupParser` class.

```csharp
// Parse a string
var document = Document.Html.Parse("<html><head></head><body></body></html>");
// Search for a body element using XPath
var body = document.Find("/html/body").FirstOrDefault() as ParentTag;
```

To parse a document from a file or any stream, use the `ParseAsync` method of the `MarkupParser` class.

```csharp
// Parse a stream
var document = await Document.Html.ParseAsync(stream, cancellationToken);
// Search for a body element using XPath
var body = document.Find("/html/body").FirstOrDefault() as ParentTag;
```

`ParseAsync` can also accept an `encoding` parameter that specifies the encoding of the document. The default encoding is UTF-8. In any case the parser will automatically detect the encoding of the document from the markup and update it on the fly.