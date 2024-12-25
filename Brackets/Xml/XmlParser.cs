namespace Brackets.Xml;

public class XmlParser : MarkupParser<XmlLexer>
{
    public XmlParser() : this(isThreadSafe: false) { }

    protected XmlParser(bool isThreadSafe) : base(MarkupLanguage.Xml, isThreadSafe)
    {
        AddKnownRefs(this);
    }

    /// <inheritdoc/>
    public override StringComparison Comparison => XmlLexer.Comparison;

    internal static XmlParser CreateConcurrent() => new(isThreadSafe: true);

    internal static void AddKnownRefs(IMarkupParser parser)
    {
        // void elements
        parser.AddTagRef(new("xml", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
        parser.AddTagRef(new("doctype", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });
        parser.AddTagRef(new("xml-stylesheet", parser) { IsParent = false, Layout = FlowLayout.Inline, IsProcessingInstruction = true });

        // attributes
        parser.AddAttrRef(new("xmlns", parser));
    }
}
