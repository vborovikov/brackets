namespace Brackets.Tests;

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class XmlTests
{
    [TestMethod]
    public void Parse_CdataSection_ParsedAsContent()
    {
        var document = Document.Xml.Parse("""<dc:creator><![CDATA[Inner<>Text]]></dc:creator>""");
        var elements = document.GetEnumerator();
        Assert.IsTrue(elements.MoveNext());
        var tag = elements.Current as ParentTag;
        Assert.IsNotNull(tag);
        Assert.AreEqual("dc:creator", tag.Name);
        Assert.AreEqual("Inner<>Text", tag.FirstOrDefault()?.ToString());
    }
}
