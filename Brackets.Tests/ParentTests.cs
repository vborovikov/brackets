namespace Brackets.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    [TestMethod]
    public void Replace_FirstElement_NewElementBecomesFirst()
    {
        var document = Document.Html.Parse("<span><i>Value1</i><i>Value2</i><i>Value3</i></span>");
        var parent = document.First<ParentTag>();
        var oldElement = parent.First<Tag>();
        var newElement = Document.Html.CreateTag("i");

        parent.Replace(oldElement, newElement);

        Assert.AreEqual(newElement, parent.First<Tag>());
    }

    [TestMethod]
    public void FindAll_OfType_AllEnumerated()
    {
        var document = Document.Html.Parse(
            """
            <div>
                <span><i>Value1</i><i>Value2</i><i>Value3</i></span>
                <div>
                    <span><i>Value4</i><i>Value5</i><i>Value6</i></span>
                </div>
                <span><i>Value7</i><i>Value8</i><i>Value9</i></span>
            </div>
            """);
        var parent = document.First<ParentTag>();
        var result = parent.FindAll<Content>().ToArray();
        Assert.AreEqual(9, result.Length);
    }

    [DataTestMethod]
    [DataRow("<div></div>", false)]
    [DataRow("<div><p>1</p></div>", true)]
    [DataRow("<div><p><i>1</i></p></div>", true)]
    [DataRow("<div><p></div>", true)]
    [DataRow("<div><p>1</p><p>2</p></div>", false)]
    [DataRow("<div><div>", true)]
    public void HasOneChild_ZeroOrMoreChildren_Detected(string html, bool expected)
    {
        var document = Document.Html.Parse(html);
        var parent = document.First<ParentTag>();

        Assert.AreEqual(expected, parent.HasOneChild);
    }
}