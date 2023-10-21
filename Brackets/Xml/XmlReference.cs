namespace Brackets.Xml;

using Collections;

public class XmlReference : MarkupReference<XmlLexer>
{
    public XmlReference() : this(
        new StringSet<TagReference>(XmlLexer.Comparison),
        new StringSet<AttributeReference>(XmlLexer.Comparison))
    { }

    protected XmlReference(IStringSet<TagReference> tagReferences, IStringSet<AttributeReference> attributeReferences)
        : base(MarkupLanguage.Xml, tagReferences, attributeReferences)
    {
        // void elements
        AddReference(new TagReference("xml", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagReference("doctype", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagReference("xml-stylesheet", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
    }

    internal static XmlReference CreateConcurrent() =>
        new(new ConcurrentStringSet<TagReference>(XmlLexer.Comparison), 
            new ConcurrentStringSet<AttributeReference>(XmlLexer.Comparison));
}
