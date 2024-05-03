namespace Brackets.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ParseTests
{
    [TestMethod]
    public void Parse_InvalidAttrStrayTag_Parsed()
    {
        var document = Document.Html.Parse(
            """
            <figure class="intro-image intro-left">
                <img src="https://cdn.example.com/wp-content/uploads/2024/04/dangerous_ai_hero-800x450.jpg" alt="A modified photo of a 1956 scientist carefully bottling " ai with robotic arms from behind a protective wall.>
                <figcaption class="caption">
                    <div class="caption-text">
                        <a href="https://cdn.example.com/wp-content/uploads/2024/04/dangerous_ai_hero.jpg" class="enlarge-link" data-height="675" data-width="1200">Enlarge</a>
                    </div>
                    <div class="caption-credit">
                        <a rel="nofollow" class="caption-link" href="https://www.example.com/detail/news-photo/scientist-carefully-bottles-isotopes-with-robotic-arms-from-news-photo/514912704">Benj Edwards | Getty Images</a>
                    </div>
                </figcaption>
            </figure>
            """);

        var figure = document.First<ParentTag>();
        var img = figure.FirstOrDefault<Tag>();
        var figcaption = figure.FirstOrDefault<ParentTag>();
        Assert.AreEqual(2, figure.Count());
        Assert.IsNotNull(img);
        Assert.IsNotNull(figcaption);
    }
}
