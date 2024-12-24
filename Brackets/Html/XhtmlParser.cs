namespace Brackets.Html;

using System;
using Collections;
using Xml;

public class XhtmlParser : MarkupParser<XmlLexer>
{
    public XhtmlParser() : this(
        new StringSet<TagRef>(XmlLexer.Comparison),
        new StringSet<AttrRef>(XmlLexer.Comparison))
    { }

    protected XhtmlParser(IStringSet<TagRef> tagReferences, IStringSet<AttrRef> attributeReferences)
        : base(MarkupLanguage.Xhtml, tagReferences, attributeReferences)
    {
        AddKnownRefs(this);
    }

    public override StringComparison Comparison => XmlLexer.Comparison;

    internal static void AddKnownRefs(IMarkupParser parser)
    {
        XmlParser.AddKnownRefs(parser);
        HtmlParser.AddKnownRefs(parser);

        parser.AddAttrRef(new("xml:lang", parser));
    }

    internal static XhtmlParser CreateConcurrent() =>
        new(new ConcurrentStringSet<TagRef>(XmlLexer.Comparison),
            new ConcurrentStringSet<AttrRef>(XmlLexer.Comparison));
}
