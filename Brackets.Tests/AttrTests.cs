namespace Brackets.Tests;

using System;

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

    [TestMethod]
    public void EnumerateAttributes_RemoveAndAddOnEachIteration_AttributesEnumerated()
    {
        var document = Document.Html.Parse("<div data-name1='value1' data-name2='value2' data-name3='value3'></div>");
        var newTag = (ParentTag)Document.Html.CreateTag("div");
        var element = document.First() as ParentTag;
        foreach (var attribute in element.EnumerateAttributes())
        {
            element.RemoveAttribute(attribute);
            newTag.AddAttribute(attribute);
        }
        Assert.AreEqual(0, element.Attributes.Count());
    }
}
