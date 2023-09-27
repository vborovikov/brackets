namespace Brackets.Xml;

public class XmlReference : Document.MarkupReference<XmlLexer>
{
    public XmlReference()
    {
        // void elements
        AddReference(new TagReference("?xml", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagReference("?xml-stylesheet", this) { IsParent = false, Level = ElementLevel.Inline });
    }
}
