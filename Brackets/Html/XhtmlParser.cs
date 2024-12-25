namespace Brackets.Html;

using System;
using Xml;

public class XhtmlParser : MarkupParser<XmlLexer>
{
    public XhtmlParser() : this(isThreadSafe: false) { }

    protected XhtmlParser(bool isThreadSafe) : base(MarkupLanguage.Xhtml, isThreadSafe)
    {
        AddKnownRefs(this);
    }

    /// <inheritdoc/>
    public override StringComparison Comparison => XmlLexer.Comparison;

    internal static XhtmlParser CreateConcurrent() => new(isThreadSafe: true);

    internal static void AddKnownRefs(IMarkupParser parser)
    {
        XmlParser.AddKnownRefs(parser);
        HtmlParser.AddKnownRefs(parser);

        parser.AddAttrRef(new("xml:lang", parser));
    }
}
