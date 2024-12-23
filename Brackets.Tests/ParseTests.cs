namespace Brackets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ParseTests
{
    [TestMethod]
    public void Parse_InvalidAttrStrayTag_Parsed()
    {
        var document = Document.Html.Parse(
            """
            <figure class="intro-image intro-left">
                <img src="https://cdn.example.com/wp-content/uploads/2024/04/dangerous_ai_hero-800x450.jpg" alt="A modified photo of a 1956 scientist carefully bottling " ai with robotic arms from behind a protective wall.>
                <figcaption class="caption">
                    <div class="caption-text">
                        <a href="https://cdn.example.com/wp-content/uploads/2024/04/dangerous_ai_hero.jpg" class="enlarge-link" data-height="675" data-width="1200">Enlarge</a>
                    </div>
                    <div class="caption-credit">
                        <a rel="nofollow" class="caption-link" href="https://www.example.com/detail/news-photo/scientist-carefully-bottles-isotopes-with-robotic-arms-from-news-photo/514912704">Benj Edwards | Getty Images</a>
                    </div>
                </figcaption>
            </figure>
            """);

        var figure = document.First<ParentTag>();
        var img = figure.FirstOrDefault<Tag>();
        var figcaption = figure.FirstOrDefault<ParentTag>();
        Assert.AreEqual(2, figure.Count());
        Assert.IsNotNull(img);
        Assert.IsNotNull(figcaption);
    }

    [TestMethod]
    public void HtmlParse_BadInstruction_Content()
    {
        var document = Document.Html.Parse("<a><?></a>");
        var a = document.First<ParentTag>();
        var badInst = a.First();
        Assert.IsInstanceOfType<Content>(badInst);
    }

    [TestMethod]
    public void XmlParse_BadInstruction_Content()
    {
        var document = Document.Xml.Parse("<a><?></a>");
        var a = document.First<ParentTag>();
        var badInst = a.First();
        Assert.IsInstanceOfType<Content>(badInst);
    }

    [TestMethod]
    public void HtmlParse_PreNewLines_FormattingPreserved()
    {
        var document = Document.Html.Parse(
            """
            <pre class="language-csharp"><code class="language-csharp"><span class="token keyword">public</span> <span class="token keyword">class</span> <span class="token class-name">Person</span><span class="token punctuation">(</span><span class="token class-name"><span class="token keyword">string</span></span> firstName<span class="token punctuation">,</span> <span class="token class-name"><span class="token keyword">string</span></span> lastName<span class="token punctuation">)</span>
            <span class="token punctuation">{</span>
                <span class="token keyword">private</span> <span class="token keyword">readonly</span> <span class="token class-name"><span class="token keyword">string</span></span> _firstName <span class="token operator">=</span> firstName<span class="token punctuation">;</span>
                <span class="token keyword">private</span> <span class="token keyword">readonly</span> <span class="token class-name"><span class="token keyword">string</span></span> _lastName <span class="token operator">=</span> lastName<span class="token punctuation">;</span>
            <span class="token punctuation">}</span></code></pre>
            """);

        var code = document.Find<ParentTag>(t => t is { Name: "code" });

        Assert.AreEqual(
            """
            public class Person(string firstName, string lastName)
            {
                private readonly string _firstName = firstName;
                private readonly string _lastName = lastName;
            }
            """, code.ToString());

    }

    [TestMethod]
    public void ChangeName_DivToSection_Renamed()
    {
        var document = Document.Html.Parse("<div><p>test</p></div>");
        var root = document.First<ParentTag>();

        Assert.AreEqual("div", root.Name);
        Document.Html.ChangeName(root, "section");
        Assert.AreEqual("section", root.Name);
    }

    [DataTestMethod]
    [DataRow(""), DataRow(" ")]
    public void ChangeName_WrongName_Throws(string wrongName)
    {
        var document = Document.Html.Parse("<div><p>test</p></div>");
        var root = document.First<ParentTag>();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Document.Html.ChangeName(root, wrongName));
    }

    [TestMethod]
    public void ChangeName_WrongTag_Throws()
    {
        var document = Document.Xml.Parse("<div><p>test</p></div>");
        var root = document.First<ParentTag>();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Document.Html.ChangeName(root, "section"));
    }
}
