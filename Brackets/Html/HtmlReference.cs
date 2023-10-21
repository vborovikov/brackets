﻿namespace Brackets.Html;

using Collections;

public class HtmlReference : MarkupReference<HtmlLexer>
{
    public HtmlReference() : this(
        new StringSet<TagReference>(HtmlLexer.Comparison),
        new StringSet<AttributeReference>(HtmlLexer.Comparison))
    { }

    protected HtmlReference(IStringSet<TagReference> tagReferences, IStringSet<AttributeReference> attributeReferences)
        : base(MarkupLanguage.Html, tagReferences, attributeReferences)
    {
        // tags
        //

        // void elements
        AddReference(new TagReference("xml", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagReference("doctype", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
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

        // attributes
        //

        AddReference(new AttributeReference("accept", this));
        AddReference(new AttributeReference("accept-charset", this));
        AddReference(new AttributeReference("accesskey", this));
        AddReference(new AttributeReference("action", this));
        AddReference(new AttributeReference("align", this));
        AddReference(new AttributeReference("allow", this));
        AddReference(new AttributeReference("alt", this));
        AddReference(new AttributeReference("async", this));
        AddReference(new AttributeReference("autocapitalize", this));
        AddReference(new AttributeReference("autocomplete", this));
        AddReference(new AttributeReference("autoplay", this));
        AddReference(new AttributeReference("background", this));
        AddReference(new AttributeReference("bgcolor", this));
        AddReference(new AttributeReference("border", this));
        AddReference(new AttributeReference("buffered", this));
        AddReference(new AttributeReference("capture", this));
        AddReference(new AttributeReference("charset", this));
        AddReference(new AttributeReference("checked", this));
        AddReference(new AttributeReference("cite", this));
        AddReference(new AttributeReference("class", this));
        AddReference(new AttributeReference("color", this));
        AddReference(new AttributeReference("cols", this));
        AddReference(new AttributeReference("colspan", this));
        AddReference(new AttributeReference("content", this));
        AddReference(new AttributeReference("contenteditable", this));
        AddReference(new AttributeReference("contextmenu", this));
        AddReference(new AttributeReference("controls", this));
        AddReference(new AttributeReference("coords", this));
        AddReference(new AttributeReference("crossorigin", this));
        AddReference(new AttributeReference("csp", this));
        AddReference(new AttributeReference("data", this));
        AddReference(new AttributeReference("datetime", this));
        AddReference(new AttributeReference("decoding", this));
        AddReference(new AttributeReference("default", this));
        AddReference(new AttributeReference("defer", this));
        AddReference(new AttributeReference("dir", this));
        AddReference(new AttributeReference("dirname", this));
        AddReference(new AttributeReference("disabled", this));
        AddReference(new AttributeReference("download", this));
        AddReference(new AttributeReference("draggable", this));
        AddReference(new AttributeReference("enctype", this));
        AddReference(new AttributeReference("enterkeyhint", this));
        AddReference(new AttributeReference("for", this));
        AddReference(new AttributeReference("form", this));
        AddReference(new AttributeReference("formaction", this));
        AddReference(new AttributeReference("formenctype", this));
        AddReference(new AttributeReference("formmethod", this));
        AddReference(new AttributeReference("formnovalidate", this));
        AddReference(new AttributeReference("formtarget", this));
        AddReference(new AttributeReference("headers", this));
        AddReference(new AttributeReference("height", this));
        AddReference(new AttributeReference("hidden", this));
        AddReference(new AttributeReference("high", this));
        AddReference(new AttributeReference("href", this));
        AddReference(new AttributeReference("hreflang", this));
        AddReference(new AttributeReference("http-equiv", this));
        AddReference(new AttributeReference("id", this));
        AddReference(new AttributeReference("integrity", this));
        AddReference(new AttributeReference("intrinsicsize", this));
        AddReference(new AttributeReference("inputmode", this));
        AddReference(new AttributeReference("ismap", this));
        AddReference(new AttributeReference("itemprop", this));
        AddReference(new AttributeReference("kind", this));
        AddReference(new AttributeReference("label", this));
        AddReference(new AttributeReference("lang", this));
        AddReference(new AttributeReference("language", this));
        AddReference(new AttributeReference("loading", this));
        AddReference(new AttributeReference("list", this));
        AddReference(new AttributeReference("loop", this));
        AddReference(new AttributeReference("low", this));
        AddReference(new AttributeReference("manifest", this));
        AddReference(new AttributeReference("max", this));
        AddReference(new AttributeReference("maxlength", this));
        AddReference(new AttributeReference("minlength", this));
        AddReference(new AttributeReference("media", this));
        AddReference(new AttributeReference("method", this));
        AddReference(new AttributeReference("min", this));
        AddReference(new AttributeReference("multiple", this));
        AddReference(new AttributeReference("muted", this));
        AddReference(new AttributeReference("name", this));
        AddReference(new AttributeReference("novalidate", this));
        AddReference(new AttributeReference("open", this));
        AddReference(new AttributeReference("optimum", this));
        AddReference(new AttributeReference("pattern", this));
        AddReference(new AttributeReference("ping", this));
        AddReference(new AttributeReference("placeholder", this));
        AddReference(new AttributeReference("playsinline", this));
        AddReference(new AttributeReference("poster", this));
        AddReference(new AttributeReference("preload", this));
        AddReference(new AttributeReference("readonly", this));
        AddReference(new AttributeReference("referrerpolicy", this));
        AddReference(new AttributeReference("rel", this));
        AddReference(new AttributeReference("required", this));
        AddReference(new AttributeReference("reversed", this));
        AddReference(new AttributeReference("role", this));
        AddReference(new AttributeReference("rows", this));
        AddReference(new AttributeReference("rowspan", this));
        AddReference(new AttributeReference("sandbox", this));
        AddReference(new AttributeReference("scope", this));
        AddReference(new AttributeReference("scoped", this));
        AddReference(new AttributeReference("selected", this));
        AddReference(new AttributeReference("shape", this));
        AddReference(new AttributeReference("size", this));
        AddReference(new AttributeReference("sizes", this));
        AddReference(new AttributeReference("slot", this));
        AddReference(new AttributeReference("span", this));
        AddReference(new AttributeReference("spellcheck", this));
        AddReference(new AttributeReference("src", this));
        AddReference(new AttributeReference("srcdoc", this));
        AddReference(new AttributeReference("srclang", this));
        AddReference(new AttributeReference("srcset", this));
        AddReference(new AttributeReference("start", this));
        AddReference(new AttributeReference("step", this));
        AddReference(new AttributeReference("style", this));
        AddReference(new AttributeReference("summary", this));
        AddReference(new AttributeReference("tabindex", this));
        AddReference(new AttributeReference("target", this));
        AddReference(new AttributeReference("title", this));
        AddReference(new AttributeReference("translate", this));
        AddReference(new AttributeReference("type", this));
        AddReference(new AttributeReference("usemap", this));
        AddReference(new AttributeReference("value", this));
        AddReference(new AttributeReference("width", this));
        AddReference(new AttributeReference("wrap", this));
    }

    internal static HtmlReference CreateConcurrent() =>
        new(new ConcurrentStringSet<TagReference>(HtmlLexer.Comparison),
            new ConcurrentStringSet<AttributeReference>(HtmlLexer.Comparison));
}