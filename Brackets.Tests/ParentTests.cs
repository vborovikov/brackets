namespace Brackets.Tests;

[TestClass]
public class ParentTests
{
    [TestMethod]
    public void Replace_NoChildren_ThrowsInvalidOperationException()
    {
        var document = Document.Html.Parse("<span></span>");
        var parent = document.First() as ParentTag;
        var oldElement = Document.Html.CreateTag("i");
        var newElement = Document.Html.CreateTag("i");

        Assert.ThrowsException<InvalidOperationException>(() => parent.Replace(oldElement, newElement));
    }

    [TestMethod]
    public void Replace_SingleChild_NewElement()
    {
        var document = Document.Html.Parse("<span>Value</span>");
        var parent = document.First() as ParentTag;
        var oldElement = parent.Single();
        var newElement = Document.Html.CreateTag("i");

        parent.Replace(oldElement, newElement);

        Assert.AreEqual(newElement, parent.Single());
    }

    [TestMethod]
    public void Replace_MultipleChildren_NewElement()
    {
        var document = Document.Html.Parse("<span><i>Value1</i><i>Value2</i><i>Value3</i></span>");
        var parent = document.First() as ParentTag;
        var oldElement = parent.Find<Tag>(t => t.Contains("Value2"));
        var newElement = Document.Html.CreateTag("i");

        parent.Replace(oldElement, newElement);

        Assert.AreEqual(newElement, parent.Find<ParentTag>(t => !t.Any()));
    }
}