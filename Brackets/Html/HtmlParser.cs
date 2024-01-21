namespace Brackets.Html;

using System;
using Collections;

public class HtmlParser : MarkupParser<HtmlLexer>
{
    public HtmlParser() : this(
        new StringSet<TagRef>(HtmlLexer.Comparison),
        new StringSet<AttrRef>(HtmlLexer.Comparison))
    { }

    protected HtmlParser(IStringSet<TagRef> tagReferences, IStringSet<AttrRef> attributeReferences)
        : base(MarkupLanguage.Html, tagReferences, attributeReferences)
    {
        // tags
        //

        // void elements
        AddReference(new TagRef("xml", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagRef("doctype", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagRef("area", this) { IsParent = false });
        AddReference(new TagRef("base", this) { IsParent = false });
        AddReference(new BrTagRef(this));
        AddReference(new TagRef("col", this) { IsParent = false });
        AddReference(new TagRef("embed", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new HrTagRef(this));
        AddReference(new TagRef("img", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagRef("input", this) { IsParent = false, Level = ElementLevel.Inline });
        AddReference(new TagRef("link", this) { IsParent = false });
        AddReference(new TagRef("meta", this) { IsParent = false });
        AddReference(new TagRef("param", this) { IsParent = false });
        AddReference(new TagRef("source", this) { IsParent = false });
        AddReference(new TagRef("track", this) { IsParent = false });
        AddReference(new TagRef("wbr", this) { IsParent = false, Level = ElementLevel.Inline });
        // raw text elements
        AddReference(new TagRef("style", this) { HasRawContent = true });
        AddReference(new TagRef("script", this) { HasRawContent = true, Level = ElementLevel.Inline });
        AddReference(new TagRef("code", this) { HasRawContent = true, Level = ElementLevel.Inline });
        // other inline elements
        AddReference(new TagRef("a", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("abbr", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("acronym", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("audio", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("b", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("bdi", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("bdo", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("big", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("button", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("canvas", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("cite", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("data", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("datalist", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("del", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("dfn", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("em", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("i", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("iframe", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("ins", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("kbd", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("label", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("map", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("mark", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("meter", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("noscript", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("object", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("output", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("picture", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("progress", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("q", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("ruby", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("s", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("samp", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("select", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("slot", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("small", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("span", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("strong", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("sub", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("sup", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("svg", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("template", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("textarea", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("time", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("u", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("tt", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("var", this) { Level = ElementLevel.Inline });
        AddReference(new TagRef("video", this) { Level = ElementLevel.Inline });

        // attributes
        //

        AddReference(new AttrRef("accept", this));
        AddReference(new AttrRef("accept-charset", this));
        AddReference(new AttrRef("accesskey", this));
        AddReference(new AttrRef("action", this));
        AddReference(new AttrRef("align", this));
        AddReference(new AttrRef("allow", this));
        AddReference(new AttrRef("alt", this));
        AddReference(new AttrRef("async", this));
        AddReference(new AttrRef("autocapitalize", this));
        AddReference(new AttrRef("autocomplete", this));
        AddReference(new AttrRef("autoplay", this));
        AddReference(new AttrRef("background", this));
        AddReference(new AttrRef("bgcolor", this));
        AddReference(new AttrRef("border", this));
        AddReference(new AttrRef("buffered", this));
        AddReference(new AttrRef("capture", this));
        AddReference(new AttrRef("charset", this));
        AddReference(new AttrRef("checked", this));
        AddReference(new AttrRef("cite", this));
        AddReference(new AttrRef("class", this));
        AddReference(new AttrRef("color", this));
        AddReference(new AttrRef("cols", this));
        AddReference(new AttrRef("colspan", this));
        AddReference(new AttrRef("content", this));
        AddReference(new AttrRef("contenteditable", this));
        AddReference(new AttrRef("contextmenu", this));
        AddReference(new AttrRef("controls", this));
        AddReference(new AttrRef("coords", this));
        AddReference(new AttrRef("crossorigin", this));
        AddReference(new AttrRef("csp", this));
        AddReference(new AttrRef("data", this));
        AddReference(new AttrRef("datetime", this));
        AddReference(new AttrRef("decoding", this));
        AddReference(new AttrRef("default", this));
        AddReference(new AttrRef("defer", this));
        AddReference(new AttrRef("dir", this));
        AddReference(new AttrRef("dirname", this));
        AddReference(new AttrRef("disabled", this));
        AddReference(new AttrRef("download", this));
        AddReference(new AttrRef("draggable", this));
        AddReference(new AttrRef("enctype", this));
        AddReference(new AttrRef("enterkeyhint", this));
        AddReference(new AttrRef("for", this));
        AddReference(new AttrRef("form", this));
        AddReference(new AttrRef("formaction", this));
        AddReference(new AttrRef("formenctype", this));
        AddReference(new AttrRef("formmethod", this));
        AddReference(new AttrRef("formnovalidate", this));
        AddReference(new AttrRef("formtarget", this));
        AddReference(new AttrRef("headers", this));
        AddReference(new AttrRef("height", this));
        AddReference(new AttrRef("hidden", this));
        AddReference(new AttrRef("high", this));
        AddReference(new AttrRef("href", this));
        AddReference(new AttrRef("hreflang", this));
        AddReference(new AttrRef("http-equiv", this));
        AddReference(new AttrRef("id", this));
        AddReference(new AttrRef("integrity", this));
        AddReference(new AttrRef("intrinsicsize", this));
        AddReference(new AttrRef("inputmode", this));
        AddReference(new AttrRef("ismap", this));
        AddReference(new AttrRef("itemprop", this));
        AddReference(new AttrRef("kind", this));
        AddReference(new AttrRef("label", this));
        AddReference(new AttrRef("lang", this));
        AddReference(new AttrRef("language", this));
        AddReference(new AttrRef("loading", this));
        AddReference(new AttrRef("list", this));
        AddReference(new AttrRef("loop", this));
        AddReference(new AttrRef("low", this));
        AddReference(new AttrRef("manifest", this));
        AddReference(new AttrRef("max", this));
        AddReference(new AttrRef("maxlength", this));
        AddReference(new AttrRef("minlength", this));
        AddReference(new AttrRef("media", this));
        AddReference(new AttrRef("method", this));
        AddReference(new AttrRef("min", this));
        AddReference(new AttrRef("multiple", this));
        AddReference(new AttrRef("muted", this));
        AddReference(new AttrRef("name", this));
        AddReference(new AttrRef("novalidate", this));
        AddReference(new AttrRef("open", this));
        AddReference(new AttrRef("optimum", this));
        AddReference(new AttrRef("pattern", this));
        AddReference(new AttrRef("ping", this));
        AddReference(new AttrRef("placeholder", this));
        AddReference(new AttrRef("playsinline", this));
        AddReference(new AttrRef("poster", this));
        AddReference(new AttrRef("preload", this));
        AddReference(new AttrRef("readonly", this));
        AddReference(new AttrRef("referrerpolicy", this));
        AddReference(new AttrRef("rel", this));
        AddReference(new AttrRef("required", this));
        AddReference(new AttrRef("reversed", this));
        AddReference(new AttrRef("role", this));
        AddReference(new AttrRef("rows", this));
        AddReference(new AttrRef("rowspan", this));
        AddReference(new AttrRef("sandbox", this));
        AddReference(new AttrRef("scope", this));
        AddReference(new AttrRef("scoped", this));
        AddReference(new AttrRef("selected", this));
        AddReference(new AttrRef("shape", this));
        AddReference(new AttrRef("size", this));
        AddReference(new AttrRef("sizes", this));
        AddReference(new AttrRef("slot", this));
        AddReference(new AttrRef("span", this));
        AddReference(new AttrRef("spellcheck", this));
        AddReference(new AttrRef("src", this));
        AddReference(new AttrRef("srcdoc", this));
        AddReference(new AttrRef("srclang", this));
        AddReference(new AttrRef("srcset", this));
        AddReference(new AttrRef("start", this));
        AddReference(new AttrRef("step", this));
        AddReference(new AttrRef("style", this));
        AddReference(new AttrRef("summary", this));
        AddReference(new AttrRef("tabindex", this));
        AddReference(new AttrRef("target", this));
        AddReference(new AttrRef("title", this));
        AddReference(new AttrRef("translate", this));
        AddReference(new AttrRef("type", this));
        AddReference(new AttrRef("usemap", this));
        AddReference(new AttrRef("value", this));
        AddReference(new AttrRef("width", this));
        AddReference(new AttrRef("wrap", this));
    }

    internal static HtmlParser CreateConcurrent() =>
        new(new ConcurrentStringSet<TagRef>(HtmlLexer.Comparison),
            new ConcurrentStringSet<AttrRef>(HtmlLexer.Comparison));

    public override StringComparison Comparison => HtmlLexer.Comparison;
}