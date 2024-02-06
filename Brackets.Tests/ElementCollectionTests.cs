namespace Brackets.Tests;

[TestClass]
public class ElementCollectionTests
{
    [TestMethod]
    public void AddAttribute_NoAttributes_AddsAttribute()
    {
        var document = Document.Html.Parse("<div></div>");
        var element = document.First() as ParentTag;

        Assert.IsFalse(element.HasAttributes);
        element.AddAttribute(Document.Html.CreateAttribute("name", "value"));
        Assert.IsTrue(element.HasAttributes);

        Assert.AreEqual("value", element.Attributes.Get("name").ToString());
    }

    [TestMethod]
    public void AddAttribute_WithSingleAttribute_TwoAttributes()
    {
        var document = Document.Html.Parse("<div name1='value1'></div>");
        var element = document.First() as ParentTag;

        Assert.AreEqual(1, element.Attributes.Count());
        Assert.AreEqual("value1", element.Attributes.Get("name1").ToString());
        element.AddAttribute(Document.Html.CreateAttribute("name2", "value2"));

        Assert.AreEqual(2, element.Attributes.Count());
        Assert.AreEqual("value2", element.Attributes.Get("name2").ToString());
    }

    [TestMethod]
    public void EnumerateElements_RemoveOnEachIteration_ElementsEnumerated()
    {
        var document = Document.Html.Parse("<div><span id='1'></span><span id='2'></span><span id='3'></span></div>");
        var element = document.First() as ParentTag;
        foreach (var item in element)
        {
            element.Remove(item);
        }
        Assert.AreEqual(0, element.Count());
    }

    [TestMethod]
    public void EnumerateAttributes_RemoveOnEachIteration_AttributesEnumerated()
    {
        var document = Document.Html.Parse("<div data-name1='value1' data-name2='value2' data-name3='value3'></div>");
        var element = document.First() as ParentTag;
        foreach (var item in element.EnumerateAttributes())
        {
            element.RemoveAttribute(item);
        }
        Assert.AreEqual(0, element.Attributes.Count());
    }
}