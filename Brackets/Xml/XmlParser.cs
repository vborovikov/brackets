namespace Brackets.Xml;

using Collections;

public class XmlParser : MarkupParser<XmlLexer>
{
    public XmlParser() : this(
        new StringSet<TagRef>(XmlLexer.Comparison),
        new StringSet<AttrRef>(XmlLexer.Comparison))
    { }

    protected XmlParser(IStringSet<TagRef> tagReferences, IStringSet<AttrRef> attributeReferences)
        : base(MarkupLanguage.Xml, tagReferences, attributeReferences)
    {
        AddKnownRefs(this);
    }

    internal static void AddKnownRefs(IMarkupParser parser)
    {
        // void elements
        parser.AddTagRef(new("xml", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
        parser.AddTagRef(new("doctype", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
        parser.AddTagRef(new("xml-stylesheet", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
    }

    internal static XmlParser CreateConcurrent() =>
        new(new ConcurrentStringSet<TagRef>(XmlLexer.Comparison), 
            new ConcurrentStringSet<AttrRef>(XmlLexer.Comparison));

    public override StringComparison Comparison => XmlLexer.Comparison;
}
