﻿namespace Brackets.Tests;

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

    [TestMethod]
    public void Parse_CdataSectionWithAnchorTag_ProperlyTruncated()
    {
        var document = Document.Xml.Parse(
            """
            <item>
              <title>Shake Shake Shake Your Spices</title>
              <itunes:author>John Doe</itunes:author>
              <itunes:subtitle>A short primer on table spices</itunes:subtitle>
              <itunes:summary><![CDATA[This week we talk about <a href="https://itunes/apple.com/us/book/antique-trader-salt-pepper/id429691295?mt=11">salt and pepper shakers</a>, comparing and contrasting pour rates, construction materials, and overall aesthetics. Come and join the party!]]></itunes:summary>
              <itunes:image href="http://example.com/podcasts/everything/AllAboutEverything/Episode1.jpg"/>
              <enclosure length="8727310" type="audio/x-m4a" url="http://example.com/podcasts/everything/AllAboutEverythingEpisode3.m4a"/>
              <guid>http://example.com/podcasts/archive/aae20140615.m4a</guid>
              <pubDate>Tue, 08 Mar 2016 12:00:00 GMT</pubDate>
              <itunes:duration>07:04</itunes:duration>
              <itunes:explicit>no</itunes:explicit>
            </item>
            """);

        var summary = document.Find<ParentTag>(r => r.Name == "itunes:summary");
        Assert.IsNotNull(summary);
        var cdata = summary.Find(el => el is Section);
        Assert.IsNotNull(cdata);

        Assert.AreEqual("""This week we talk about <a href="https://itunes/apple.com/us/book/antique-trader-salt-pepper/id429691295?mt=11">salt and pepper shakers</a>, comparing and contrasting pour rates, construction materials, and overall aesthetics. Come and join the party!""",
            cdata.ToString());
    }

    [TestMethod]
    public void Parse_TagAttributeValueWithEqSign_Ignored()
    {
        var document = Document.Xml.Parse("""<link rel="alternate" href="http://www.youtube.com/watch?v=AbcdDefG"/>""");

        var link = document.FirstOrDefault() as Tag;
        Assert.IsNotNull(link);

        Assert.AreEqual(2, link.EnumerateAttributes().Count());
        Assert.AreEqual("alternate", link.EnumerateAttributes().ElementAt(0).ToString());
        Assert.AreEqual("http://www.youtube.com/watch?v=AbcdDefG", link.EnumerateAttributes().ElementAt(1).ToString());
    }

    [TestMethod]
    public void Parse_TagAttributeValueWithEqSign2_Ignored()
    {
        var document = Document.Xml.Parse("""<link rel="self" href="http://www.youtube.com/feeds/videos.xml?channel_id=UCmEN5ZnsHUXIxgpLitRTmWw"/>""");

        var link = document.FirstOrDefault() as Tag;
        Assert.IsNotNull(link);

        Assert.AreEqual(2, link.EnumerateAttributes().Count());
        Assert.AreEqual("self", link.EnumerateAttributes().ElementAt(0).ToString());
        Assert.AreEqual("http://www.youtube.com/feeds/videos.xml?channel_id=UCmEN5ZnsHUXIxgpLitRTmWw", link.EnumerateAttributes().ElementAt(1).ToString());
    }

    [TestMethod]
    public void Parse_XhtmlInstructionDeclaration_Recognized()
    {
        var document = Document.Xml.Parse(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE html
                PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"
                "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
            <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
            </html>
            """);

        var first = document.First();
        Assert.IsNotNull(first);
        Assert.IsInstanceOfType(first, typeof(Instruction));
        Assert.IsInstanceOfType(first.Next, typeof(Declaration));
        Assert.IsInstanceOfType(first.Prev, typeof(ParentTag));
    }

    [TestMethod]
    public void Parse_XmlIntrlName_Recognized()
    {
        var document = Document.Xml.Parse("""<руссо-туристо облико="морале">фирштейн?</руссо-туристо>""");

        var tag = document.FirstOrDefault<ParentTag>();
        Assert.IsNotNull(tag);
        Assert.AreEqual("руссо-туристо", tag.Name);

        var attr = tag.Attributes.FirstOrDefault();
        Assert.IsNotNull(attr);
        Assert.AreEqual("облико", attr.Name);

        var content = document.Find("//руссо-туристо/text()").FirstOrDefault() as Content;
        Assert.IsNotNull(content);
        Assert.AreEqual("фирштейн?", content.Data.ToString());
    }
}
