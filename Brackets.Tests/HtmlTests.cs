namespace Brackets.Tests.Markup
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HtmlTests
    {
        [TestMethod]
        public void EmptyHtml_Parse_AllTags()
        {
            var document = Document.Html.Parse(
                """

                <!doctype html>
                <html lang="en">
                 <head>
                  <meta charset="UTF-8">
                  <meta name="Generator" content="EditPlus®">
                  <meta name="Author" content="">
                  <meta name="Keywords" content="">
                  <meta name="Description" content="">
                  <title>Document</title>
                 </head>
                 <body>
                 </body>
                </html>

                """);
            Assert.IsNotNull(document);
            Assert.IsNotNull(document.Find<Tag>(tag => tag.Name == "!doctype"));
            Assert.IsNotNull(document.Find<ParentTag>(tag => tag.Name == "html"));
            Assert.IsNotNull(document.Find<ParentTag>(tag => tag.Name == "head"));
            var metaTags = document.FindAll<Tag>(tag => tag.Name == "meta").ToArray();
            Assert.AreEqual(5, metaTags.Length);
            CollectionAssert.AllItemsAreNotNull(metaTags);
            Assert.IsNotNull(document.Find<ParentTag>(tag => tag.Name == "title"));
            Assert.IsNotNull(document.Find<ParentTag>(tag => tag.Name == "body"));
        }

        [TestMethod]
        public void Tag_DifferentQuotations_3Pairs2Flags()
        {
            var markup = """<tag n1=v1 n2='v2' n3="v3" n4 n5></tag>""";
            var doc = Document.Html.Parse(markup);
            var tag = doc.FirstOrDefault() as Tag;

            Assert.IsNotNull(tag);
            Assert.AreEqual(5, tag.Attributes.Count);
        }

        [TestMethod]
        public void Attr_FlagWithWhiteSpace_NoWhiteSpaceInName()
        {
            var markup =
                """
                <button
                                type="submit"
                        class="js--ForgotPassword__login-button  Button  jsButton Button_theme_primary Button_size_m Button_full-width"
                        data-label="Войти"
                         data-disabled             ><span class="Button__text jsButton__text">
                                Войти
                            </span></button>
                """;

            var doc = Document.Html.Parse(markup);

            var buttonTag = doc.FirstOrDefault() as Tag;
            Assert.IsNotNull(buttonTag);

            var dataDisabledAttr = buttonTag.Attributes.FirstOrDefault(attr => attr.Name.StartsWith("data-disabled"));
            Assert.IsNotNull(dataDisabledAttr);

            Assert.AreEqual("data-disabled", dataDisabledAttr.Name);
            Assert.IsTrue(dataDisabledAttr.IsFlag);
        }

        [TestMethod]
        public void Attr_ValueSpaceQuotes_Correct()
        {
            var markup = """<tag n1 =  " ""v1"" "   >""";
            var doc = Document.Html.Parse(markup);
            var tag = doc.FirstOrDefault() as Tag;

            Assert.IsNotNull(tag);
            Assert.AreEqual(1, tag.Attributes.Count);

            var attr = tag.Attributes.First();
            Assert.AreEqual("\" \"\"v1\"\" \"", attr.Value.ToString());
        }

        [TestMethod]
        public void Attr_SelfClosingTag_Correct()
        {
            var markup = """<tag n1="v1" n2='v2' />""";
            var doc = Document.Html.Parse(markup);
            var tag = doc.FirstOrDefault() as Tag;

            Assert.IsNotNull(tag);
            Assert.IsNull(tag as ParentTag);
            var attr = tag.Attributes.LastOrDefault();
            Assert.IsNotNull(attr);
            Assert.AreNotEqual("/", attr.Value.ToString());

            Assert.AreEqual(2, tag.Attributes.Count);
        }

        [TestMethod]
        public void ParentTag_ToTextBrs_NewLines()
        {
            var markup =
                "<p class=H1>1 c. peanut butter<br/>" +
                "3/4 c. graham cracker crumbs<br/>" +
                "1 c. melted butter<br/>" +
                "1 lb. (3 1/2 c.) powdered sugar<br/>" +
                "1 large pkg. chocolate chips<br/></p>";
            var doc = Document.Html.Parse(markup);
            var text = doc.First().ToText();

            Assert.AreEqual(
                "1 c. peanut butter" + Environment.NewLine +
                "3/4 c. graham cracker crumbs" + Environment.NewLine +
                "1 c. melted butter" + Environment.NewLine +
                "1 lb. (3 1/2 c.) powdered sugar" + Environment.NewLine +
                "1 large pkg. chocolate chips" + Environment.NewLine,
                text);
        }

        [TestMethod]
        public void Parse_StyleCdataInCssComment_TwoTagsParsed()
        {
            var markup =
                """
                <style type="text/css">
                /*<![CDATA[*/
                @media print {
                  .breadcrumbs {display:none;}
                  h1 {margin-bottom:0px;}
                  .h3 {margin-top:0px;margin-bottom:20px;}
                  .page-info {margin-top:0px;margin-bottom:0px;border-top:none;padding-top:0px;}
                  .block-part {margin-bottom:0px;}
                  .table-wrapper {margin-bottom:0px;}
                  table.data td, table.data th {padding:4px;}
                }
                /*]]*/
                </style>
                <script type="text/javascript">
                </script>
                """;

            var doc = Document.Html.Parse(markup);

            Assert.AreEqual(2, doc.Count());
        }
    }
}
