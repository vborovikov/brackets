namespace CookTests.Markup.XPath
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Brackets;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XPathTests
    {
        private static string[] correctTests = {
             // Expressions from http://www.w3.org/TR/xpath#location-paths
            @"child::para"                                                          ,
            @"child::*"                                                             ,
            @"child::text()"                                                        ,
            @"child::node()"                                                        ,
            @"attribute::name"                                                      ,
            @"attribute::*"                                                         ,
            @"descendant::para"                                                     ,
            @"ancestor::div"                                                        ,
            @"ancestor-or-self::div"                                                ,
            @"descendant-or-self::para"                                             ,
            @"self::para"                                                           ,
            @"child::chapter/descendant::para"                                      ,
            @"child::*/child::para"                                                 ,
            @"/"                                                                    ,
            @"/descendant::para"                                                    ,
            @"/descendant::olist/child::item"                                       ,
            @"child::para[position()=1]"                                            ,
            @"child::para[position()=last()]"                                       ,
            @"child::para[position()=last()-1]"                                     ,
            @"child::para[position()>1]"                                            ,
            @"following-sibling::chapter[position()=1]"                             ,
            @"preceding-sibling::chapter[position()=1]"                             ,
            @"/descendant::figure[position()=42]"                                   ,
            @"/child::doc/child::chapter[position()=5]/child::section[position()=2]",
            @"child::para[attribute::type=""warning""]"                             ,
            @"child::para[attribute::type='warning'][position()=5]"                 ,
            @"child::para[position()=5][attribute::type=""warning""]"               ,
            @"child::chapter[child::title='Introduction']"                          ,
            @"child::chapter[child::title]"                                         ,
            @"child::*[self::chapter or self::appendix]"                            ,
            @"child::*[self::chapter or self::appendix][position()=last()]"         ,
        };

        private static string[] errorTests = {
            @""     ,
            @"a b"  ,
            @"a["   ,
            @"]"    ,
            @"///"  ,
            @"fo("  ,
            @")"    ,
            @"a[']" ,
            @"b[""]",
            @"3e8"  ,
            @"child::*[self::chapter or self::appendix][position()=last()] child::*[self::chapter or self::appendix][position()=last()]",
        };

        [TestMethod]
        public void XPath_FindContent_Text()
        {
            var document = LoadSample("benq.html");

            var elements = document.Find("trim(//span[contains(@class, 'price-default_current-price')]/text())");

            Assert.AreEqual(1, elements.Count());
            Assert.IsTrue(elements.First().TryGetValue<double>(out var value));
            Assert.AreEqual(32580d, value);
        }

        [TestMethod]
        public void XPath_FeedLinks_Found()
        {
            var document = LoadSample("newsde.html");
            var links = document.Find("/html/head/link[@rel='alternate']");
            Assert.AreEqual(2, links.Count());
        }

        private static Document LoadSample(string fileName)
        {
            using var htmlStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brackets.Tests.Samples." + fileName);
            using var htmlReader = new StreamReader(htmlStream, Encoding.UTF8);
            return Document.Html.Parse(htmlReader.ReadToEnd());
        }
    }
}