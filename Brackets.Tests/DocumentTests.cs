namespace Brackets.Tests.Markup
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Brackets.Primitives;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class DocumentTests
    {
        private static Assembly assembly;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            assembly = Assembly.GetExecutingAssembly();
        }

        private static string GetSample(string fileName)
        {
            using var fileStream = assembly.GetManifestResourceStream($"Brackets.Tests.Samples.{fileName}");
            using var fileReader = new StreamReader(fileStream, Encoding.UTF8);
            return fileReader.ReadToEnd();
        }

        [TestMethod]
        public void Document_Find_NestedContent()
        {
            var document = Document.Html.Parse("<a><b1></b1>123<b2><c1></c1><c2>@<c3/>!</c2></b2><b3/></a>");
            var element = document.Find<Content>(cn => cn.ToString() == "!");
            Assert.IsNotNull(element);
        }

        [TestMethod]
        public void ComplexScript_Build_ClosedProperly()
        {
            var sample = GetSample("head_script.html");
            var document = Document.Html.Parse(sample);

            var headBlock = document.First() as ParentTag;
            Assert.IsNotNull(headBlock);
            Assert.AreEqual("head", headBlock.Name);

            var scriptBlock = headBlock.First() as ParentTag;
            Assert.IsNotNull(scriptBlock);
            Assert.AreEqual("script", scriptBlock.Name);

            var content = scriptBlock.First() as Content;
            Assert.IsNotNull(content);

            var contentStr = content.ToString();
            Assert.IsFalse(contentStr.EndsWith("</script>"));
        }

        [TestMethod]
        public void XmlParse_BrokenTag_ClosedBeforeNextTag()
        {
            var sample = GetSample("broken.xml");
            var document = Document.Xml.Parse(sample);

            var docsTag = document.Find<ParentTag>(t => t.Name == "docs");
            Assert.IsNotNull(docsTag);
            Assert.AreEqual(0, docsTag.SkipWhile(el => el is Content).Count());
        }

        [TestMethod]
        public void XmlParse_UnclosedTags_ClosedAfterContent()
        {
            var document = Document.Xml.Parse(
                """
                <?xml version="1.0" encoding="utf-8"?>
                <rss version="2.0">
                <channel>
                <title>The Joy of Tech</title>
                <link>http://www.geekculture.com/joyoftech/index.html
                <copyright>Copyright 2023</copyright>
                <lastBuildDate>Mon, 18 Sept 2023 00:00:01 EST</lastBuildDate>
                <generator>manual</generator>
                <docs>http://blogs.law.harvard.edu/tech/rss</x.html</link>
                <description></description>docs>
                </channel>
                </rss>
                """);

            AssertTags(document, "?xml", "rss");
            AssertTags(document.Find<ParentTag>(t => t.Name == "channel"), 
                "title", "link", "copyright", "lastBuildDate", "generator", "docs", "description");

            var linkTag = document.Find<ParentTag>(t => t.Name == "link");
            Assert.IsNotNull(linkTag);
            Assert.AreEqual(1, linkTag.Count());
        }

        private static void AssertTags(IEnumerable<Element> parent, params string[] tags)
        {
            var elements = parent.GetEnumerator();
            foreach (var tag in tags)
            {
                do { Assert.IsTrue(elements.MoveNext()); }
                while (elements.Current is not Tag);

                Assert.AreEqual(tag, ((Tag)elements.Current).Name,
                    $"Wrong element '{elements.Current.ToDebugString()}', expected '{tag}'.");
            }
        }
    }
}
