namespace Brackets.Html;

public class HtmlReference : Document.MarkupReference<HtmlLexer>
{
    public HtmlReference()
    {
        // void elements
        AddReference(new TagReference("!doctype", this) { IsParent = false });
        AddReference(new TagReference("area", this) { IsParent = false });
        AddReference(new TagReference("base", this) { IsParent = false });
        AddReference(new BrTagReference(this));
        AddReference(new TagReference("col", this) { IsParent = false });
        AddReference(new TagReference("embed", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new HrTagReference(this));
        AddReference(new TagReference("img", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagReference("input", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagReference("link", this) { IsParent = false });
        AddReference(new TagReference("meta", this) { IsParent = false });
        AddReference(new TagReference("param", this) { IsParent = false });
        AddReference(new TagReference("source", this) { IsParent = false });
        AddReference(new TagReference("track", this) { IsParent = false });
        AddReference(new TagReference("wbr", this) { IsParent = false, Level = ElementLevel.Inline });
        // raw text elements
        AddReference(new TagReference("style", this) { HasRawContent = true });
        AddReference(new TagReference("script", this) { HasRawContent = true, Level = ElementLevel.Inline });
        AddReference(new TagReference("code", this) { HasRawContent = true, Level = ElementLevel.Inline });
        // other inline elements
        AddReference(new TagReference("a", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("abbr", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("acronym", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("audio", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("b", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("bdi", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("bdo", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("big", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("button", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("canvas", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("cite", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("data", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("datalist", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("del", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("dfn", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("em", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("i", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("iframe", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("ins", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("kbd", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("label", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("map", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("mark", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("meter", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("noscript", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("object", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("output", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("picture", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("progress", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("q", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("ruby", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("s", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("samp", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("select", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("slot", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("small", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("span", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("strong", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("sub", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("sup", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("svg", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("template", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("textarea", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("time", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("u", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("tt", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("var", this) { Level = ElementLevel.Inline });
        AddReference(new TagReference("video", this) { Level = ElementLevel.Inline });
    }
}