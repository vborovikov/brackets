namespace Brackets.Html;

using Primitives;

public class HtmlReference : Document.MarkupReference
{
    public HtmlReference() : base(new MarkupSyntax
    {
        Opener = '<',
        Closer = '>',
        Slash = '/',
        EqSign = '=',
        AltOpener = '!',
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
        AddReference(new TagReference("!doctype") { IsParent = false });
        AddReference(new TagReference("area") { IsParent = false });
        AddReference(new TagReference("base") { IsParent = false });
        AddReference(new BrTagReference());
        AddReference(new TagReference("col") { IsParent = false });
        AddReference(new TagReference("embed") { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new HrTagReference());
        AddReference(new TagReference("img") { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagReference("input") { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagReference("link") { IsParent = false });
        AddReference(new TagReference("meta") { IsParent = false });
        AddReference(new TagReference("param") { IsParent = false });
        AddReference(new TagReference("source") { IsParent = false });
        AddReference(new TagReference("track") { IsParent = false });
        AddReference(new TagReference("wbr") { IsParent = false, Level = ElementLevel.Inline });
        // raw text elements
        AddReference(new TagReference("style") { HasRawContent = true });
        AddReference(new TagReference("script") { HasRawContent = true, Level = ElementLevel.Inline });
        // other inline elements
        AddReference(new TagReference("a") { Level = ElementLevel.Inline });
        AddReference(new TagReference("abbr") { Level = ElementLevel.Inline });
        AddReference(new TagReference("acronym") { Level = ElementLevel.Inline });
        AddReference(new TagReference("audio") { Level = ElementLevel.Inline });
        AddReference(new TagReference("b") { Level = ElementLevel.Inline });
        AddReference(new TagReference("bdi") { Level = ElementLevel.Inline });
        AddReference(new TagReference("bdo") { Level = ElementLevel.Inline });
        AddReference(new TagReference("big") { Level = ElementLevel.Inline });
        AddReference(new TagReference("button") { Level = ElementLevel.Inline });
        AddReference(new TagReference("canvas") { Level = ElementLevel.Inline });
        AddReference(new TagReference("cite") { Level = ElementLevel.Inline });
        AddReference(new TagReference("code") { Level = ElementLevel.Inline });
        AddReference(new TagReference("data") { Level = ElementLevel.Inline });
        AddReference(new TagReference("datalist") { Level = ElementLevel.Inline });
        AddReference(new TagReference("del") { Level = ElementLevel.Inline });
        AddReference(new TagReference("dfn") { Level = ElementLevel.Inline });
        AddReference(new TagReference("em") { Level = ElementLevel.Inline });
        AddReference(new TagReference("i") { Level = ElementLevel.Inline });
        AddReference(new TagReference("iframe") { Level = ElementLevel.Inline });
        AddReference(new TagReference("ins") { Level = ElementLevel.Inline });
        AddReference(new TagReference("kbd") { Level = ElementLevel.Inline });
        AddReference(new TagReference("label") { Level = ElementLevel.Inline });
        AddReference(new TagReference("map") { Level = ElementLevel.Inline });
        AddReference(new TagReference("mark") { Level = ElementLevel.Inline });
        AddReference(new TagReference("meter") { Level = ElementLevel.Inline });
        AddReference(new TagReference("noscript") { Level = ElementLevel.Inline });
        AddReference(new TagReference("object") { Level = ElementLevel.Inline });
        AddReference(new TagReference("output") { Level = ElementLevel.Inline });
        AddReference(new TagReference("picture") { Level = ElementLevel.Inline });
        AddReference(new TagReference("progress") { Level = ElementLevel.Inline });
        AddReference(new TagReference("q") { Level = ElementLevel.Inline });
        AddReference(new TagReference("ruby") { Level = ElementLevel.Inline });
        AddReference(new TagReference("s") { Level = ElementLevel.Inline });
        AddReference(new TagReference("samp") { Level = ElementLevel.Inline });
        AddReference(new TagReference("select") { Level = ElementLevel.Inline });
        AddReference(new TagReference("slot") { Level = ElementLevel.Inline });
        AddReference(new TagReference("small") { Level = ElementLevel.Inline });
        AddReference(new TagReference("span") { Level = ElementLevel.Inline });
        AddReference(new TagReference("strong") { Level = ElementLevel.Inline });
        AddReference(new TagReference("sub") { Level = ElementLevel.Inline });
        AddReference(new TagReference("sup") { Level = ElementLevel.Inline });
        AddReference(new TagReference("svg") { Level = ElementLevel.Inline });
        AddReference(new TagReference("template") { Level = ElementLevel.Inline });
        AddReference(new TagReference("textarea") { Level = ElementLevel.Inline });
        AddReference(new TagReference("time") { Level = ElementLevel.Inline });
        AddReference(new TagReference("u") { Level = ElementLevel.Inline });
        AddReference(new TagReference("tt") { Level = ElementLevel.Inline });
        AddReference(new TagReference("var") { Level = ElementLevel.Inline });
        AddReference(new TagReference("video") { Level = ElementLevel.Inline });
    }
}