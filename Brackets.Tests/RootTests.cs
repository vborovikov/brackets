namespace Brackets.Tests;

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RootTests
{
    private const string XmlDocument =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <root>
            <child1>Text1</child1>
            <child2 attr="value">Text2</child2>
            <child3>
                <grandchild>Text3</grandchild>
            </child3>
            <!-- Comment -->
            <child4/>
        </root>
        """;

    private const string HtmlDocument =
        """
        <!DOCTYPE html>
        <html>
        <head>
            <title>Title</title>
        </head>
        <body>
            <h1>Heading</h1>
            <p>Paragraph</p>
            <div class="class1">Div</div>
            <!-- Comment -->
            <br/>
        </body>
        </html>
        """;

    [TestMethod]
    public void Length_XmlDocument_ReturnsCorrectLength()
    {
        var document = Document.Xml.Parse(XmlDocument);
        Assert.AreEqual(XmlDocument.Length, document.Length);
    }

    [TestMethod]
    public void Length_HtmlDocument_ReturnsCorrectLength()
    {
        var document = Document.Html.Parse(HtmlDocument);
        Assert.AreEqual(HtmlDocument.Length, document.Length);
    }

    [TestMethod]
    public void Length_EmptyDocument_ReturnsZero()
    {
        var document = Document.Html.Parse(string.Empty);
        Assert.AreEqual(0, document.Length);
    }

    [TestMethod]
    public void GetEnumerator_XmlDocument_IteratesAllElements()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var parent = document.FirstOrDefault<ParentTag>(t => t.Name == "root");
        Assert.IsNotNull(parent);

        var elements = new List<Element>();
        foreach (var element in parent)
        {
            elements.Add(element);
        }
        Assert.AreEqual(5, elements.Count);
    }

    [TestMethod]
    public void GetEnumerator_HtmlDocument_IteratesAllElements()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var parent = document.FirstOrDefault<ParentTag>(t => t.Name == "html");
        Assert.IsNotNull(parent);

        var elements = new List<Element>();
        foreach (var element in parent)
        {
            elements.Add(element);
        }
        Assert.AreEqual(2, elements.Count);
    }

    [TestMethod]
    public void GetEnumerator_EmptyDocument_IteratesNoElements()
    {
        var document = Document.Html.Parse(string.Empty);
        var elements = new List<Element>();
        foreach (var element in document)
        {
            elements.Add(element);
        }
        Assert.AreEqual(0, elements.Count);
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_IteratesDirectChildren()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(2, children.Count);
        Assert.IsTrue(children.All(c => c.Parent == document.Root));
    }

    [TestMethod]
    public void EnumerateChildren_HtmlDocument_IteratesDirectChildren()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(2, children.Count);
        Assert.IsTrue(children.All(c => c.Parent == document.Root));
    }

    [TestMethod]
    public void EnumerateChildren_EmptyDocument_IteratesNoChildren()
    {
        var document = Document.Html.Parse(string.Empty);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(0, children.Count);
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_PredicateMatch()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var root = document.FirstOrDefault<ParentTag>(t => t.Name == "root");
        Assert.IsNotNull(root);

        var children = new List<Element>();
        var enumerator = root.EnumerateChildren(e => e is Tag { Name: "child2" });
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(1, children.Count);
        Assert.AreEqual("child2", (children[0] as Tag).Name);
    }

    [TestMethod]
    public void EnumerateChildren_HtmlDocument_PredicateMatch()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var root = document.FirstOrDefault<ParentTag>(t => t.Name == "html");
        Assert.IsNotNull(root);

        var children = new List<Element>();
        var enumerator = root.EnumerateChildren(e => e is Tag { Name: "body" });
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(1, children.Count);
        Assert.AreEqual("body", (children[0] as Tag).Name);
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_PredicateNoMatch()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren(e => e is Tag { Name: "nonexistent" });
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(0, children.Count);
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_TypeMatch()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var root = document.FirstOrDefault<ParentTag>(t => t.Name == "root");
        Assert.IsNotNull(root);

        var children = new List<Element>();
        var enumerator = root.EnumerateChildren<Tag>();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(4, children.Count);
        Assert.IsTrue(children.All(c => c is Tag));
    }

    [TestMethod]
    public void EnumerateChildren_HtmlDocument_TypeMatch()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren<Tag>();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(2, children.Count);
        Assert.IsTrue(children.All(c => c is Tag));
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_TypeNoMatch()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren<Comment>();
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(0, children.Count);
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_TypeAndPredicateMatch()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var root = document.FirstOrDefault<ParentTag>(t => t.Name == "root");
        Assert.IsNotNull(root);

        var children = new List<Tag>();
        var enumerator = root.EnumerateChildren<Tag>(e => e.Name == "child2");
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(1, children.Count);
        Assert.AreEqual("child2", children[0].Name);
    }

    [TestMethod]
    public void EnumerateChildren_HtmlDocument_TypeAndPredicateMatch()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var parent = document.FirstOrDefault<ParentTag>(t => t.Name == "html");
        Assert.IsNotNull(parent);

        var children = new List<Tag>();
        var enumerator = parent.EnumerateChildren<Tag>(e => e.Name == "body");
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(1, children.Count);
        Assert.AreEqual("body", children[0].Name);
    }

    [TestMethod]
    public void EnumerateChildren_XmlDocument_TypeAndPredicateNoMatch()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var children = new List<Element>();
        var enumerator = document.EnumerateChildren<Tag>(e => e.Name == "nonexistent");
        while (enumerator.MoveNext())
        {
            children.Add(enumerator.Current);
        }
        Assert.AreEqual(0, children.Count);
    }

    [TestMethod]
    public void Find_XmlDocument_FindsMatchingElement()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var element = document.Find(e => e is Tag { Name: "child2" });
        Assert.IsNotNull(element);
        Assert.AreEqual("child2", (element as Tag)?.Name);
    }

    [TestMethod]
    public void Find_HtmlDocument_FindsMatchingElement()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var element = document.Find(e => e is Tag { Name: "body" });
        Assert.IsNotNull(element);
        Assert.AreEqual("body", (element as Tag)?.Name);
    }

    [TestMethod]
    public void Find_XmlDocument_NoMatchingElement()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var element = document.Find(e => e is Tag { Name: "nonexistent" });
        Assert.IsNull(element);
    }

    [TestMethod]
    public void Find_XmlDocument_FindsMatchingTypedElement()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var element = document.Find<Tag>(e => e is Tag { Name: "child2" });
        Assert.IsNotNull(element);
        Assert.AreEqual("child2", element.Name);
    }

    [TestMethod]
    public void Find_HtmlDocument_FindsMatchingTypedElement()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var element = document.Find<Tag>(e => e is Tag { Name: "body" });
        Assert.IsNotNull(element);
        Assert.AreEqual("body", element.Name);
    }

    [TestMethod]
    public void Find_XmlDocument_NoMatchingTypedElement()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var element = document.Find<Comment>(e => e.Data is "nonexistent");
        Assert.IsNull(element);
    }

    [TestMethod]
    public void FindAll_XmlDocument_FindsMatchingElements()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll(e => e is Tag t && t.Name.StartsWith("child"));
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(4, elements.Count);
    }

    [TestMethod]
    public void FindAll_HtmlDocument_FindsMatchingElements()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll(e => e is Tag t && t.Name.Length > 3);
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(5, elements.Count);
    }

    [TestMethod]
    public void FindAll_XmlDocument_NoMatchingElements()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll(e => e is Tag t && t.Name == "nonexistent");
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(0, elements.Count);
    }

    [TestMethod]
    public void FindAll_XmlDocument_FindsMatchingTypedElements()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll<Tag>(e => e.Name.StartsWith("child"));
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(4, elements.Count);
        Assert.IsTrue(elements.All(e => e is Tag));
    }

    [TestMethod]
    public void FindAll_HtmlDocument_FindsMatchingTypedElements()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll<Tag>(e => e.Name.Length > 3);
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(5, elements.Count);
        Assert.IsTrue(elements.All(e => e is Tag));
    }

    [TestMethod]
    public void FindAll_XmlDocument_NoMatchingTypedElements()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll<Comment>(e => e.Data == "nonexistent");
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(0, elements.Count);
    }

    [TestMethod]
    public void FindAll_XmlDocument_FindsAllTypedElements()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll<Tag>();
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(7, elements.Count);
        Assert.IsTrue(elements.All(e => e is Tag));
    }

    [TestMethod]
    public void FindAll_HtmlDocument_FindsAllTypedElements()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var elements = new List<Element>();
        var enumerator = document.FindAll<Tag>();
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(9, elements.Count);
        Assert.IsTrue(elements.All(e => e is Tag));
    }

    [TestMethod]
    public void FindAll_EmptyDocument_FindsNoTypedElements()
    {
        var document = Document.Html.Parse(string.Empty);
        var elements = new List<Element>();
        var enumerator = document.FindAll<Tag>();
        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }
        Assert.AreEqual(0, elements.Count);
    }

    [TestMethod]
    public void ToString_XmlDocument_ReturnsFormattedString()
    {
        var document = Document.Xml.Parse(XmlDocument);
        var formattedString = document.ToString("X");
        Assert.IsNotNull(formattedString);
        Assert.IsTrue(formattedString.Contains("<root>"));
        Assert.IsTrue(formattedString.Contains("<child1>Text1</child1>"));
    }

    [TestMethod]
    public void ToString_HtmlDocument_ReturnsFormattedString()
    {
        var document = Document.Html.Parse(HtmlDocument);
        var formattedString = document.ToString("H");
        Assert.IsNotNull(formattedString);
        Assert.IsTrue(formattedString.Contains("<html>"));
        Assert.IsTrue(formattedString.Contains("<title>Title</title>"));
    }

    [TestMethod]
    public void ToString_EmptyDocument_ReturnsEmptyString()
    {
        var document = Document.Html.Parse(string.Empty);
        var formattedString = document.ToString();
        Assert.AreEqual("", formattedString);
    }
}
