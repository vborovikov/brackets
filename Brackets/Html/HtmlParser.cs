namespace Brackets.Html;

using static ContentCategory;
using static FlowLayout;

public class HtmlParser : MarkupParser<HtmlLexer>
{
    public HtmlParser() : this(isThreadSafe: false) { }

    protected HtmlParser(bool isThreadSafe) : base(MarkupLanguage.Html, isThreadSafe)
    {
        InitKnownRefs(this);
    }

    /// <inheritdoc/>
    public override StringComparison Comparison => HtmlLexer.Comparison;

    internal static HtmlParser CreateConcurrent() => new(isThreadSafe: true);

    internal static void InitKnownRefs(IMarkupParser parser)
    {
        //
        // tags
        //

        // structural
        parser.InitTagRef(new("a", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive, PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("article", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.InitTagRef(new("aside", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.InitTagRef(new("body", parser) { PermittedContent = Flow });
        parser.InitTagRef(new BrTagRef(parser));
        parser.InitTagRef(new("details", parser) { Category = Flow | Interactive, PermittedContent = Flow });
        parser.InitTagRef(new("div", parser) { Category = Flow, PermittedContent = Flow });
        parser.InitTagRef(new("head", parser) { PermittedContent = Metadata });
        parser.InitTagRef(new("header", parser) { Category = Flow, PermittedContent = Flow });
        parser.InitTagRef(new("hgroup", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new("h1", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new("h2", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new("h3", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new("h4", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new("h5", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new("h6", parser) { Category = Flow | Heading, PermittedContent = Phrasing });
        parser.InitTagRef(new HrTagRef(parser));
        parser.InitTagRef(new("html", parser));
        parser.InitTagRef(new("footer", parser) { Category = Flow, PermittedContent = Flow });
        parser.InitTagRef(new("main", parser) { Category = Flow, PermittedContent = Flow });
        parser.InitTagRef(new("nav", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.InitTagRef(new("p", parser) { Category = Flow, PermittedContent = Phrasing });
        parser.InitTagRef(new("section", parser) { Category = Flow | Sectioning, PermittedContent = Flow });
        parser.InitTagRef(new("span", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("summary", parser) { PermittedContent = Phrasing | Heading });

        // metadata
        parser.InitTagRef(new("base", parser) { IsParent = false, Category = Metadata });
        parser.InitTagRef(new("doctype", parser) { IsParent = false, Layout = Inline, IsProcessingInstruction = true });
        parser.InitTagRef(new("basefont", parser) { IsParent = false, Category = Metadata });
        parser.InitTagRef(new("link", parser) { IsParent = false, Category = Metadata });
        parser.InitTagRef(new("meta", parser) { IsParent = false, Category = Metadata });
        parser.InitTagRef(new("style", parser) { ParsingMode = ParsingMode.RawContent, Category = Metadata });
        parser.InitTagRef(new("title", parser) { Category = Metadata });
        parser.InitTagRef(new("xml", parser) { IsParent = false, Layout = Inline, IsProcessingInstruction = true });

        // form
        parser.InitTagRef(new("button", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Phrasing });
        parser.InitTagRef(new("datalist", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Form });
        parser.InitTagRef(new("dialog", parser) { Layout = Inline, Category = Flow | Form, PermittedContent = Flow });
        parser.InitTagRef(new("fieldset", parser) { Category = Flow | Form, PermittedContent = Flow });
        parser.InitTagRef(new("form", parser) { Layout = Inline, Category = Flow | Form, PermittedContent = Flow });
        parser.InitTagRef(new("input", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Form });
        parser.InitTagRef(new("keygen", parser) { IsParent = false, Layout = Inline, Category = Flow | Form });
        parser.InitTagRef(new("label", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Phrasing });
        parser.InitTagRef(new("legend", parser) { Layout = Inline, Category = Form, PermittedContent = Phrasing | Heading });
        parser.InitTagRef(new("meter", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Phrasing });
        parser.InitTagRef(new("optgroup", parser) { Category = Form, PermittedContent = Form });
        parser.InitTagRef(new("option", parser) { Layout = Inline, Category = Form, PermittedContent = Phrasing });
        parser.InitTagRef(new("select", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Form });
        parser.InitTagRef(new("textarea", parser) { Layout = Inline, Category = Flow | Phrasing | Interactive | Form, PermittedContent = Phrasing });

        // formatting
        parser.InitTagRef(new("abbr", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("acronym", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("address", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow });
        parser.InitTagRef(new("b", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("bdi", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("bdo", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("big", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("blockquote", parser) { Layout = Block, Category = Flow, PermittedContent = Flow });
        parser.InitTagRef(new("center", parser) { Layout = Block, Category = Flow, PermittedContent = Flow });
        parser.InitTagRef(new("cite", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("code", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("data", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("del", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("dfn", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("em", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("font", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("i", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("ins", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("kbd", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("mark", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("output", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Phrasing });
        parser.InitTagRef(new("pre", parser) { ParsingMode = ParsingMode.Formatting, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("progress", parser) { Layout = Inline, Category = Flow | Phrasing | Form, PermittedContent = Phrasing });
        parser.InitTagRef(new("q", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("rb", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("rp", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("rt", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("rtc", parser) { Layout = Inline, Category = Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("ruby", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("s", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("samp", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("small", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("strike", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("strong", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("sub", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("sup", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("tt", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("u", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("var", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("wbr", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing });

        // list
        parser.InitTagRef(new("dd", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("dir", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("dl", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("dt", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("li", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("ol", parser) { Category = Flow });
        parser.InitTagRef(new("menu", parser) { Category = Flow });
        parser.InitTagRef(new("menuitem", parser) { IsParent = false, Layout = Inline, Category = Interactive });
        parser.InitTagRef(new("ul", parser) { Category = Flow });

        // table
        parser.InitTagRef(new("caption", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("col", parser) { IsParent = false });
        parser.InitTagRef(new("colgroup", parser) { });
        parser.InitTagRef(new("table", parser) { Category = Flow });
        parser.InitTagRef(new("tbody", parser) { });
        parser.InitTagRef(new("td", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("tfoot", parser) { });
        parser.InitTagRef(new("thead", parser) { });
        parser.InitTagRef(new("th", parser) { PermittedContent = Flow });
        parser.InitTagRef(new("tr", parser) { });

        // scripting
        parser.InitTagRef(new("noscript", parser) { Layout = Inline, Category = Metadata | Flow | Phrasing | Script });
        parser.InitTagRef(new("script", parser) { ParsingMode = ParsingMode.RawContent, Layout = Inline, Category = Metadata | Flow | Phrasing | Script });
        parser.InitTagRef(new("template", parser) { Layout = Inline, Category = Metadata | Flow | Phrasing | Script });

        // embedded
        parser.InitTagRef(new("applet", parser) { });
        parser.InitTagRef(new("area", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing });
        parser.InitTagRef(new("audio", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.InitTagRef(new("canvas", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded, PermittedContent = Interactive });
        parser.InitTagRef(new("embed", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Embedded | Interactive });
        parser.InitTagRef(new("figcaption", parser) { PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("figure", parser) { Category = Flow, PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("frame", parser) { IsParent = false, Category = Embedded });
        parser.InitTagRef(new("frameset", parser) { Category = Embedded });
        parser.InitTagRef(new("iframe", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded | Interactive });
        parser.InitTagRef(new("img", parser) { IsParent = false, Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.InitTagRef(new("map", parser) { Layout = Inline, Category = Flow | Phrasing });
        parser.InitTagRef(new("math", parser) { Category = Flow | Phrasing | Embedded });
        parser.InitTagRef(new("noframes", parser) { Category = Embedded, PermittedContent = Flow });
        parser.InitTagRef(new("object", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded | Form, PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("param", parser) { IsParent = false });
        parser.InitTagRef(new("picture", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.InitTagRef(new("slot", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Flow | Phrasing });
        parser.InitTagRef(new("source", parser) { IsParent = false });
        parser.InitTagRef(new("svg", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });
        parser.InitTagRef(new("time", parser) { Layout = Inline, Category = Flow | Phrasing, PermittedContent = Phrasing });
        parser.InitTagRef(new("track", parser) { IsParent = false });
        parser.InitTagRef(new("video", parser) { Layout = Inline, Category = Flow | Phrasing | Embedded });

        //
        // attributes
        //

        parser.InitAttrRef(new("accept", parser));
        parser.InitAttrRef(new("accept-charset", parser));
        parser.InitAttrRef(new("accesskey", parser));
        parser.InitAttrRef(new("action", parser));
        parser.InitAttrRef(new("align", parser));
        parser.InitAttrRef(new("allow", parser));
        parser.InitAttrRef(new("alt", parser));
        parser.InitAttrRef(new("async", parser));
        parser.InitAttrRef(new("autocapitalize", parser));
        parser.InitAttrRef(new("autocomplete", parser));
        parser.InitAttrRef(new("autoplay", parser));
        parser.InitAttrRef(new("background", parser));
        parser.InitAttrRef(new("bgcolor", parser));
        parser.InitAttrRef(new("border", parser));
        parser.InitAttrRef(new("buffered", parser));
        parser.InitAttrRef(new("capture", parser));
        parser.InitAttrRef(new("charset", parser));
        parser.InitAttrRef(new("checked", parser));
        parser.InitAttrRef(new("cite", parser));
        parser.InitAttrRef(new("class", parser));
        parser.InitAttrRef(new("color", parser));
        parser.InitAttrRef(new("cols", parser));
        parser.InitAttrRef(new("colspan", parser));
        parser.InitAttrRef(new("content", parser));
        parser.InitAttrRef(new("contenteditable", parser));
        parser.InitAttrRef(new("contextmenu", parser));
        parser.InitAttrRef(new("controls", parser));
        parser.InitAttrRef(new("coords", parser));
        parser.InitAttrRef(new("crossorigin", parser));
        parser.InitAttrRef(new("csp", parser));
        parser.InitAttrRef(new("data", parser));
        parser.InitAttrRef(new("datetime", parser));
        parser.InitAttrRef(new("decoding", parser));
        parser.InitAttrRef(new("default", parser));
        parser.InitAttrRef(new("defer", parser));
        parser.InitAttrRef(new("dir", parser));
        parser.InitAttrRef(new("dirname", parser));
        parser.InitAttrRef(new("disabled", parser));
        parser.InitAttrRef(new("download", parser));
        parser.InitAttrRef(new("draggable", parser));
        parser.InitAttrRef(new("enctype", parser));
        parser.InitAttrRef(new("enterkeyhint", parser));
        parser.InitAttrRef(new("for", parser));
        parser.InitAttrRef(new("form", parser));
        parser.InitAttrRef(new("formaction", parser));
        parser.InitAttrRef(new("formenctype", parser));
        parser.InitAttrRef(new("formmethod", parser));
        parser.InitAttrRef(new("formnovalidate", parser));
        parser.InitAttrRef(new("formtarget", parser));
        parser.InitAttrRef(new("headers", parser));
        parser.InitAttrRef(new("height", parser));
        parser.InitAttrRef(new("hidden", parser));
        parser.InitAttrRef(new("high", parser));
        parser.InitAttrRef(new("href", parser));
        parser.InitAttrRef(new("hreflang", parser));
        parser.InitAttrRef(new("http-equiv", parser));
        parser.InitAttrRef(new("id", parser));
        parser.InitAttrRef(new("integrity", parser));
        parser.InitAttrRef(new("intrinsicsize", parser));
        parser.InitAttrRef(new("inputmode", parser));
        parser.InitAttrRef(new("ismap", parser));
        parser.InitAttrRef(new("itemprop", parser));
        parser.InitAttrRef(new("kind", parser));
        parser.InitAttrRef(new("label", parser));
        parser.InitAttrRef(new("lang", parser));
        parser.InitAttrRef(new("language", parser));
        parser.InitAttrRef(new("loading", parser));
        parser.InitAttrRef(new("list", parser));
        parser.InitAttrRef(new("loop", parser));
        parser.InitAttrRef(new("low", parser));
        parser.InitAttrRef(new("manifest", parser));
        parser.InitAttrRef(new("max", parser));
        parser.InitAttrRef(new("maxlength", parser));
        parser.InitAttrRef(new("minlength", parser));
        parser.InitAttrRef(new("media", parser));
        parser.InitAttrRef(new("method", parser));
        parser.InitAttrRef(new("min", parser));
        parser.InitAttrRef(new("multiple", parser));
        parser.InitAttrRef(new("muted", parser));
        parser.InitAttrRef(new("name", parser));
        parser.InitAttrRef(new("novalidate", parser));
        parser.InitAttrRef(new("open", parser));
        parser.InitAttrRef(new("optimum", parser));
        parser.InitAttrRef(new("pattern", parser));
        parser.InitAttrRef(new("ping", parser));
        parser.InitAttrRef(new("placeholder", parser));
        parser.InitAttrRef(new("playsinline", parser));
        parser.InitAttrRef(new("poster", parser));
        parser.InitAttrRef(new("preload", parser));
        parser.InitAttrRef(new("readonly", parser));
        parser.InitAttrRef(new("referrerpolicy", parser));
        parser.InitAttrRef(new("rel", parser));
        parser.InitAttrRef(new("required", parser));
        parser.InitAttrRef(new("reversed", parser));
        parser.InitAttrRef(new("role", parser));
        parser.InitAttrRef(new("rows", parser));
        parser.InitAttrRef(new("rowspan", parser));
        parser.InitAttrRef(new("sandbox", parser));
        parser.InitAttrRef(new("scope", parser));
        parser.InitAttrRef(new("scoped", parser));
        parser.InitAttrRef(new("selected", parser));
        parser.InitAttrRef(new("shape", parser));
        parser.InitAttrRef(new("size", parser));
        parser.InitAttrRef(new("sizes", parser));
        parser.InitAttrRef(new("slot", parser));
        parser.InitAttrRef(new("span", parser));
        parser.InitAttrRef(new("spellcheck", parser));
        parser.InitAttrRef(new("src", parser));
        parser.InitAttrRef(new("srcdoc", parser));
        parser.InitAttrRef(new("srclang", parser));
        parser.InitAttrRef(new("srcset", parser));
        parser.InitAttrRef(new("start", parser));
        parser.InitAttrRef(new("step", parser));
        parser.InitAttrRef(new("style", parser));
        parser.InitAttrRef(new("summary", parser));
        parser.InitAttrRef(new("tabindex", parser));
        parser.InitAttrRef(new("target", parser));
        parser.InitAttrRef(new("title", parser));
        parser.InitAttrRef(new("translate", parser));
        parser.InitAttrRef(new("type", parser));
        parser.InitAttrRef(new("usemap", parser));
        parser.InitAttrRef(new("value", parser));
        parser.InitAttrRef(new("width", parser));
        parser.InitAttrRef(new("wrap", parser));
    }
}