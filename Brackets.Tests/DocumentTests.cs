namespace Brackets.Tests.Markup
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
