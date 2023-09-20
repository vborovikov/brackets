namespace Brackets.Xml;

public class XmlReference : Document.MarkupReference
{
    public XmlReference()
    {
        // void elements
        AddReference(new TagReference("?xml") { IsParent = false });
    }
}
