namespace Brackets.Tests.Markup
{
    using System;
    using System.Linq;
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
            Assert.AreEqual(" \"\"v1\"\" ", attr.Value.ToString());
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
        public void Parse_CodeTag_RawContent()
        {
            var document = Document.Html.Parse("<code><PackageReference/></code>");
            var code = document.First() as ParentTag;
            Assert.IsNotNull(code);
            var content = code.SingleOrDefault() as Content;
            Assert.IsNotNull(content);
            Assert.AreEqual("<PackageReference/>", content.ToString());
        }

        [TestMethod]
        public void Parse_CodeTagMultipleTags_RawContent()
        {
            var document = Document.Html.Parse(
                """
                <pre><code class="language-xml"><Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFrameworks>net8.0;net7.0</TargetFrameworks>
                    <LangVersion>Preview</LangVersion>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    <ServerGarbageCollection>true</ServerGarbageCollection>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="BenchmarkDotNet" Version="0.13.8" />
                  </ItemGroup>

                </Project></code></pre>
                """);

            var pre = document.SingleOrDefault() as ParentTag;
            Assert.IsNotNull(pre);
            var code = pre.SingleOrDefault() as ParentTag;
            Assert.IsNotNull(code);
            var content = code.SingleOrDefault() as Content;
            Assert.IsNotNull(content);

            Assert.AreEqual(
                """
                <Project Sdk="Microsoft.NET.Sdk">
                
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFrameworks>net8.0;net7.0</TargetFrameworks>
                    <LangVersion>Preview</LangVersion>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    <ServerGarbageCollection>true</ServerGarbageCollection>
                  </PropertyGroup>
                
                  <ItemGroup>
                    <PackageReference Include="BenchmarkDotNet" Version="0.13.8" />
                  </ItemGroup>
                
                </Project>
                """, content.ToString());
        }

        [TestMethod]
        public void Parse_CodeTagCsharp_RawContent()
        {
            var document = Document.Html.Parse(
                """
                <pre><code class="language-C#">static void Count(ref uint sharedCounter)
                {
                    uint currentCount = sharedCounter, delta = 1;
                    if (currentCount > 0)
                    {
                        int logCount = 31 - (int)uint.LeadingZeroCount(currentCount);
                        if (logCount >= 13)
                        {
                            delta = 1u << (logCount - 12);
                            uint random = (uint)Random.Shared.NextInt64(0, uint.MaxValue + 1L);
                            if ((random & (delta - 1)) != 0)
                            {
                                return;
                            }
                        }
                    }

                    Interlocked.Add(ref sharedCounter, delta);
                }</code></pre>
                """);

            var pre = document.SingleOrDefault() as ParentTag;
            Assert.IsNotNull(pre);
            var code = pre.SingleOrDefault() as ParentTag;
            Assert.IsNotNull(code);
            var content = code.SingleOrDefault() as Content;
            Assert.IsNotNull(content);

            Assert.AreEqual(
                """
                static void Count(ref uint sharedCounter)
                {
                    uint currentCount = sharedCounter, delta = 1;
                    if (currentCount > 0)
                    {
                        int logCount = 31 - (int)uint.LeadingZeroCount(currentCount);
                        if (logCount >= 13)
                        {
                            delta = 1u << (logCount - 12);
                            uint random = (uint)Random.Shared.NextInt64(0, uint.MaxValue + 1L);
                            if ((random & (delta - 1)) != 0)
                            {
                                return;
                            }
                        }
                    }

                    Interlocked.Add(ref sharedCounter, delta);
                }
                """, content.ToString());
        }

        [TestMethod]
        public void Parse_CodeTagRazor_SingleContent()
        {
            var document = Document.Html.Parse(
                """
                <pre><code class="language-razor"><EditForm Model="Customer" method="post" OnSubmit="DisplayCustomer" FormName="customer">
                    <div>
                        <label>Name</label>
                        <InputText @bind-Value="Customer.Name" />
                    </div>
                    <AddressEditor @bind-Value="Customer.BillingAddress" />
                    <button>Send</button>
                </EditForm>

                @if (submitted)
                {
                    <!-- Display customer data -->
                    <h3>Customer</h3>
                    <p>Name: @Customer.Name</p>
                    <p>Street: @Customer.BillingAddress.Street</p>
                    <p>City: @Customer.BillingAddress.City</p>
                    <p>State: @Customer.BillingAddress.State</p>
                    <p>Zip: @Customer.BillingAddress.Zip</p>
                }

                @code {
                    public void DisplayCustomer()
                    {
                        submitted = true;
                    }

                    [SupplyParameterFromForm] Customer? Customer { get; set; }

                    protected override void OnInitialized() => Customer ??= new();

                    bool submitted = false;
                    public void Submit() => submitted = true;
                }</code></pre>
                """
                );

            var pre = document.SingleOrDefault() as ParentTag;
            Assert.IsNotNull(pre);
            var code = pre.SingleOrDefault() as ParentTag;
            Assert.IsNotNull(code);
            var content = code.SingleOrDefault() as Content;
            Assert.IsNotNull(content);

            Assert.AreEqual(
                """
                <EditForm Model="Customer" method="post" OnSubmit="DisplayCustomer" FormName="customer">
                    <div>
                        <label>Name</label>
                        <InputText @bind-Value="Customer.Name" />
                    </div>
                    <AddressEditor @bind-Value="Customer.BillingAddress" />
                    <button>Send</button>
                </EditForm>
                
                @if (submitted)
                {
                    <!-- Display customer data -->
                    <h3>Customer</h3>
                    <p>Name: @Customer.Name</p>
                    <p>Street: @Customer.BillingAddress.Street</p>
                    <p>City: @Customer.BillingAddress.City</p>
                    <p>State: @Customer.BillingAddress.State</p>
                    <p>Zip: @Customer.BillingAddress.Zip</p>
                }
                
                @code {
                    public void DisplayCustomer()
                    {
                        submitted = true;
                    }
                
                    [SupplyParameterFromForm] Customer? Customer { get; set; }
                
                    protected override void OnInitialized() => Customer ??= new();
                
                    bool submitted = false;
                    public void Submit() => submitted = true;
                }
                """, content.ToString());
        }
    }
}
