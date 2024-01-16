namespace Brackets.Tests;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tools;

[TestClass]
public class ElementCloneTests
{
    [DataTestMethod]
    [DataRow("benq.html")]
    [DataRow("broken.xml")]
    [DataRow("dotnet8perf.html")]
    [DataRow("google.html")]
    [DataRow("head_script.html")]
    [DataRow("japantimes.html")]
    [DataRow("newsde.html")]
    public void Clone_Document_Same(string fileName)
    {
        var document = Document.Html.Parse(Samples.GetString(fileName));
        var clonedDocument = document.Clone();

        Assert.IsNotNull(clonedDocument);
        AssertAreEqual(document, clonedDocument);
    }

    private void AssertAreEqual(IEnumerable<Element> expectedElements, IEnumerable<Element> actualElements)
    {
        var expectedEnumerator = expectedElements.GetEnumerator();
        var actualEnumerator = actualElements.GetEnumerator();

        while (actualEnumerator.MoveNext() && expectedEnumerator.MoveNext())
        {
            var expected = expectedEnumerator.Current;
            var actual = actualEnumerator.Current;

            var elemStr = $"{expected.GetType().Name}: {expected.ToDebugString()}";

            Assert.AreNotSame(expected, actual, elemStr);
            Assert.AreEqual(expected.Offset, actual.Offset, elemStr);
            Assert.AreEqual(expected.Length, actual.Length, elemStr);
            if (expected is CharacterData && actual is CharacterData)
            {
                Assert.AreEqual(expected.ToString(), actual.ToString(), elemStr);
            }
            else if (expected is Tag expectedTag && actual is Tag actualTag)
            {
                AssertAreEqual(expectedTag.EnumerateAttributes(), actualTag.EnumerateAttributes());

                if (expectedTag is ParentTag expectedParent && actualTag is ParentTag actualParent)
                {
                    AssertAreEqual(expectedParent, actualParent);
                }
            }
            else if (expected is Attribute expectedAttr && actual is Attribute actualAttr)
            {
                Assert.AreEqual(expectedAttr.HasValue, actualAttr.HasValue, elemStr);
                Assert.AreEqual(expectedAttr.Name, actualAttr.Name, elemStr);
                Assert.AreEqual(expectedAttr.ToString(), actualAttr.ToString(), elemStr);
            }
        }

        Assert.IsFalse(expectedEnumerator.MoveNext());
        Assert.IsFalse(actualEnumerator.MoveNext());
    }
}
