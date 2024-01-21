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
        Assert.AreEqual(40, Marshal.SizeOf(typeof(Token)));
        Assert.AreEqual(1, Marshal.SizeOf(typeof(XmlLexer)));
        Assert.AreEqual(1, Marshal.SizeOf(typeof(HtmlLexer)));

        Assert.AreEqual(24, Marshal.SizeOf(typeof(Element.Enumerator)));
        Assert.AreEqual(24, Marshal.SizeOf(typeof(Attr.Enumerator)));
    }
}
