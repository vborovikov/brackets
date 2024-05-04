namespace Brackets.Tests.Markup
{
    using System;
    using Brackets.Tests.Tools;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HtmlTests
    {
        [TestMethod]
        public void HtmlParse_ImproperlyNestedTags_Corrected()
        {
            var document = Document.Html.Parse("<b><i>This text is bold and italic</b></i>");
            Assert.IsTrue(document.IsWellFormed);

            var b = document.First() as ParentTag;
            Assert.IsNotNull(b);
            Assert.AreEqual("b", b.Name);
            var i = b.First() as ParentTag;
            Assert.IsNotNull(i);
            Assert.AreEqual("i", i.Name);
            var innerText = i.First() as Content;
            Assert.IsNotNull(innerText);
            Assert.AreEqual("This text is bold and italic", innerText.ToString());
        }

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
            Assert.IsNotNull(document.Find<Declaration>(tag => tag.Name == "doctype"));
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
            Assert.AreEqual(5, tag.EnumerateAttributes().Count());
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

            var dataDisabledAttr = buttonTag.EnumerateAttributes().FirstOrDefault(attr => attr.Name.StartsWith("data-disabled"));
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
            Assert.IsTrue(tag.HasAttributes);
            Assert.AreEqual(1, tag.EnumerateAttributes().Count());

            var attr = tag.EnumerateAttributes().First();
            Assert.AreEqual("\"\"v1\"\"", attr.Value.ToString());
        }

        [TestMethod]
        public void Attr_SelfClosingTag_Correct()
        {
            var markup = """<tag n1="v1" n2='v2' />""";
            var doc = Document.Html.Parse(markup);
            var tag = doc.FirstOrDefault() as Tag;

            Assert.IsNotNull(tag);
            Assert.IsNull(tag as ParentTag);
            var attr = tag.EnumerateAttributes().LastOrDefault();
            Assert.IsNotNull(attr);
            Assert.AreNotEqual("/", attr.Value.ToString());

            Assert.AreEqual(2, tag.EnumerateAttributes().Count());
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
            var text = doc.First().ToString();

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

        [TestMethod]
        public void Parse_CodeTagEscapedTag_RawContent()
        {
            var document = Document.Html.Parse("<code>&lt;PackageReference/&gt;</code>");
            var code = document.First() as ParentTag;
            Assert.IsNotNull(code);
            var content = code.SingleOrDefault() as Content;
            Assert.IsNotNull(content);
            Assert.AreEqual("&lt;PackageReference/&gt;", content.ToString());
        }

        [TestMethod]
        public void Parse_CodeTagUnescapedTag_UknownTag()
        {
            var document = Document.Html.Parse("<code><PackageReference/></code>");
            var code = document.First() as ParentTag;
            Assert.IsNotNull(code);
            var content = code.SingleOrDefault();
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType<Tag>(content);
        }

        [TestMethod]
        public void Parse_CDataInScript_ParsedAsCData()
        {
            var document = Document.Html.Parse(
                """
                <script type="application/ld+json">
                    <![CDATA[
                    {"@context":"https:\/\/schema.org","@type":"Article","name":"Hermitian matrix","url":"https:\/\/en.wikipedia.org\/wiki\/Hermitian_matrix","sameAs":"http:\/\/www.wikidata.org\/entity\/Q652941","mainEntity":"http:\/\/www.wikidata.org\/entity\/Q652941","author":{"@type":"Organization","name":"Contributors to Wikimedia projects"},"publisher":{"@type":"Organization","name":"Wikimedia Foundation, Inc.","logo":{"@type":"ImageObject","url":"https:\/\/www.wikimedia.org\/static\/images\/wmf-hor-googpub.png"}},"datePublished":"2003-02-28T21:51:08Z","dateModified":"2020-02-24T20:33:46Z","headline":"matrix equal to its conjugate-transpose"}
                    ]]>
                </script>
                """);

            var script = document.FirstOrDefault() as ParentTag;
            Assert.IsNotNull(script);
            Assert.AreEqual("script", script.Name);

            var cdata = script.SingleOrDefault();
            Assert.IsNotNull(cdata);
            Assert.IsInstanceOfType(cdata, typeof(Section));
        }

        [TestMethod]
        public void ToString_NoWhiteSpace_WhiteSpaceIntroduced()
        {
            var document = Document.Html.Parse(
                """
                <p>
                    <span>by</span> <a>the author</a>
                </p>
                """);

            var para = document.First<ParentTag>();
            Assert.AreEqual("p", para.Name);

            var text = para.ToString().Trim();
            Assert.AreEqual("by the author", text);
        }

        [TestMethod]
        public void HtmlParse_IFrameWithSrcMarkup_ParsedAsAttrValue()
        {
            var document = Document.Html.Parse(
                """
                <iframe width="0" height="0" frameborder="0" id="google_ads_iframe_/4049405/728x90_1_home_hrreview_0__hidden__" name="google_ads_iframe_/4049405/728x90_1_home_hrreview_0__hidden__" scrolling="no" marginwidth="0" marginheight="0" style="border: 0px none; vertical-align: bottom; visibility: hidden; display: none;" src="javascript:&quot;<html><body style='background:transparent'></body></html>&quot;"></iframe>
                """);
            var iframe = document.First<ParentTag>();
            Assert.IsFalse(iframe.Any());
        }

        [TestMethod]
        public async Task HtmlAsyncParse_IFrameWithSrcMarkup_ParsedAsAttrValue()
        {
            using var stream = Samples.GetStream("iab.html");
            var document = await Document.Html.ParseAsync(stream, default);

            var iframe = document.Find<ParentTag>(t =>
                t.Name == "iframe" &&
                t.Attributes["id"] is "google_ads_iframe_/4049405/728x90_1_home_hrreview_0__hidden__");
            Assert.IsNotNull(iframe);

            Assert.IsFalse(iframe.Any());
        }

        [TestMethod]
        public void HtmlParse_AttrValueLtSign_ParsedAsAttrValue()
        {
            var document = Document.Html.Parse(
                """
                <p>But
                  as it was, I should have as soon thought of turning my pale <a href="javascript:void(0);"
                  onclick="return overlib('>How is Bartleby like this wise philosopher?', STICKY)"
                  onmouseout="nd();" class="popup">plaster-of-paris 
                bust of Cicero</a> out of doors.</p>
                """);

            var para = document.First<ParentTag>();
            Assert.IsNotNull(para);
            var anchor = para.First<ParentTag>();
            Assert.IsNotNull(anchor);

            Assert.AreEqual("return overlib('>How is Bartleby like this wise philosopher?', STICKY)",
                anchor.Attributes["onclick"].ToString());
        }

        [TestMethod]
        public void HtmlParse_AttrValueAngleBrackets_ParsedFullValue()
        {
            var document = Document.Html.Parse(
                """
                <figure class="full-width ">
                    <img src="https://example.com/r/w1560/getpro/habr/upload_files/311/fb6/452/311fb64527140d3c49239ee747a37ac0.png" alt="> AND >  =>  range &amp;> range(expr, expr, '[]')" title="> AND >  =>  range &amp;> range(expr, expr, '[]')" width="962" height="194" data-src="https://example.com/getpro/habr/upload_files/311/fb6/452/311fb64527140d3c49239ee747a37ac0.png">
                    <div>
                        <figcaption>
                            <code>&gt; AND &gt;  =&gt;  range &amp;&gt; range(expr, expr, '[]')</code>
                        </figcaption>
                    </div>
                </figure>
                """);

            var figure = document.First<ParentTag>();
            Assert.AreEqual(2, figure.Count<Tag>());
            var img = figure.First<Tag>();
            Assert.IsNotInstanceOfType<ParentTag>(img);

            Assert.AreEqual("> AND >  =>  range &amp;> range(expr, expr, '[]')", img.Attributes["alt"].ToString());
            Assert.AreEqual("> AND >  =>  range &amp;> range(expr, expr, '[]')", img.Attributes["title"].ToString());
        }
    }
}
