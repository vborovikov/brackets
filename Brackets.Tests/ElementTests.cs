namespace Brackets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ElementTests
{
    [TestMethod]
    public void First_MultipleTags_ReturnsFirstElement()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.First() as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("Value1", result.First().ToString());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void First_MultipleTags_ThrowsWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.First(element => element is Tag { Name: "div" });
    }

    [TestMethod]
    public void FirstOrDefault_MultipleTags_ReturnsFirstElementSatisfyingCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.FirstOrDefault(element => element is Tag { Name: "span" }) as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("Value1", result.First().ToString());
    }

    [TestMethod]
    public void FirstOrDefault_MultipleTags_ReturnsNullWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.FirstOrDefault(element => element is Tag { Name: "div" });
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Last_MultipleTags_ReturnsLastElement()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.Last() as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("Value3", result.First().ToString());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Last_MultipleTags_ThrowsWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.Last(element => element is Tag { Name: "div" });
    }

    [TestMethod]
    public void LastOrDefault_MultipleTags_ReturnsLastElementSatisfyingCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.LastOrDefault(element => element is Tag { Name: "span" }) as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("Value3", result.First().ToString());
    }

    [TestMethod]
    public void LastOrDefault_MultipleTags_ReturnsNullWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.LastOrDefault(element => element is Tag { Name: "div" });
        Assert.IsNull(result);
    }
}