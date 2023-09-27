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
            var tags = Lexer.TokenizeElements("foo<tag>bar</tag>", Document.Html.Syntax);
            AssertTokens(tags, "foo", "<tag>", "bar", "</tag>");
        }

        [TestMethod]
        public void EmptyTag_Parse_Content()
        {
            var tags = Lexer.TokenizeElements("<tag><></tag>", Document.Html.Syntax);
            AssertTokens(tags, "<tag>", "<>", "</tag>");
        }

        [TestMethod]
        public void UnpairedTag_Parse_Content()
        {
            var tags = Lexer.TokenizeElements("<tag><img /></tag>", Document.Html.Syntax);
            AssertTokens(tags, "<tag>", "<img />", "</tag>");
            AssertCategories(tags, TokenCategory.OpeningTag, TokenCategory.UnpairedTag, TokenCategory.ClosingTag);
        }

        [TestMethod]
        public void IncorrectClosingTag_Parse_Content()
        {
            var tags = Lexer.TokenizeElements("<tag></ta<g/>", Document.Html.Syntax);
            AssertTokens(tags, "<tag>", "</ta", "<g/>");
            AssertCategories(tags, TokenCategory.OpeningTag, TokenCategory.Content, TokenCategory.UnpairedTag);
        }

        [TestMethod]
        public void ExclamationMarkTagName_Parse_AsTag()
        {
            var tags = Lexer.TokenizeElements("<!doctype>", Document.Html.Syntax);
            AssertTokens(tags, "<!doctype>");
            AssertCategories(tags, TokenCategory.OpeningTag);
        }

        [TestMethod]
        public void Tags_CommentNoSpace_ParsedAsComment()
        {
            var tags = Lexer.TokenizeElements("<!--TERMS OF SERVICE-->", Document.Html.Syntax);
            AssertCategories(tags, TokenCategory.Comment);
            AssertTokens(tags, "<!--TERMS OF SERVICE-->");
        }

        [TestMethod]
        public void Tags_CommentedSelfClosingTag_ParsedAsComment()
        {
            var tags = Lexer.TokenizeElements("<!-- <tag /> -->", Document.Html.Syntax);
            AssertCategories(tags, TokenCategory.Comment);
            AssertTokens(tags, "<!-- <tag /> -->");
        }

        [TestMethod]
        public void Tags_CommentedParentTags_ParsedAsComment()
        {
            var tags = Lexer.TokenizeElements("<!-- <tag>abc</tag> -->", Document.Html.Syntax);
            AssertCategories(tags, TokenCategory.Comment);
            AssertTokens(tags, "<!-- <tag>abc</tag> -->");
        }

        [TestMethod]
        public void Tags_CommentedTags_ParsedAsComment()
        {
            var tags = Lexer.TokenizeElements(
                """
                <!--[if lt IE 7]>

                <style type="text/css">

                img, div, a, input { behavior: url(/assets/iepngfix/iepngfix.htc) }

                </style>

                <script type="text/javascript" src="/assets/iepngfix/iepngfix_tilebg.js"></script>

                <![endif]-->
                """, Document.Html.Syntax);
            AssertCategories(tags, TokenCategory.Comment);
        }

        [TestMethod]
        public void Tags_SectionContentHtml_ParsedAsSection()
        {
            var tags = Lexer.TokenizeElements("<ms><![CDATA[x<y]]></ms>", Document.Html.Syntax);
            AssertCategories(tags, TokenCategory.OpeningTag, TokenCategory.Section, TokenCategory.ClosingTag);
            AssertTokens(tags, "<ms>", "<![CDATA[x<y]]>", "</ms>");
        }

        [TestMethod]
        public void Tags_SectionContentXml_ParsedAsSection()
        {
            var tags = Lexer.TokenizeElements("<ms><![CDATA[x<y]]></ms>", Document.Xml.Syntax);
            AssertCategories(tags, TokenCategory.OpeningTag, TokenCategory.Section, TokenCategory.ClosingTag);
            AssertTokens(tags, "<ms>", "<![CDATA[x<y]]>", "</ms>");
        }

        [TestMethod]
        public void ParseAttributes_Prolog_QuestionMarkIgnored()
        {
            var tags = Lexer.TokenizeElements("""<?xml version="1.0" encoding="utf-8"?>""", Document.Xml.Syntax);
            Assert.IsTrue(tags.MoveNext());

            var prolog = tags.Current;
            var attrs = Lexer.TokenizeAttributes(prolog, Document.Xml.Syntax);
            
            Assert.IsTrue(attrs.MoveNext()); // version="1.0"
            Assert.IsTrue(attrs.MoveNext()); // encoding="utf-8"
            Assert.AreEqual("\"utf-8\"", attrs.Current.Data.ToString());
        }

        [TestMethod]
        public void Tags_UnclosedTag_HasOnlyContent()
        {
            var tags = Lexer.TokenizeElements(
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
                TokenCategory.UnpairedTag,  // <?xml version="1.0" encoding="utf-8"?>
                TokenCategory.OpeningTag,   // <rss>
                TokenCategory.OpeningTag,   // <channel>
                TokenCategory.OpeningTag,   // <title>
                TokenCategory.Content,      // The Joy of Tech
                TokenCategory.ClosingTag,   // </title>
                TokenCategory.OpeningTag,   // <link>
                TokenCategory.Content,      // http://www.geekculture.com/joyoftech/index.html
                TokenCategory.OpeningTag,   // <copyright>
                TokenCategory.Content,      // Copyright 2023
                TokenCategory.ClosingTag,   // </copyright>
                TokenCategory.OpeningTag,   // <lastBuildDate>
                TokenCategory.Content,      // Mon, 18 Sept 2023 00:00:01 EST
                TokenCategory.ClosingTag,   // </lastBuildDate>
                TokenCategory.OpeningTag,   // <generator>
                TokenCategory.Content,      // manual
                TokenCategory.ClosingTag,   // </generator>
                TokenCategory.OpeningTag,   // <docs>
                TokenCategory.Content,      // http://blogs.law.harvard.edu/tech/rss
                TokenCategory.Content,      // </x.html
                TokenCategory.ClosingTag,   // </link>
                TokenCategory.OpeningTag,   // <description>
                TokenCategory.ClosingTag,   // </description>
                TokenCategory.Content,      // docs>
                TokenCategory.ClosingTag,   // </channel>
                TokenCategory.ClosingTag    // </rss>
                );
        }

        private static void AssertCategories<TLexer>(Lexer.ElementTokenEnumerator<TLexer> tags, params TokenCategory[] categories)
            where TLexer : struct, IMarkupLexer
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

        private static void AssertTokens<TLexer>(Lexer.ElementTokenEnumerator<TLexer> tags, params string[] tokens)
            where TLexer : struct, IMarkupLexer
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
