namespace Brackets.Html;

using static ContentCategory;
using static FlowLayout;

public class HtmlParser : MarkupParser<HtmlLexer>
{
    public HtmlParser() : this(isThreadSafe: false) { }

    protected HtmlParser(bool isThreadSafe) : base(MarkupLanguage.Html, isThreadSafe)
    {
        AddKnownRefs(this);
    }

    /// <inheritdoc/>
    public override StringComparison Comparison => HtmlLexer.Comparison;

    internal static HtmlParser CreateConcurrent() => new(isThreadSafe: true);

    internal static void AddKnownRefs(IMarkupParser parser)
    {
        //
        // tags
        //

        // structural
        parser.AddTagRef(new("a", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive, PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("article", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.AddTagRef(new("aside", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.AddTagRef(new("body", parser) { PermittedContent = Flow });
        parser.AddTagRef(new BrTagRef(parser));
        parser.AddTagRef(new("details", parser) { Category = Flow | Interactive, PermittedContent = Flow });
        parser.AddTagRef(new("div", parser) { Category = Flow, PermittedContent = Flow });
        parser.AddTagRef(new("head", parser) { PermittedContent = Metadata });
        parser.AddTagRef(new("header", parser) { Category = Flow, PermittedContent = Flow });
        parser.AddTagRef(new("hgroup", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new("h1", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new("h2", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new("h3", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new("h4", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new("h5", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new("h6", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.AddTagRef(new HrTagRef(parser));
        parser.AddTagRef(new("html", parser));
        parser.AddTagRef(new("footer", parser) { Category = Flow, PermittedContent = Flow });
        parser.AddTagRef(new("main", parser) { Category = Flow, PermittedContent = Flow });
        parser.AddTagRef(new("nav", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.AddTagRef(new("p", parser) { Category = Flow, PermittedContent = Phrasing });
        parser.AddTagRef(new("section", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.AddTagRef(new("span", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("summary", parser) { PermittedContent = Phrasing | Heading });

        // metadata
        parser.AddTagRef(new("base", parser) { IsParent = false, Category = Metadata });
        parser.AddTagRef(new("doctype", parser) { IsParent = false, Layout = Inline, IsProcessingInstruction = true });
        parser.AddTagRef(new("basefont", parser) { IsParent = false, Category = Metadata });
        parser.AddTagRef(new("link", parser) { IsParent = false, Category = Metadata });
        parser.AddTagRef(new("meta", parser) { IsParent = false, Category = Metadata });
        parser.AddTagRef(new("style", parser) { ParsingMode = ParsingMode.RawContent, Category = Metadata });
        parser.AddTagRef(new("title", parser) { Category = Metadata });
        parser.AddTagRef(new("xml", parser) { IsParent = false, Layout = Inline, IsProcessingInstruction = true });

        // form
        parser.AddTagRef(new("button", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Phrasing });
        parser.AddTagRef(new("datalist", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Form });
        parser.AddTagRef(new("dialog", parser) { Layout = Inline, Category = Flow | Form, PermittedContent = Flow });
        parser.AddTagRef(new("fieldset", parser) { Category = Flow | Form, PermittedContent = Flow });
        parser.AddTagRef(new("form", parser) { Layout = Inline, Category = Flow | Form, PermittedContent = Flow });
        parser.AddTagRef(new("input", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Form });
        parser.AddTagRef(new("keygen", parser) { IsParent = false, Layout = Inline, Category = Flow | Form });
        parser.AddTagRef(new("label", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Phrasing });
        parser.AddTagRef(new("legend", parser) { Layout = Inline, Category = Form, PermittedContent = Phrasing | Heading });
        parser.AddTagRef(new("meter", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Phrasing });
        parser.AddTagRef(new("optgroup", parser) { Category = Form, PermittedContent = Form });
        parser.AddTagRef(new("option", parser) { Layout = Inline, Category = Form, PermittedContent = Phrasing });
        parser.AddTagRef(new("select", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Form });
        parser.AddTagRef(new("textarea", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Phrasing });

        // formatting
        parser.AddTagRef(new("abbr", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("acronym", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("address", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow });
        parser.AddTagRef(new("b", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("bdi", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("bdo", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("big", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("blockquote", parser) { Layout = Block, Category = Flow, PermittedContent = Flow });
        parser.AddTagRef(new("center", parser) { Layout = Block, Category = Flow, PermittedContent = Flow });
        parser.AddTagRef(new("cite", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("code", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("data", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("del", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("dfn", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("em", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("font", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("i", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("ins", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("kbd", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("mark", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("output", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Phrasing });
        parser.AddTagRef(new("pre", parser) { ParsingMode = ParsingMode.Formatting, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("progress", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Phrasing });
        parser.AddTagRef(new("q", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("rb", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("rp", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("rt", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("rtc", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("ruby", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("s", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("samp", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("small", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("strike", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("strong", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("sub", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("sup", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("tt", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("u", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("var", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("wbr", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing });

        // list
        parser.AddTagRef(new("dd", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("dir", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("dl", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("dt", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("li", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("ol", parser) { Category = Flow });
        parser.AddTagRef(new("menu", parser) { Category = Flow });
        parser.AddTagRef(new("menuitem", parser) { IsParent = false, Layout = Inline, Category = Interactive });
        parser.AddTagRef(new("ul", parser) { Category = Flow });

        // table
        parser.AddTagRef(new("caption", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("col", parser) { IsParent = false });
        parser.AddTagRef(new("colgroup", parser) { });
        parser.AddTagRef(new("table", parser) { Category = Flow });
        parser.AddTagRef(new("tbody", parser) { });
        parser.AddTagRef(new("td", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("tfoot", parser) { });
        parser.AddTagRef(new("thead", parser) { });
        parser.AddTagRef(new("th", parser) { PermittedContent = Flow });
        parser.AddTagRef(new("tr", parser) { });

        // scripting
        parser.AddTagRef(new("noscript", parser) { Layout = Inline, Category = Metadata | Flow | Phrasing | Script });
        parser.AddTagRef(new("script", parser) { ParsingMode = ParsingMode.RawContent, Layout = Inline, Category = Metadata | Flow | Phrasing | Script });
        parser.AddTagRef(new("template", parser) { Layout = Inline, Category = Metadata | Flow | Phrasing | Script });

        // embedded
        parser.AddTagRef(new("applet", parser) { });
        parser.AddTagRef(new("area", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing });
        parser.AddTagRef(new("audio", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.AddTagRef(new("canvas", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded, PermittedContent = Interactive });
        parser.AddTagRef(new("embed", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Embedded | Interactive });
        parser.AddTagRef(new("figcaption", parser) { PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("figure", parser) { Category = Flow, PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("frame", parser) { IsParent = false, Category = Embedded });
        parser.AddTagRef(new("frameset", parser) { Category = Embedded });
        parser.AddTagRef(new("iframe", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded | Interactive });
        parser.AddTagRef(new("img", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.AddTagRef(new("map", parser) { Layout = Inline, Category = Flow | Phrasing });
        parser.AddTagRef(new("math", parser) { Category = Flow | Phrasing | Embedded });
        parser.AddTagRef(new("noframes", parser) { Category = Embedded, PermittedContent = Flow });
        parser.AddTagRef(new("object", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded | Form, PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("param", parser) { IsParent = false });
        parser.AddTagRef(new("picture", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.AddTagRef(new("slot", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow | Phrasing });
        parser.AddTagRef(new("source", parser) { IsParent = false });
        parser.AddTagRef(new("svg", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.AddTagRef(new("time", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.AddTagRef(new("track", parser) { IsParent = false });
        parser.AddTagRef(new("video", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });

        //
        // attributes
        //

        parser.AddAttrRef(new("accept", parser));
        parser.AddAttrRef(new("accept-charset", parser));
        parser.AddAttrRef(new("accesskey", parser));
        parser.AddAttrRef(new("action", parser));
        parser.AddAttrRef(new("align", parser));
        parser.AddAttrRef(new("allow", parser));
        parser.AddAttrRef(new("alt", parser));
        parser.AddAttrRef(new("async", parser));
        parser.AddAttrRef(new("autocapitalize", parser));
        parser.AddAttrRef(new("autocomplete", parser));
        parser.AddAttrRef(new("autoplay", parser));
        parser.AddAttrRef(new("background", parser));
        parser.AddAttrRef(new("bgcolor", parser));
        parser.AddAttrRef(new("border", parser));
        parser.AddAttrRef(new("buffered", parser));
        parser.AddAttrRef(new("capture", parser));
        parser.AddAttrRef(new("charset", parser));
        parser.AddAttrRef(new("checked", parser));
        parser.AddAttrRef(new("cite", parser));
        parser.AddAttrRef(new("class", parser));
        parser.AddAttrRef(new("color", parser));
        parser.AddAttrRef(new("cols", parser));
        parser.AddAttrRef(new("colspan", parser));
        parser.AddAttrRef(new("content", parser));
        parser.AddAttrRef(new("contenteditable", parser));
        parser.AddAttrRef(new("contextmenu", parser));
        parser.AddAttrRef(new("controls", parser));
        parser.AddAttrRef(new("coords", parser));
        parser.AddAttrRef(new("crossorigin", parser));
        parser.AddAttrRef(new("csp", parser));
        parser.AddAttrRef(new("data", parser));
        parser.AddAttrRef(new("datetime", parser));
        parser.AddAttrRef(new("decoding", parser));
        parser.AddAttrRef(new("default", parser));
        parser.AddAttrRef(new("defer", parser));
        parser.AddAttrRef(new("dir", parser));
        parser.AddAttrRef(new("dirname", parser));
        parser.AddAttrRef(new("disabled", parser));
        parser.AddAttrRef(new("download", parser));
        parser.AddAttrRef(new("draggable", parser));
        parser.AddAttrRef(new("enctype", parser));
        parser.AddAttrRef(new("enterkeyhint", parser));
        parser.AddAttrRef(new("for", parser));
        parser.AddAttrRef(new("form", parser));
        parser.AddAttrRef(new("formaction", parser));
        parser.AddAttrRef(new("formenctype", parser));
        parser.AddAttrRef(new("formmethod", parser));
        parser.AddAttrRef(new("formnovalidate", parser));
        parser.AddAttrRef(new("formtarget", parser));
        parser.AddAttrRef(new("headers", parser));
        parser.AddAttrRef(new("height", parser));
        parser.AddAttrRef(new("hidden", parser));
        parser.AddAttrRef(new("high", parser));
        parser.AddAttrRef(new("href", parser));
        parser.AddAttrRef(new("hreflang", parser));
        parser.AddAttrRef(new("http-equiv", parser));
        parser.AddAttrRef(new("id", parser));
        parser.AddAttrRef(new("integrity", parser));
        parser.AddAttrRef(new("intrinsicsize", parser));
        parser.AddAttrRef(new("inputmode", parser));
        parser.AddAttrRef(new("ismap", parser));
        parser.AddAttrRef(new("itemprop", parser));
        parser.AddAttrRef(new("kind", parser));
        parser.AddAttrRef(new("label", parser));
        parser.AddAttrRef(new("lang", parser));
        parser.AddAttrRef(new("language", parser));
        parser.AddAttrRef(new("loading", parser));
        parser.AddAttrRef(new("list", parser));
        parser.AddAttrRef(new("loop", parser));
        parser.AddAttrRef(new("low", parser));
        parser.AddAttrRef(new("manifest", parser));
        parser.AddAttrRef(new("max", parser));
        parser.AddAttrRef(new("maxlength", parser));
        parser.AddAttrRef(new("minlength", parser));
        parser.AddAttrRef(new("media", parser));
        parser.AddAttrRef(new("method", parser));
        parser.AddAttrRef(new("min", parser));
        parser.AddAttrRef(new("multiple", parser));
        parser.AddAttrRef(new("muted", parser));
        parser.AddAttrRef(new("name", parser));
        parser.AddAttrRef(new("novalidate", parser));
        parser.AddAttrRef(new("open", parser));
        parser.AddAttrRef(new("optimum", parser));
        parser.AddAttrRef(new("pattern", parser));
        parser.AddAttrRef(new("ping", parser));
        parser.AddAttrRef(new("placeholder", parser));
        parser.AddAttrRef(new("playsinline", parser));
        parser.AddAttrRef(new("poster", parser));
        parser.AddAttrRef(new("preload", parser));
        parser.AddAttrRef(new("readonly", parser));
        parser.AddAttrRef(new("referrerpolicy", parser));
        parser.AddAttrRef(new("rel", parser));
        parser.AddAttrRef(new("required", parser));
        parser.AddAttrRef(new("reversed", parser));
        parser.AddAttrRef(new("role", parser));
        parser.AddAttrRef(new("rows", parser));
        parser.AddAttrRef(new("rowspan", parser));
        parser.AddAttrRef(new("sandbox", parser));
        parser.AddAttrRef(new("scope", parser));
        parser.AddAttrRef(new("scoped", parser));
        parser.AddAttrRef(new("selected", parser));
        parser.AddAttrRef(new("shape", parser));
        parser.AddAttrRef(new("size", parser));
        parser.AddAttrRef(new("sizes", parser));
        parser.AddAttrRef(new("slot", parser));
        parser.AddAttrRef(new("span", parser));
        parser.AddAttrRef(new("spellcheck", parser));
        parser.AddAttrRef(new("src", parser));
        parser.AddAttrRef(new("srcdoc", parser));
        parser.AddAttrRef(new("srclang", parser));
        parser.AddAttrRef(new("srcset", parser));
        parser.AddAttrRef(new("start", parser));
        parser.AddAttrRef(new("step", parser));
        parser.AddAttrRef(new("style", parser));
        parser.AddAttrRef(new("summary", parser));
        parser.AddAttrRef(new("tabindex", parser));
        parser.AddAttrRef(new("target", parser));
        parser.AddAttrRef(new("title", parser));
        parser.AddAttrRef(new("translate", parser));
        parser.AddAttrRef(new("type", parser));
        parser.AddAttrRef(new("usemap", parser));
        parser.AddAttrRef(new("value", parser));
        parser.AddAttrRef(new("width", parser));
        parser.AddAttrRef(new("wrap", parser));
    }
}