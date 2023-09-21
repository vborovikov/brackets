namespace Brackets.Tests.Markup
{
    using System;
    using Primitives;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Text.RegularExpressions;

    [TestClass]
    public class TagsTests
    {
        [TestMethod]
        public void SimpleMarkup_Parse_Tags()
        {
            var tags = Tags.Parse("foo<tag>bar</tag>", Document.Html.Syntax);
            AssertTokens(tags, "foo", "<tag>", "bar", "</tag>");
        }

        [TestMethod]
        public void EmptyTag_Parse_Content()
        {
            var tags = Tags.Parse("<tag><></tag>", Document.Html.Syntax);
            AssertTokens(tags, "<tag>", "<>", "</tag>");
        }

        [TestMethod]
        public void UnpairedTag_Parse_Content()
        {
            var tags = Tags.Parse("<tag><img /></tag>", Document.Html.Syntax);
            AssertTokens(tags, "<tag>", "<img />", "</tag>");
            AssertCategories(tags, TagCategory.Opening, TagCategory.Unpaired, TagCategory.Closing);
        }

        [TestMethod]
        public void IncorrectClosingTag_Parse_Content()
        {
            var tags = Tags.Parse("<tag></ta<g/>", Document.Html.Syntax);
            AssertTokens(tags, "<tag>", "</ta", "<g/>");
            AssertCategories(tags, TagCategory.Opening, TagCategory.Content, TagCategory.Unpaired);
        }

        [TestMethod]
        public void ExclamationMarkTagName_Parse_AsTag()
        {
            var tags = Tags.Parse("<!doctype>", Document.Html.Syntax);
            AssertTokens(tags, "<!doctype>");
            AssertCategories(tags, TagCategory.Opening);
        }

        [TestMethod]
        public void Tags_CommentNoSpace_ParsedAsComment()
        {
            var tags = Tags.Parse("<!--TERMS OF SERVICE-->", Document.Html.Syntax);
            AssertCategories(tags, TagCategory.Comment);
            AssertTokens(tags, "<!--TERMS OF SERVICE-->");
        }

        [TestMethod]
        public void Tags_CommentedSelfClosingTag_ParsedAsComment()
        {
            var tags = Tags.Parse("<!-- <tag /> -->", Document.Html.Syntax);
            AssertCategories(tags, TagCategory.Comment);
            AssertTokens(tags, "<!-- <tag /> -->");
        }

        [TestMethod]
        public void Tags_CommentedParentTags_ParsedAsComment()
        {
            var tags = Tags.Parse("<!-- <tag>abc</tag> -->", Document.Html.Syntax);
            AssertCategories(tags, TagCategory.Comment);
            AssertTokens(tags, "<!-- <tag>abc</tag> -->");
        }

        [TestMethod]
        public void Tags_CommentedTags_ParsedAsComment()
        {
            var tags = Tags.Parse(
                """
                <!--[if lt IE 7]>

                <style type="text/css">

                img, div, a, input { behavior: url(/assets/iepngfix/iepngfix.htc) }

                </style>

                <script type="text/javascript" src="/assets/iepngfix/iepngfix_tilebg.js"></script>

                <![endif]-->
                """, Document.Html.Syntax);
            AssertCategories(tags, TagCategory.Comment);
        }

        [TestMethod]
        public void Tags_SectionContentHtml_ParsedAsSection()
        {
            var tags = Tags.Parse("<ms><![CDATA[x<y]]></ms>", Document.Html.Syntax);
            AssertCategories(tags, TagCategory.Opening, TagCategory.Section, TagCategory.Closing);
            AssertTokens(tags, "<ms>", "<![CDATA[x<y]]>", "</ms>");
        }

        [TestMethod]
        public void Tags_SectionContentXml_ParsedAsSection()
        {
            var tags = Tags.Parse("<ms><![CDATA[x<y]]></ms>", Document.Xml.Syntax);
            AssertCategories(tags, TagCategory.Opening, TagCategory.Section, TagCategory.Closing);
            AssertTokens(tags, "<ms>", "<![CDATA[x<y]]>", "</ms>");
        }

        [TestMethod]
        public void Tags_UnclosedTag_HasOnlyContent()
        {
            var tags = Tags.Parse(
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
                """, Document.Xml.Syntax);

            AssertCategories(tags,
                TagCategory.Opening, // <?xml version="1.0" encoding="utf-8"?>
                TagCategory.Opening,  // <rss>
                TagCategory.Opening,  // <channel>
                TagCategory.Opening,  // <title>
                TagCategory.Content,  // The Joy of Tech
                TagCategory.Closing,  // </title>
                TagCategory.Opening,  // <link>
                TagCategory.Content,  // http://www.geekculture.com/joyoftech/index.html
                //TagCategory.Closing,  // </link>
                TagCategory.Opening,  // <copyright>
                TagCategory.Content,  // Copyright 2023
                TagCategory.Closing,  // </copyright>
                TagCategory.Opening,  // <lastBuildDate>
                TagCategory.Content,  // Mon, 18 Sept 2023 00:00:01 EST
                TagCategory.Closing,  // </lastBuildDate>
                TagCategory.Opening,  // <generator>
                TagCategory.Content,  // manual
                TagCategory.Closing,  // </generator>
                TagCategory.Opening,  // <docs>
                TagCategory.Content,  // http://blogs.law.harvard.edu/tech/rss
                TagCategory.Content,  // </x.html
                TagCategory.Closing,  // </link>
                TagCategory.Opening,  // <description>
                TagCategory.Closing,  // </description>
                TagCategory.Content,  // docs>
                TagCategory.Closing,  // </channel>
                TagCategory.Closing   // </rss>
                );
        }

        private static void AssertCategories(Tags.TagEnumerator tags, params TagCategory[] categories)
        {
            tags.Reset();
            foreach (var category in categories)
            {
                do { Assert.IsTrue(tags.MoveNext()); }
                while (tags.Current.IsEmpty);

                Assert.AreEqual(category, tags.Current.Category,
                    $"Wrong category '{tags.Current.Category}' for token '{Regex.Escape(tags.Current.Span.ToString())}', expected '{category}'.");
            }
            Assert.IsFalse(tags.MoveNext(), "More tokens left.");
        }

        private static void AssertTokens(Tags.TagEnumerator tags, params string[] tokens)
        {
            tags.Reset();
            foreach (var token in tokens)
            {
                do { Assert.IsTrue(tags.MoveNext()); }
                while (tags.Current.IsEmpty);

                Assert.IsTrue(tags.Current.Span.Equals(token, StringComparison.OrdinalIgnoreCase),
                    $"Wrong token '{Regex.Escape(tags.Current.Span.ToString())}', expected '{token}'.");
            }
            Assert.IsFalse(tags.MoveNext(), "More tokens left.");
        }
    }
}
