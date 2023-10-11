namespace Brackets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ElementTests
{
    [TestMethod]
    public void First_EmptyDocument_ThrowsInvalidOperationException()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.First());
    }

    [TestMethod]
    public void First_SingleTag_ReturnsFirstElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.First() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void First_MultipleTags_ReturnsFirstElement()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.First() as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void FirstWithPredicate_EmptyDocument_ThrowsInvalidOperationException()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.First(element => true));
    }
    
    [TestMethod]
    public void FirstWithPredicate_SingleTag_ReturnsFirstElementWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.First(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void FirstWithPredicate_SingleTag_ThrowsInvalidOperationExceptionWhenPredicateNotSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.First(element => element is Tag { Name: "div" }));
    }
    
    [TestMethod]
    public void FirstWithPredicate_MultipleTags_ReturnsFirstElementWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.First(element => element is Tag { Name: "div" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("div", result.Name);
    }
    
    [TestMethod]
    public void FirstWithPredicate_MultipleTags_ThrowsInvalidOperationExceptionWhenNoElementSatisfiesPredicate()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.First(element => element is Tag { Name: "div" }));
    }

    [TestMethod]
    public void FirstOrDefault_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        var result = document.FirstOrDefault();
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FirstOrDefault_SingleTag_ReturnsFirstElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.FirstOrDefault() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void FirstOrDefault_MultipleTags_ReturnsFirstElement()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.FirstOrDefault() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void FirstOrDefaultWithPredicate_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        var result = document.FirstOrDefault(element => element is Tag { Name: "span" });
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void FirstOrDefaultWithPredicate_SingleTag_ReturnsFirstMatchingElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.FirstOrDefault(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void FirstOrDefaultWithPredicate_SingleTag_ReturnsNullWhenNoMatchingElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.FirstOrDefault(element => element is Tag { Name: "div" });
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FirstOrDefaultWithPredicate_MultipleTags_ReturnsFirstElementSatisfyingCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.FirstOrDefault(element => element is Tag { Name: "span" }) as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void FirstOrDefaultWithPredicate_MultipleTags_ReturnsNullWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.FirstOrDefault(element => element is Tag { Name: "div" });
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Last_EmptyDocument_ThrowsInvalidOperationException()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.Last());
    }

    [TestMethod]
    public void Last_SingleTag_ReturnsLastElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Last() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void Last_MultipleTags_ReturnsLastElement()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.Last() as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("Value3", result.First().ToString());
    }

    [TestMethod]
    public void LastWithPredicate_EmptyDocument_ThrowsInvalidOperationException()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.Last(element => true));
    }
    
    [TestMethod]
    public void LastWithPredicate_SingleTag_ReturnsLastElementWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Last(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void LastWithPredicate_SingleTag_ThrowsInvalidOperationExceptionWhenPredicateNotSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Last(element => element is Tag { Name: "div" }));
    }
    
    [TestMethod]
    public void LastWithPredicate_MultipleTags_ReturnsLastElementWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.Last(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void LastWithPredicate_MultipleTags_ThrowsWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Last(element => element is Tag { Name: "div" }));
    }

    [TestMethod]
    public void LastOrDefault_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        var result = document.LastOrDefault();
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LastOrDefault_SingleTag_ReturnsLastElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.LastOrDefault() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void LastOrDefault_MultipleTags_ReturnsLastElement()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.LastOrDefault() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void LastOrDefaultWithPredicate_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        var result = document.LastOrDefault(element => element is Tag { Name: "span" });
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void LastOrDefaultWithPredicate_SingleTag_ReturnsLastMatchingElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.LastOrDefault(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }
    
    [TestMethod]
    public void LastOrDefaultWithPredicate_SingleTag_ReturnsNullWhenNoMatchingElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.LastOrDefault(element => element is Tag { Name: "div" });
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LastOrDefaultWithPredicate_MultipleTags_ReturnsLastElementSatisfyingCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.LastOrDefault(element => element is Tag { Name: "span" }) as ParentTag;
        Assert.IsNotNull(result);
        Assert.AreEqual("Value3", result.First().ToString());
    }

    [TestMethod]
    public void LastOrDefaultWithPredicate_MultipleTags_ReturnsNullWhenNoElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.LastOrDefault(element => element is Tag { Name: "div" });
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Contains_MultipleTags_ReturnsTrue()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.IsTrue(document.First().Contains("Value1"));
    }

    [TestMethod]
    public void Single_EmptyDocument_Throws()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single());
    }

    [TestMethod]
    public void Single_SingleTag_ReturnsSingleElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Single() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void Single_MultipleTags_ThrowsWhenMultipleElements()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single());
    }

    [TestMethod]
    public void SingleOrDefault_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        Assert.IsNull(document.SingleOrDefault());
    }

    [TestMethod]
    public void SingleOrDefault_SingleTag_ReturnsSingleElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.SingleOrDefault() as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void SingleOrDefault_MultipleTags_Throws()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.SingleOrDefault());
    }

    [TestMethod]
    public void SingleWithPredicate_EmptyDocument_ThrowsInvalidOperationException()
    {
        var document = Document.Html.Parse("");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single(element => true));
    }

    [TestMethod]
    public void SingleWithPredicate_SingleTag_ReturnsSingleElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Single(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void SingleWithPredicate_MultipleTags_ThrowsWhenNoElementSatisfiesPatternMatchingCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single(element => element is Tag { Name: "div" }));
    }

    [TestMethod]
    public void SingleWithPredicate_MultipleTags_SingleElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.Single(element => element is Tag { Name: "div" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("div", result.Name);
    }

    [TestMethod]
    public void SingleWithPredicate_MultipleTags_ThrowsWhenMultipleElementsSatisfyCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span><div>Value4</div>");
        Assert.ThrowsException<InvalidOperationException>(() => document.Single(element => element is Tag { Name: "div" }));
    }

    [TestMethod]
    public void SingleOrDefaultWithPredicate_EmptyDocument_ReturnsNull()
    {
        var document = Document.Html.Parse("");
        Assert.IsNull(document.SingleOrDefault(element => true));
    }

    [TestMethod]
    public void SingleOrDefaultWithPredicate_SingleTag_ReturnsSingleElement()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.SingleOrDefault(element => element is Tag { Name: "span" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("span", result.Name);
    }

    [TestMethod]
    public void SingleOrDefaultWithPredicate_MultipleTags_ReturnsNullWhenNoElementSatisfiesPatternMatchingCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        Assert.IsNull(document.SingleOrDefault(element => element is Tag { Name: "div" }));
    }

    [TestMethod]
    public void SingleOrDefaultWithPredicate_MultipleTags_SingleElementSatisfiesCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.SingleOrDefault(element => element is Tag { Name: "div" }) as Tag;
        Assert.IsNotNull(result);
        Assert.AreEqual("div", result.Name);
    }

    [TestMethod]
    public void SingleOrDefaultWithPredicate_MultipleTags_ThrowsWhenMultipleElementsSatisfyCondition()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span><div>Value4</div>");
        Assert.ThrowsException<InvalidOperationException>(() => document.SingleOrDefault(element => element is Tag { Name: "div" }));
    }

    [TestMethod]
    public void All_EmptyDocument_ReturnsTrue()
    {
        var document = Document.Html.Parse("");
        var result = document.All(element => true);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void All_SingleTag_ReturnsTrueWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.All(element => element is Tag { Name: "span" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void All_SingleTag_ReturnsFalseWhenPredicateNotSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.All(element => element is Tag { Name: "div" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void All_MultipleTags_ReturnsTrueWhenAllElementsSatisfyPredicate()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.All(element => element is Tag { Name: "span" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void All_MultipleTags_ReturnsFalseWhenNotAllElementsSatisfyPredicate()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.All(element => element is Tag { Name: "span" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Any_EmptyDocument_ReturnsFalse()
    {
        var document = Document.Html.Parse("");
        var result = document.Any();
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Any_SingleTag_ReturnsTrue()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Any();
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Any_MultipleTags_ReturnsTrue()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.Any();
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AnyWithPredicate_EmptyDocument_ReturnsFalse()
    {
        var document = Document.Html.Parse("");
        var result = document.Any(element => element is Tag { Name: "span" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void AnyWithPredicate_SingleTag_ReturnsTrueWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Any(element => element is Tag { Name: "span" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AnyWithPredicate_SingleTag_ReturnsFalseWhenPredicateNotSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var result = document.Any(element => element is Tag { Name: "div" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void AnyWithPredicate_MultipleTags_ReturnsTrueWhenAnyElementSatisfiesPredicate()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var result = document.Any(element => element is Tag { Name: "div" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AnyWithPredicate_MultipleTags_ReturnsFalseWhenNoElementSatisfiesPredicate()
    {
        var document = Document.Html.Parse("<span>Value1</span><span>Value2</span><span>Value3</span>");
        var result = document.Any(element => element is Tag { Name: "div" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Count_EmptyDocument_ReturnsZero()
    {
        var document = Document.Html.Parse("");
        var count = document.Count();
        Assert.AreEqual(0, count);
    }
    
    [TestMethod]
    public void Count_SingleTag_ReturnsOne()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var count = document.Count();
        Assert.AreEqual(1, count);
    }
    
    [TestMethod]
    public void Count_MultipleTags_ReturnsNumberOfTags()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var count = document.Count();
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public void CountWithPredicate_EmptyDocument_ReturnsZero()
    {
        var document = Document.Html.Parse("");
        var count = document.Count(element => element is Tag { Name: "span" });
        Assert.AreEqual(0, count);
    }
    
    [TestMethod]
    public void CountWithPredicate_SingleTag_ReturnsOneWhenPredicateSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var count = document.Count(element => element is Tag { Name: "span" });
        Assert.AreEqual(1, count);
    }
    
    [TestMethod]
    public void CountWithPredicate_SingleTag_ReturnsZeroWhenPredicateNotSatisfied()
    {
        var document = Document.Html.Parse("<span>Value1</span>");
        var count = document.Count(element => element is Tag { Name: "div" });
        Assert.AreEqual(0, count);
    }
    
    [TestMethod]
    public void CountWithPredicate_MultipleTags_ReturnsNumberOfMatchingTags()
    {
        var document = Document.Html.Parse("<span>Value1</span><div>Value2</div><span>Value3</span>");
        var count = document.Count(element => element is Tag { Name: "span" });
        Assert.AreEqual(2, count);
    }
}