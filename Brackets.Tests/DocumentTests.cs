namespace Brackets.Tests.Markup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Tools;

    [TestClass]
    public class DocumentTests
    {
        [TestMethod]
        public void CreateAttribute_SimpleAttribute_Created()
        {
            var attr = Document.Html.CreateAttribute("name", "value");
            Assert.IsNotNull(attr);
            Assert.AreEqual("name", attr.Name);
            Assert.IsTrue(attr.HasValue);
            Assert.AreEqual("value", attr.Value.ToString());
        }

        [TestMethod]
        public void CreateTag_DivTag_CreatedParentTag()
        {
            var tag = Document.Html.CreateTag("div");
            Assert.IsNotNull(tag);
            Assert.IsInstanceOfType(tag, typeof(ParentTag));
            Assert.AreEqual("div", tag.Name);
        }

        [DataTestMethod]
        [DataRow("benq.html")]
        [DataRow("broken.xml")]
        [DataRow("dotnet8perf.html")]
        [DataRow("google.html")]
        [DataRow("head_script.html")]
        [DataRow("japantimes.html")]
        [DataRow("newsde.html")]
        public async Task ParseAsync_AsyncSyncHtmlParsing_Equal(string fileName)
        {
            using var stream = Samples.GetStream(fileName);
            var asyncDoc = await Document.Html.ParseAsync(stream, default);
            var asyncElements = asyncDoc.FindAll(_ => true).GetEnumerator();

            var sample = Samples.GetString(fileName);
            var syncDoc = Document.Html.Parse(sample);
            var syncElements = syncDoc.FindAll(_ => true).GetEnumerator();

            while (syncElements.MoveNext() && asyncElements.MoveNext())
            {
                var elemStr = $"{asyncElements.Current.GetType().Name}: {asyncElements.Current.ToDebugString()}";

                Assert.IsInstanceOfType(asyncElements.Current, syncElements.Current.GetType(), elemStr);
                Assert.AreEqual(syncElements.Current.Offset, asyncElements.Current.Offset, elemStr);
                Assert.AreEqual(syncElements.Current.Length, asyncElements.Current.Length, elemStr);
                if (asyncElements.Current is not Comment && syncElements.Current is not Comment)
                {
                    Assert.AreEqual(syncElements.Current.ToDebugString(), asyncElements.Current.ToDebugString(), elemStr);
                }
            }

            Assert.IsFalse(asyncElements.MoveNext());
            Assert.IsFalse(syncElements.MoveNext());
        }

        [DataTestMethod]
        [DataRow("broken.xml")]
        public async Task ParseAsync_AsyncSyncXmlParsing_Equal(string fileName)
        {
            using var stream = Samples.GetStream(fileName);
            var asyncDoc = await Document.Xml.ParseAsync(stream, default);
            var asyncElements = asyncDoc.FindAll(_ => true).GetEnumerator();

            var sample = Samples.GetString(fileName);
            var syncDoc = Document.Xml.Parse(sample);
            var syncElements = syncDoc.FindAll(_ => true).GetEnumerator();

            while (syncElements.MoveNext() && asyncElements.MoveNext())
            {
                var elemStr = $"{asyncElements.Current.GetType().Name}: {asyncElements.Current.ToDebugString()}";

                Assert.IsInstanceOfType(asyncElements.Current, syncElements.Current.GetType(), elemStr);
                Assert.AreEqual(syncElements.Current.Offset, asyncElements.Current.Offset, elemStr);
                Assert.AreEqual(syncElements.Current.Length, asyncElements.Current.Length, elemStr);
                if (asyncElements.Current is not Comment && syncElements.Current is not Comment)
                {
                    Assert.AreEqual(syncElements.Current.ToDebugString(), asyncElements.Current.ToDebugString(), elemStr);
                }
            }

            Assert.IsFalse(asyncElements.MoveNext());
            Assert.IsFalse(syncElements.MoveNext());
        }

        [TestMethod]
        public async Task ParseAsync_ParallelParsing_ThreadSafe()
        {
            var fileNames = new[] { "benq.html", "broken.xml", "dotnet8perf.html", "google.html", "head_script.html", "japantimes.html", "newsde.html" };

            await Parallel.ForEachAsync(fileNames, async (fileName, cancellationToken) =>
            {
                await using var stream = Samples.GetStream(fileName);
                var document = await Document.Xml.ParseAsync(stream, cancellationToken);
                Assert.IsNotNull(document);
                Assert.IsNotNull(document.FirstOrDefault());
            });
        }

        [TestMethod]
        public async Task ParseAsync_LargeFile_Parsed()
        {
            using var stream = Samples.GetStream("newsde.html");
            var document = await Document.Html.ParseAsync(stream, default);
            Assert.IsNotNull(document);
            Assert.AreEqual(6, document.Count());
            var links = document.Find("/html/head/link[@rel='alternate']");
            Assert.AreEqual(2, links.Count());
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
            var sample = Samples.GetString("head_script.html");
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
            var sample = Samples.GetString("broken.xml");
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

            Assert.IsTrue(document.IsComplete);
            Assert.IsTrue(document.IsWellFormed);

            AssertTags(document, "xml", "rss");
            AssertTags(document.Find<ParentTag>(t => t.Name == "channel"),
                "title", "link", "copyright", "lastBuildDate", "generator", "docs", "description");

            var linkTag = document.Find<ParentTag>(t => t.Name == "link");
            Assert.IsNotNull(linkTag);
            Assert.AreEqual(1, linkTag.Count());
        }

        private static void AssertTags(IEnumerable<Element> parent, params string[] tags)
        {
            using var elements = parent.GetEnumerator();
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
