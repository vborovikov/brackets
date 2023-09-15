namespace CookTests.Markup.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;
    using Brackets.JPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QueryExpressionTests
    {
        [TestMethod]
        public void AndExpressionTest()
        {
            CompositeExpression compositeExpression = new CompositeExpression(QueryOperator.And)
            {
                Expressions =
                {
                    new BooleanQueryExpression(QueryOperator.Exists, new List<PathFilter> { new FieldFilter("FirstName") }, null),
                    new BooleanQueryExpression(QueryOperator.Exists, new List<PathFilter> { new FieldFilter("LastName") }, null)
                }
            };

            JsonDocument o1 = JsonDocument.Parse(@"
            {
                ""Title"": ""Title!"",
                ""FirstName"": ""FirstName!"",
                ""LastName"": ""LastName!""
            }");

            Assert.IsTrue(compositeExpression.IsMatch(o1.RootElement, o1.RootElement));

            JsonDocument o2 = JsonDocument.Parse(@"
            {
                ""Title"": ""Title!"",
                ""FirstName"": ""FirstName!""
            }");

            Assert.IsFalse(compositeExpression.IsMatch(o2.RootElement, o2.RootElement));

            JsonDocument o3 = JsonDocument.Parse(@"
            {
                ""Title"": ""Title!""
            }");

            Assert.IsFalse(compositeExpression.IsMatch(o3.RootElement, o3.RootElement));
        }

        [TestMethod]
        public void OrExpressionTest()
        {
            CompositeExpression compositeExpression = new CompositeExpression(QueryOperator.Or)
            {
                Expressions = 
                {
                    new BooleanQueryExpression(QueryOperator.Exists, new List<PathFilter> { new FieldFilter("FirstName") }, null),
                    new BooleanQueryExpression(QueryOperator.Exists, new List<PathFilter> { new FieldFilter("LastName") }, null)
                }
            };

            JsonDocument o1 = JsonDocument.Parse(@"
            {
                ""Title"": ""Title!"",
                ""FirstName"": ""FirstName!"",
                ""LastName"": ""LastName!""
            }");

            Assert.IsTrue(compositeExpression.IsMatch(o1.RootElement, o1.RootElement));

            JsonDocument o2 = JsonDocument.Parse(@"
            {
                ""Title"": ""Title!"",
                ""FirstName"": ""FirstName!""
            }");

            Assert.IsTrue(compositeExpression.IsMatch(o2.RootElement, o2.RootElement));

            JsonDocument o3 = JsonDocument.Parse(@"
            {
                ""Title"": ""Title!""
            }");

            Assert.IsFalse(compositeExpression.IsMatch(o3.RootElement, o3.RootElement));
        }

        [TestMethod]
        public void BooleanExpressionTest_RegexEqualsOperator()
        {
            BooleanQueryExpression e1 = new BooleanQueryExpression(QueryOperator.RegexEquals,
                new List<PathFilter> { new ArrayIndexFilter() },
                JsonDocument.Parse("\"/foo.*d/\"").RootElement);

            var nullElt = JsonDocument.Parse("null").RootElement;
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[\"food\"]").RootElement));
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[\"fooood and drink\"]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[\"FOOD\"]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[\"foo\", \"foog\", \"good\"]").RootElement));

            BooleanQueryExpression e2 = new BooleanQueryExpression(QueryOperator.RegexEquals,
                new List<PathFilter> { new ArrayIndexFilter() }, JsonDocument.Parse("\"/Foo.*d/i\"").RootElement);

            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[\"food\"]").RootElement));
            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[\"fooood and drink\"]").RootElement));
            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[\"FOOD\"]").RootElement));
            Assert.IsFalse(e2.IsMatch(nullElt, JsonDocument.Parse("[\"foo\", \"foog\", \"good\"]").RootElement));
        }

        [TestMethod]
        public void BooleanExpressionTest_RegexEqualsOperator_CornerCase()
        {
            BooleanQueryExpression e1 = new BooleanQueryExpression(QueryOperator.RegexEquals,
                new List<PathFilter> { new ArrayIndexFilter() }, JsonDocument.Parse("\"/// comment/\"").RootElement);

            var nullElt = JsonDocument.Parse("null").RootElement;
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[\"// comment\"]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[\"//comment\", \"/ comment\"]").RootElement));

            BooleanQueryExpression e2 = new BooleanQueryExpression(QueryOperator.RegexEquals,
                new List<PathFilter> { new ArrayIndexFilter() },
                JsonDocument.Parse("\"/<tag>.*</tag>/i\"").RootElement);

            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[\"<Tag>Test</Tag>\", \"\"]").RootElement));
            Assert.IsFalse(e2.IsMatch(nullElt, JsonDocument.Parse("[\"<tag>Test<tag>\"]").RootElement));
        }

        [TestMethod]
        public void BooleanExpressionTest()
        {
            BooleanQueryExpression e1 = new BooleanQueryExpression(QueryOperator.LessThan,
                new List<PathFilter> { new ArrayIndexFilter() }, JsonDocument.Parse("3").RootElement);

            var nullElt = JsonDocument.Parse("null").RootElement;
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[1, 2, 3, 4, 5]").RootElement));
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[2, 3, 4, 5]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[3, 4, 5]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[4, 5]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[\"11\", 5]").RootElement));

            BooleanQueryExpression e2 = new BooleanQueryExpression(QueryOperator.LessThanOrEquals,
                new List<PathFilter> { new ArrayIndexFilter() }, JsonDocument.Parse("3").RootElement);

            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[1, 2, 3, 4, 5]").RootElement));
            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[2, 3, 4, 5]").RootElement));
            Assert.IsTrue(e2.IsMatch(nullElt, JsonDocument.Parse("[3, 4, 5]").RootElement));
            Assert.IsFalse(e2.IsMatch(nullElt, JsonDocument.Parse("[4, 5]").RootElement));
            Assert.IsFalse(e2.IsMatch(nullElt, JsonDocument.Parse("[\"11\", 5]").RootElement));
        }

        [TestMethod]
        public void BooleanExpressionTest_GreaterThanOperator()
        {
            BooleanQueryExpression e1 = new BooleanQueryExpression(QueryOperator.GreaterThan,
                new List<PathFilter> { new ArrayIndexFilter() }, JsonDocument.Parse("3").RootElement);

            var nullElt = JsonDocument.Parse("null").RootElement;
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[\"2\", \"26\"]").RootElement));
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[2, 26]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[2, 3]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[\"2\", \"3\"]").RootElement));
        }

        [TestMethod]
        public void BooleanExpressionTest_GreaterThanOrEqualsOperator()
        {
            BooleanQueryExpression e1 = new BooleanQueryExpression(QueryOperator.GreaterThanOrEquals,
                new List<PathFilter> { new ArrayIndexFilter() }, JsonDocument.Parse("3").RootElement);

            var nullElt = JsonDocument.Parse("null").RootElement;
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[\"2\", \"26\"]").RootElement));
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[2, 26]").RootElement));
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[2, 3]").RootElement));
            Assert.IsTrue(e1.IsMatch(nullElt, JsonDocument.Parse("[\"2\", \"3\"]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[2, 1]").RootElement));
            Assert.IsFalse(e1.IsMatch(nullElt, JsonDocument.Parse("[\"2\", \"1\"]").RootElement));
        }
    }
}
