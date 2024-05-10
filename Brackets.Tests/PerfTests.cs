namespace Brackets.Tests;

using System.Runtime.InteropServices;
using Brackets.Html;
using Brackets.Parsing;
using Brackets.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PerfTests
{
    [TestMethod]
    public void SizeOf_MostUsedTypes_NotLarge()
    {
        Assert.AreEqual(32, Marshal.SizeOf(typeof(Token)));
        Assert.AreEqual(1, Marshal.SizeOf(typeof(XmlLexer)));
        Assert.AreEqual(1, Marshal.SizeOf(typeof(HtmlLexer)));

        Assert.AreEqual(24, Marshal.SizeOf(typeof(Element.Enumerator)));
        Assert.AreEqual(24, Marshal.SizeOf(typeof(Attr.Enumerator)));
        Assert.AreEqual(8, Marshal.SizeOf(typeof(Attr.List)));
    }

    [TestMethod]
    public void AttrListIndexedProperty_Setter_CS1612NotRaised()
    {
        var tag = Document.Html.CreateTag("div");
        tag.Attributes["id"] = "can-set-id";
        var value = tag.Attributes["id"];
        Assert.AreEqual("can-set-id", value.ToString());
    }
}
