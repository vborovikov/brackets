namespace Brackets.Html;

using System;
using Collections;
using static FlowLayout;
using static ContentCategory;

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
        AddReference(new TagRef("xml", this) { IsParent = false, Layout = Inline, IsProcessingInstruction = true });
        AddReference(new TagRef("doctype", this) { IsParent = false, Layout = Inline, IsProcessingInstruction = true });
        AddReference(new TagRef("area", this) { IsParent = false });
        AddReference(new TagRef("base", this) { IsParent = false, Category = Metadata });
        AddReference(new BrTagRef(this));
        AddReference(new TagRef("col", this) { IsParent = false });
        AddReference(new TagRef("embed", this) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Embedded | Interactive });
        AddReference(new HrTagRef(this));
        AddReference(new TagRef("img", this) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Embedded });
        AddReference(new TagRef("input", this) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Form });
        AddReference(new TagRef("link", this) { IsParent = false, Category = Metadata });
        AddReference(new TagRef("meta", this) { IsParent = false, Category = Metadata });
        AddReference(new TagRef("param", this) { IsParent = false });
        AddReference(new TagRef("source", this) { IsParent = false });
        AddReference(new TagRef("track", this) { IsParent = false });
        AddReference(new TagRef("wbr", this) { IsParent = false, Layout = Inline, Category = Flow | Phrasing });
        // raw text elements
        AddReference(new TagRef("style", this) { HasRawContent = true, Category = Metadata });
        AddReference(new TagRef("script", this) { HasRawContent = true, Layout = Inline, Category = Metadata | Flow | Phrasing | Script });
        AddReference(new TagRef("code", this) { Layout = Inline, Category = Flow | Phrasing });
        // other inline elements
        AddReference(new TagRef("a", this) { Layout = Inline });
        AddReference(new TagRef("abbr", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("acronym", this) { Layout = Inline });
        AddReference(new TagRef("audio", this) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        AddReference(new TagRef("b", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("bdi", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("bdo", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("big", this) { Layout = Inline });
        AddReference(new TagRef("button", this) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form });
        AddReference(new TagRef("canvas", this) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        AddReference(new TagRef("cite", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("data", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("datalist", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("del", this) { Layout = Inline });
        AddReference(new TagRef("dfn", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("em", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("i", this) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        AddReference(new TagRef("iframe", this) { Layout = Inline, Category = Flow | Phrasing | Embedded | Interactive });
        AddReference(new TagRef("ins", this) { Layout = Inline });
        AddReference(new TagRef("kbd", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("label", this) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form });
        AddReference(new TagRef("map", this) { Layout = Inline });
        AddReference(new TagRef("mark", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("meter", this) { Layout = Inline, Category = Flow | Phrasing | Form });
        AddReference(new TagRef("noscript", this) { Layout = Inline, Category = Metadata | Flow | Phrasing });
        AddReference(new TagRef("object", this) { Layout = Inline, Category = Flow | Phrasing | Embedded | Form });
        AddReference(new TagRef("output", this) { Layout = Inline, Category = Flow | Phrasing | Form });
        AddReference(new TagRef("picture", this) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        AddReference(new TagRef("progress", this) { Layout = Inline, Category = Flow | Phrasing | Form });
        AddReference(new TagRef("q", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("ruby", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("s", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("samp", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("select", this) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form });
        AddReference(new TagRef("slot", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("small", this) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        AddReference(new TagRef("span", this) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        AddReference(new TagRef("strong", this) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        AddReference(new TagRef("sub", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("sup", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("svg", this) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        AddReference(new TagRef("template", this) { Layout = Inline, Category = Flow | Phrasing | Script });
        AddReference(new TagRef("textarea", this) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form });
        AddReference(new TagRef("time", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("u", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("tt", this) { Layout = Inline });
        AddReference(new TagRef("var", this) { Layout = Inline, Category = Flow | Phrasing });
        AddReference(new TagRef("video", this) { Layout = Inline, Category = Flow | Phrasing | Embedded });

        // more elements
        AddReference(new TagRef("title", this) { Category = Metadata });
        
        AddReference(new TagRef("article", this) { Category = Flow | Sectioning });
        AddReference(new TagRef("aside", this) { Category = Flow | Sectioning });
        AddReference(new TagRef("nav", this) { Category = Flow | Sectioning });
        AddReference(new TagRef("section", this) { Category = Flow | Sectioning });
        
        AddReference(new TagRef("h1", this) { Category = Flow | Heading });
        AddReference(new TagRef("h2", this) { Category = Flow | Heading });
        AddReference(new TagRef("h3", this) { Category = Flow | Heading });
        AddReference(new TagRef("h4", this) { Category = Flow | Heading });
        AddReference(new TagRef("h5", this) { Category = Flow | Heading });
        AddReference(new TagRef("h6", this) { Category = Flow | Heading });
        AddReference(new TagRef("hgroup", this) { Category = Flow | Heading });

        AddReference(new TagRef("math", this) { Category = Flow | Phrasing | Embedded });
        AddReference(new TagRef("details", this) { Category = Flow | Interactive });
        AddReference(new TagRef("fieldset", this) { Category = Flow | Form });

        // block elements
        AddReference(new TagRef("p", this) { PermittedContent = Phrasing });

        //
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