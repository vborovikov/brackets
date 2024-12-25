namespace Brackets.Xml;

public class XmlParser : MarkupParser<XmlLexer>
{
    public XmlParser() : this(isThreadSafe: false) { }

    protected XmlParser(bool isThreadSafe) : base(MarkupLanguage.Xml, isThreadSafe)
    {
        InitKnownRefs(this);
    }

    /// <inheritdoc/>
    public override StringComparison Comparison => XmlLexer.Comparison;

    internal static XmlParser CreateConcurrent() => new(isThreadSafe: true);

    internal static void InitKnownRefs(IMarkupParser parser)
    {
        // void elements
        parser.InitTagRef(new("xml", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
        parser.InitTagRef(new("doctype", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
        parser.InitTagRef(new("xml-stylesheet", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });

        // attributes
        parser.InitAttrRef(new("xmlns", parser));
    }
}
