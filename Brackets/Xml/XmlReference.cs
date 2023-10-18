namespace Brackets.Xml;

public class XmlReference : MarkupReference<XmlLexer>
{
    public XmlReference()
    {
        // void elements
        AddReference(new TagReference("xml", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagReference("doctype", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagReference("xml-stylesheet", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
    }
}
