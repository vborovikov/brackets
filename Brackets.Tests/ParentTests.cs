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

    [TestMethod]
    public void FindAll_NonExistent_NotFound()
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

        var keys = document.FindAll<ParentTag>(p => p.Name == "key").ToArray();
        Assert.AreEqual(0, keys.Length);
    }

    [TestMethod]
    public void FindAll_OnlyChildren_Found()
    {
        var document = Document.Xml.Parse("<root><a/><b/><c/></root>");
        var found = document.FindAll<Tag>(t => t.Name == "b").ToArray();

        Assert.AreEqual(1, found.Length);
    }

    [TestMethod]
    public void FindAll_OnlyChildren_NotFound()
    {
        var document = Document.Xml.Parse("<root><a/><b/><c/></root>");
        var found = document.FindAll<Tag>(t => t.Name == "x").ToArray();

        Assert.AreEqual(0, found.Length);
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

    //todo:[TestMethod]
    public void FindAll_RemoveOnEachIteration_ElementsEnumerated()
    {
        // it would be nice to be able to modify the element tree
        // as we enumerating/searching the elements

        var document = Document.Html.Parse(
            """
            <div>
                <span id='1'><i>test</i></span>
                <span id='2'></span>
                <span id='3'><i>test</i></span>
            </div>
            """);
        var root = document.FirstOrDefault<ParentTag>();
        Assert.IsNotNull(root);
        Assert.AreEqual("div", root.Name);

        foreach (var item in root.FindAll<ParentTag>())
        {
            item.Parent?.Remove(item);
        }

        Assert.AreEqual(0, root.Count());
    }

    [TestMethod]
    public void IsDescendantOf_DirectChild_ReturnsTrue()
    {
        var document = Document.Html.Parse("<div><p>Hello</p></div>"); // Placeholder - replace with your parsing method

        var div = document.First<ParentTag>();
        var p = div.First<ParentTag>();

        Assert.IsTrue(p.IsDescendantOf(div));
    }

    [TestMethod]
    public void IsDescendantOf_Grandchild_ReturnsTrue()
    {
        var document = Document.Html.Parse("<div><section><p>Hello</p></section></div>");

        var div = document.First<ParentTag>();
        var p = div.Find<ParentTag>(t => t.Name == "p");

        Assert.IsNotNull(p);
        Assert.IsTrue(p.IsDescendantOf(div));
    }

    [TestMethod]
    public void IsDescendantOf_NotDescendant_ReturnsFalse()
    {
        var document = Document.Html.Parse("<div><p>Hello</p></div><span>World</span>");

        var div = document.First<ParentTag>(t => t.Name == "div");
        var span = document.First<ParentTag>(t => t.Name == "span");

        Assert.IsFalse(span.IsDescendantOf(div));
    }

    [TestMethod]
    public void IsDescendantOf_NullTag_ThrowsException()
    {
        var document = Document.Html.Parse("<p></p>");
        var p = document.First<ParentTag>();
        Assert.ThrowsException<ArgumentNullException>(() => p.IsDescendantOf(default));
    }

    [TestMethod]
    public void IsDescendantOf_Self_ReturnsFalse()
    {
        var document = Document.Html.Parse("<p></p>");
        var p = document.First<ParentTag>();
        Assert.IsFalse(p.IsDescendantOf(p));
    }

    [TestMethod]
    public void IsAncestorOf_DirectChild_ReturnsTrue()
    {
        var document = Document.Html.Parse("<div><p>Hello</p></div>");

        var div = document.First<ParentTag>();
        var p = div.First<ParentTag>();

        Assert.IsTrue(div.IsAncestorOf(p));
    }

    [TestMethod]
    public void IsAncestorOf_Grandchild_ReturnsTrue()
    {
        var document = Document.Html.Parse("<div><section><p>Hello</p></section></div>");

        var div = document.First<ParentTag>();
        var p = div.Find<ParentTag>(t => t.Name == "p");

        Assert.IsTrue(div.IsAncestorOf(p));
    }

    [TestMethod]
    public void IsAncestorOf_NotAncestor_ReturnsFalse()
    {
        var document = Document.Html.Parse("<div><p>Hello</p></div><span>World</span>");

        var div = document.First<ParentTag>(t => t.Name == "div");
        var span = document.First<ParentTag>(t => t.Name == "span");

        Assert.IsFalse(div.IsAncestorOf(span));
    }

    [TestMethod]
    public void IsAncestorOf_NullElement_ThrowsException()
    {
        var document = Document.Html.Parse("<div></div>");
        var div = document.First<ParentTag>();

        Assert.ThrowsException<ArgumentNullException>(() => div.IsAncestorOf(default));
    }

    [TestMethod]
    public void IsAncestorOf_Self_ReturnsFalse()
    {
        var document = Document.Html.Parse("<div></div>");
        var div = document.First<ParentTag>();

        Assert.IsFalse(div.IsAncestorOf(div));
    }
}