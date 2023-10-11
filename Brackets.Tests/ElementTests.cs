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
    public void Last_MultipleTags_ThrowsWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Last(element => element is Tag { Name: "div" }));
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

    [TestMethod]
    public void Contains_MultipleTags_ReturnsTrue()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.IsTrue(document.First().Contains("Value1"));
    }

    [TestMethod]
    public void Single_EmptyDocument_Throws()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single());
    }

    [TestMethod]
    public void Single_SingleTag_ReturnsSingleElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Single() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void Single_MultipleTags_Throws()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single());
    }

    [TestMethod]
    public void SingleOrDefault_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        Assert.IsNull(document.SingleOrDefault());
    }

    [TestMethod]
    public void SingleOrDefault_SingleTag_ReturnsSingleElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.SingleOrDefault() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void SingleOrDefault_MultipleTags_Throws()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.SingleOrDefault());
    }
}