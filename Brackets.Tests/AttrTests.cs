namespace Brackets.Tests;

[TestClass]
public class AttrTests
{
    [TestMethod]
    public void AttrListHas_MultipleClassNames_ReturnsTrue()
    {
        var document = Document.Html.Parse(
            """
            <div class="bg-color-lime font-style-italic font-large">
              Applying classes to a div element example
            </div>
            """);
        var div = document.First<Tag>();

        Assert.IsTrue(div.Attributes.Has("class", "font-style-italic"));
        Assert.IsFalse(div.Attributes.Has("id", "div-id-name"));
    }

    [TestMethod]
    public void AttrListGet_LeadingTrailingNewLines_Trimmed()
    {
        var document = Document.Html.Parse(
            """
            <meta name="description" content="
              Laurent Fabius a accueilli jeudi matin à Roissy un premier avion spécial ramenant des rescapés.
            "/>
            """);
        var meta = document.First<Tag>();

        Assert.AreEqual("Laurent Fabius a accueilli jeudi matin à Roissy un premier avion spécial ramenant des rescapés.",
            meta.Attributes["content"].ToString());
    }
}
