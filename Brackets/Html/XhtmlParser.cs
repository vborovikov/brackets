namespace Brackets.Html;

using System;
using Xml;

public class XhtmlParser : MarkupParser<XmlLexer>
{
    public XhtmlParser() : this(isThreadSafe: false) { }

    protected XhtmlParser(bool isThreadSafe) : base(MarkupLanguage.Xhtml, isThreadSafe)
    {
        InitKnownRefs(this);
    }

    /// <inheritdoc/>
    public override StringComparison Comparison => XmlLexer.Comparison;

    internal static XhtmlParser CreateConcurrent() => new(isThreadSafe: true);

    internal static void InitKnownRefs(IMarkupParser parser)
    {
        XmlParser.InitKnownRefs(parser);
        HtmlParser.InitKnownRefs(parser);

        parser.InitAttrRef(new("xml:lang", parser));
    }
}
