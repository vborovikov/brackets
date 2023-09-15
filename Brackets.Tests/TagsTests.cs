namespace Brackets.Tests.Markup
{
    using System;
    using Primitives;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TagsTests
    {
        [TestMethod]
        public void SimpleMarkup_Parse_Tags()
        {
            var tags = Tags.Parse("foo<tag>bar</tag>");
            AssertTokens(tags, "foo", "<tag>", "bar", "</tag>");
        }

        [TestMethod]
        public void EmptyTag_Parse_Content()
        {
            var tags = Tags.Parse("<tag><></tag>");
            AssertTokens(tags, "<tag>", "<>", "</tag>");
        }

        [TestMethod]
        public void UnpairedTag_Parse_Content()
        {
            var tags = Tags.Parse("<tag><img /></tag>");
            AssertTokens(tags, "<tag>", "<img />", "</tag>");
            AssertCategories(tags, TagCategory.Opening, TagCategory.Unpaired, TagCategory.Closing);
        }

        [TestMethod]
        public void IncorrectClosingTag_Parse_Content()
        {
            var tags = Tags.Parse("<tag></ta<g/>");
            AssertTokens(tags, "<tag>", "</ta", "<g/>");
            AssertCategories(tags, TagCategory.Opening, TagCategory.Content, TagCategory.Unpaired);
        }

        [TestMethod]
        public void ExclamationMarkTagName_Parse_AsTag()
        {
            var tags = Tags.Parse("<!doctype>");
            AssertTokens(tags, "<!doctype>");
            AssertCategories(tags, TagCategory.Opening);
        }

        [TestMethod]
        public void Tags_CommentNoSpace_ParsedAsComment()
        {
            var tags = Tags.Parse("<!--TERMS OF SERVICE-->");
            AssertCategories(tags, TagCategory.Comment);
            AssertTokens(tags, "<!--TERMS OF SERVICE-->");
        }

        [TestMethod]
        public void Tags_CommentedSelfClosingTag_ParsedAsComment()
        {
            var tags = Tags.Parse("<!-- <tag /> -->");
            AssertCategories(tags, TagCategory.Comment);
            AssertTokens(tags, "<!-- <tag /> -->");
        }

        [TestMethod]
        public void Tags_CommentedParentTags_ParsedAsComment()
        {
            var tags = Tags.Parse("<!-- <tag>abc</tag> -->");
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
                """);
            AssertCategories(tags, TagCategory.Comment);
        }

        [TestMethod]
        public void Tags_SectionContent_ParsedAsSection()
        {
            var tags = Tags.Parse("<ms><![CDATA[x<y]]></ms>");
            AssertCategories(tags, TagCategory.Opening, TagCategory.Section, TagCategory.Closing);
            AssertTokens(tags, "<ms>", "<![CDATA[x<y]]>", "</ms>");
        }

        private static void AssertCategories(Tags.TagEnumerator tags, params TagCategory[] categories)
        {
            tags.Reset();
            foreach (var category in categories)
            {
                Assert.IsTrue(tags.MoveNext() && tags.Current.Category == category,
                    $"Wrong category '{tags.Current.Category}' for token '{tags.Current.Span.ToString()}', expected '{category}'.");
            }
            Assert.IsFalse(tags.MoveNext(), "More tokens left.");
        }

        private static void AssertTokens(Tags.TagEnumerator tags, params string[] tokens)
        {
            tags.Reset();
            foreach (var token in tokens)
            {
                Assert.IsTrue(tags.MoveNext() && tags.Current.Span.Equals(token, StringComparison.OrdinalIgnoreCase),
                    $"Wrong token '{tags.Current.Span.ToString()}', expected '{token}'.");
            }
            Assert.IsFalse(tags.MoveNext(), "More tokens left.");
        }
    }
}
