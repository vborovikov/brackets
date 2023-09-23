namespace Brackets.Xml;

using Primitives;

public class XmlReference : Document.MarkupReference
{
    public XmlReference() : base(new MarkupSyntax
    {
        Comparison = StringComparison.Ordinal,
        Opener = '<',
        Closer = '>',
        Terminator = '/',
        EqSign = '=',
        AltOpener = '?',
        Separators = " \r\n\t\xA0",
        AttrSeparators = "= \r\n\t\xA0",
        QuotationMarks = "'\"",
        CommentOpener = "<!--",
        CommentOpenerNB = "!--",
        CommentCloser = "-->",
        SectionOpener = "<![",
        SectionOpenerNB = "![",
        SectionCloser = "]]>",
        ContentOpener = '[',
    })
    {
        // void elements
        AddReference(new TagReference("?xml", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagReference("?xml-stylesheet", this) { IsParent = false, Level = ElementLevel.Inline });
    }
}
