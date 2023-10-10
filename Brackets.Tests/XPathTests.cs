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

        [TestMethod]
        public void XPath_FeedLinks2_Found()
        {
            var document = LoadSample("japantimes.html");
            var links = document.Find("/html/head/link[@rel='alternate']");
            Assert.AreEqual(1, links.Count());
        }

        private static readonly string[] sources = new string[]
        {
            """
            <img width="900" height="507" src="https://www.example.com/wp-content/uploads/2023/10/BTS-2023-10-W1.webp" class="webfeedsFeaturedVisual wp-post-image" alt="Vacationing - Behind The Screen - 2023 October, Week 1" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/10/BTS-2023-10-W1.webp 900w, https://www.example.com/wp-content/uploads/2023/10/BTS-2023-10-W1-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/10/BTS-2023-10-W1-768x433.webp 768w, https://www.example.com/wp-content/uploads/2023/10/BTS-2023-10-W1-800x450.webp 800w" sizes="(max-width: 900px) 100vw, 900px" /><p>Welcome to another Behind The Screen vlog. This week I discuss my upcoming plans around vacationing and what that'll mean for my other commitments!</p>
            The post <a href="https://www.example.com/2023/10/06/vacationing-behind-the-screen-2023-october-week-1/">Vacationing – Behind The Screen – 2023 October, Week 1</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="How To Organize Autofac Modules - 5 Tips For Organizing Code" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/How-To-Organize-Autofac-Modules-5-Tips-For-Organizing-Code.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>You've started using Autofac for dependency injection in C#, but now you're wondering how to organize Autofac modules most effectively. Dive in for 5 easy tips!</p>
            The post <a href="https://www.example.com/2023/10/02/how-to-organize-autofac-modules-5-tips-for-organizing-code/">How To Organize Autofac Modules: 5 Tips For Organizing Code</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="From Chaos to Cohesion - How To Organize Code For Vertical Slices" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" fetchpriority="high" srcset="https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/From-Chaos-to-Cohesion-How-To-Organize-Code-For-Vertical-Slices.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>Learn how to organize code for vertical slices and use Autofac modules effectively. Discover how this improves code maintainability and enhances collaboration!</p>
            The post <a href="https://www.example.com/2023/10/09/from-chaos-to-cohesion-how-to-organize-code-for-vertical-slices/">From Chaos to Cohesion: How To Organize Code For Vertical Slices</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="Strong Coding Foundations - What Are The Principles of Programming Languages?" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/Strong-Coding-Foundations-What-Are-The-Principles-of-Programming-Languages.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>Let's answer "What are the principles of programming languages" so that you, as a beginner, can help decide how to navigate selection of programming languages.</p>
            The post <a href="https://www.example.com/2023/10/06/strong-coding-foundations-what-are-the-principles-of-programming-languages/">Strong Coding Foundations – What Are The Principles of Programming Languages?</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="The Builder Pattern in C# - How To Leverage Extension Methods Creatively" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/The-Builder-Pattern-in-C-How-To-Leverage-Extension-Methods-Creatively.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>If you want to see examples of the builder pattern in C#, dive into this article. We'll explore how the builder pattern in C# works with code examples!</p>
            The post <a href="https://www.example.com/2023/10/01/the-builder-pattern-in-c-how-to-leverage-extension-methods-creatively/">The Builder Pattern in C#: How To Leverage Extension Methods Creatively</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="How to Master Unit Testing in C# with xUnit and Moq" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" srcset="https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/How-to-Master-Unit-Testing-in-C-with-xUnit-and-Moq.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>Interested in unit testing in C#? Let's look at xUnit and Moq for unit testing! We'll explore mocking external dependencies and the role of these in unit tests.</p>
            The post <a href="https://www.example.com/2023/10/08/xunit-and-moq-how-to-master-unit-testing-in-c/">xUnit And Moq – How To Master Unit Testing In C#</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="Vertical Slice Architecture in C# - Examples on How To Streamline Code" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/Vertical-Slice-Architecture-in-C-Examples-on-How-To-Streamline-Code.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>Let's implement a vertical slice architecture in C#! We start by defining vertical slice architecture &#038; dive into a C# vertical slice example. Let's dive in!</p>
            The post <a href="https://www.example.com/2023/10/03/vertical-slice-architecture-in-c-examples-on-how-to-streamline-code/">Vertical Slice Architecture in C# – Examples on How To Streamline Code</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="Beginner&#039;s Guide To Software Engineering - How To Get Started Today" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/Beginners-Guide-To-Software-Engineering-How-To-Get-Started-Today.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>Interested in an introduction to software development? Need those first steps in programming? Then check out this beginner's guide to software engineering!</p>
            The post <a href="https://www.example.com/2023/10/04/beginners-guide-to-software-engineering-how-to-get-started-today/">Beginner’s Guide To Software Engineering – How To Get Started Today</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor-1024x576.webp" class="webfeedsFeaturedVisual wp-post-image" alt="Step-by-Step Guide: How to Make a Todo List in C# with ASP.NET Core Blazor" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor-1024x576.webp 1024w, https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor-1536x864.webp 1536w, https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor-800x450.webp 800w, https://www.example.com/wp-content/uploads/2023/09/Step-by-Step-Guide-How-to-Make-a-Todo-List-in-CSharp-with-ASPNET-Core-Blazor.webp 1920w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>Learn how to make a todo list in C# using ASP.NET Core and tackle data binding and user interface design. Blazor todo list - Perfect for a beginner portfolio!</p>
            The post <a href="https://www.example.com/2023/10/05/step-by-step-guide-how-to-make-a-todo-list-in-c-with-asp-net-core-blazor/">Step-by-Step Guide: How to Make a Todo List in C# with ASP.NET Core Blazor</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
            """
            <img width="1024" height="576" src="https://www.example.com/wp-content/uploads/2023/10/DLW-12.webp" class="webfeedsFeaturedVisual wp-post-image" alt="Exceptions Galore! - Example Weekly Issue 12" decoding="async" style="display: block; margin-bottom: 5px; clear:both;max-width: 100%;" link_thumbnail="" loading="lazy" srcset="https://www.example.com/wp-content/uploads/2023/10/DLW-12.webp 1024w, https://www.example.com/wp-content/uploads/2023/10/DLW-12-300x169.webp 300w, https://www.example.com/wp-content/uploads/2023/10/DLW-12-768x432.webp 768w, https://www.example.com/wp-content/uploads/2023/10/DLW-12-800x450.webp 800w" sizes="(max-width: 1024px) 100vw, 1024px" /><p>In this issue of Example Weekly, I share useful resources with a couple of videos focused on exceptions in C#! Thank you for supporting! Check it out!</p>
            The post <a href="https://www.example.com/2023/10/07/exceptions-galore-dev-leader-weekly-issue-12/">Exceptions Galore! – Example Weekly Issue 12</a> appeared first on <a href="https://www.example.com">Example</a>.
            """,
        };

        [TestMethod]
        public void XPath_AsyncFind_FoundOne()
        {
            Parallel.ForEach(sources, source =>
            {
                var html = Document.Html.Parse(source);

                var found = false;
                foreach (var img in html.Find("//img").ToArray())
                {
                    if (img.Parent is ParentTag parent)
                    {
                        parent.Remove(img);
                        found = true;
                    }
                }
                Assert.IsTrue(found);
            });
        }

        private static Document LoadSample(string fileName)
        {
            using var htmlStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brackets.Tests.Samples." + fileName);
            using var htmlReader = new StreamReader(htmlStream, Encoding.UTF8);
            return Document.Html.Parse(htmlReader.ReadToEnd());
        }
    }
}