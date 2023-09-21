namespace Brackets.Xml;

using Primitives;

public class XmlReference : Document.MarkupReference
{
    public XmlReference() : base(new MarkupSyntax
    {
        Opener = '<',
        Closer = '>',
        Terminator = '/',
        EqSign = '=',
        AltOpener = '?',
        Separators = " \r\n\t\xA0",
        AttrSeparators = "= \r\n\t\xA0",
        QuotationMarks = "'\"",
        CommentOpener = "<!--",
        CommentCloser = "-->",
        SectionOpener = "<![",
        SectionCloser = "]]>",
    })
    {
        // void elements
        AddReference(new TagReference("?xml", this) { IsParent = false });
    }
}
