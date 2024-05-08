namespace Brackets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ElementTests
{
    [TestMethod]
    public void ToString_InlineElements_SpaceBetweenPreserved()
    {
        var document = Document.Html.Parse("<div><i>a</i> <i>b</i> </div>");
        var str = document.ToString();

        Assert.AreEqual("a b", str);
    }
}