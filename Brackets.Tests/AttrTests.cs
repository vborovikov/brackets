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
        var div = document.FirstOrDefault() as Tag;
        Assert.IsNotNull(div);

        Assert.IsTrue(div.Attributes.Has("class", "font-style-italic"));
        Assert.IsFalse(div.Attributes.Has("id", "div-id-name"));
    }
}
