namespace CookTests.Markup.JPath
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Brackets.JPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JPathParseTests
    {
        [TestMethod]
        public void BooleanQuery_TwoValues()
        {
            var path = new JPathParser("[?(1 > 2)]");
            Assert.AreEqual(1, path.Filters.Count);
            BooleanQueryExpression booleanExpression = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(1, ((JsonElement)booleanExpression.Left!).GetInt32());
            Assert.AreEqual(2, ((JsonElement)booleanExpression.Right!).GetInt32());
            Assert.AreEqual(QueryOperator.GreaterThan, booleanExpression.Operator);
        }

        [TestMethod]
        public void BooleanQuery_TwoPaths()
        {
            var path = new JPathParser("[?(@.price > @.max_price)]");
            Assert.AreEqual(1, path.Filters.Count);
            BooleanQueryExpression booleanExpression = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            List<PathFilter> leftPaths = (List<PathFilter>)booleanExpression.Left;
            List<PathFilter> rightPaths = (List<PathFilter>)booleanExpression.Right!;

            Assert.AreEqual("price", ((FieldFilter)leftPaths[0]).Name);
            Assert.AreEqual("max_price", ((FieldFilter)rightPaths[0]).Name);
            Assert.AreEqual(QueryOperator.GreaterThan, booleanExpression.Operator);
        }

        [TestMethod]
        public void SingleProperty()
        {
            var path = new JPathParser("Blah");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SingleQuotedProperty()
        {
            var path = new JPathParser("['Blah']");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SingleQuotedPropertyWithWhitespace()
        {
            var path = new JPathParser("[  'Blah'  ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SingleQuotedPropertyWithDots()
        {
            var path = new JPathParser("['Blah.Ha']");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah.Ha", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SingleQuotedPropertyWithBrackets()
        {
            var path = new JPathParser("['[*]']");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("[*]", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SinglePropertyWithRoot()
        {
            var path = new JPathParser("$.Blah");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SinglePropertyWithRootWithStartAndEndWhitespace()
        {
            var path = new JPathParser(" $.Blah ");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void RootWithBadWhitespace()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("$ .Blah"); }, @"Unexpected character while parsing path:  ");
        }

        [TestMethod]
        public void NoFieldNameAfterDot()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("$.Blah."); }, @"Unexpected end while parsing path.");
        }

        [TestMethod]
        public void RootWithBadWhitespace2()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("$. Blah"); }, @"Unexpected character while parsing path:  ");
        }

        [TestMethod]
        public void WildcardPropertyWithRoot()
        {
            var path = new JPathParser("$.*");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(null, ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void WildcardArrayWithRoot()
        {
            var path = new JPathParser("$.[*]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(null, ((ArrayIndexFilter)path.Filters[0]).Index);
        }

        [TestMethod]
        public void RootArrayNoDot()
        {
            var path = new JPathParser("$[1]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(1, ((ArrayIndexFilter)path.Filters[0]).Index);
        }

        [TestMethod]
        public void WildcardArray()
        {
            var path = new JPathParser("[*]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(null, ((ArrayIndexFilter)path.Filters[0]).Index);
        }

        [TestMethod]
        public void WildcardArrayWithProperty()
        {
            var path = new JPathParser("[ * ].derp");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual(null, ((ArrayIndexFilter)path.Filters[0]).Index);
            Assert.AreEqual("derp", ((FieldFilter)path.Filters[1]).Name);
        }

        [TestMethod]
        public void QuotedWildcardPropertyWithRoot()
        {
            var path = new JPathParser("$.['*']");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("*", ((FieldFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void SingleScanWithRoot()
        {
            var path = new JPathParser("$..Blah");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual("Blah", ((ScanFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void QueryTrue()
        {
            var path = new JPathParser("$.elements[?(true)]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("elements", ((FieldFilter)path.Filters[0]).Name);
            Assert.AreEqual(QueryOperator.Exists, ((QueryFilter)path.Filters[1]).Expression.Operator);
        }

        [TestMethod]
        public void ScanQuery()
        {
            var path = new JPathParser("$.elements..[?(@.id=='AAA')]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("elements", ((FieldFilter)path.Filters[0]).Name);

            BooleanQueryExpression expression = (BooleanQueryExpression)((QueryScanFilter) path.Filters[1]).Expression;

            List<PathFilter> paths = (List<PathFilter>)expression.Left;

            Assert.IsInstanceOfType(paths[0], typeof(FieldFilter));
        }

        [TestMethod]
        public void WildcardScanWithRoot()
        {
            var path = new JPathParser("$..*");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(null, ((ScanFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void WildcardScanWithRootWithWhitespace()
        {
            var path = new JPathParser("$..* ");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(null, ((ScanFilter)path.Filters[0]).Name);
        }

        [TestMethod]
        public void TwoProperties()
        {
            var path = new JPathParser("Blah.Two");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            Assert.AreEqual("Two", ((FieldFilter)path.Filters[1]).Name);
        }

        [TestMethod]
        public void OnePropertyOneScan()
        {
            var path = new JPathParser("Blah..Two");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            Assert.AreEqual("Two", ((ScanFilter)path.Filters[1]).Name);
        }

        [TestMethod]
        public void SinglePropertyAndIndexer()
        {
            var path = new JPathParser("Blah[0]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            Assert.AreEqual(0, ((ArrayIndexFilter)path.Filters[1]).Index);
        }

        [TestMethod]
        public void SinglePropertyAndExistsQuery()
        {
            var path = new JPathParser("Blah[ ?( @..name ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Exists, expressions.Operator);
            List<PathFilter> paths = (List<PathFilter>)expressions.Left;
            Assert.AreEqual(1, paths.Count);
            Assert.AreEqual("name", ((ScanFilter)paths[0]).Name);
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithWhitespace()
        {
            var path = new JPathParser("Blah[ ?( @.name=='hi' ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Equals, expressions.Operator);
            Assert.AreEqual("hi", ((JsonElement)expressions.Right!).ToString());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithEscapeQuote()
        {
            var path = new JPathParser(@"Blah[ ?( @.name=='h\'i' ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Equals, expressions.Operator);
            Assert.AreEqual("h'i", ((JsonElement)expressions.Right!).ToString());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithDoubleEscape()
        {
            var path = new JPathParser(@"Blah[ ?( @.name=='h\\i' ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Equals, expressions.Operator);
            Assert.AreEqual("h\\i", ((JsonElement)expressions.Right!).ToString());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithRegexAndOptions()
        {
            var path = new JPathParser("Blah[ ?( @.name=~/hi/i ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.RegexEquals, expressions.Operator);
            Assert.AreEqual("/hi/i", ((JsonElement)expressions.Right!).ToString());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithRegex()
        {
            var path = new JPathParser("Blah[?(@.title =~ /^.*Sword.*$/)]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.RegexEquals, expressions.Operator);
            Assert.AreEqual("/^.*Sword.*$/", ((JsonElement)expressions.Right!).ToString());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithEscapedRegex()
        {
            var path = new JPathParser(@"Blah[?(@.title =~ /[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g)]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.RegexEquals, expressions.Operator);
            Assert.AreEqual(@"/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g", ((JsonElement)expressions.Right!).ToString());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithOpenRegex()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser(@"Blah[?(@.title =~ /[\"); }, "Path ended with an open regex.");
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithUnknownEscape()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser(@"Blah[ ?( @.name=='h\i' ) ]"); }, @"Unknown escape character: \i");
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithFalse()
        {
            var path = new JPathParser("Blah[ ?( @.name==false ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Equals, expressions.Operator);
            Assert.AreEqual(false, ((JsonElement)expressions.Right!).GetBoolean());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithTrue()
        {
            var path = new JPathParser("Blah[ ?( @.name==true ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Equals, expressions.Operator);
            Assert.AreEqual(true, ((JsonElement)expressions.Right!).GetBoolean());
        }

        [TestMethod]
        public void SinglePropertyAndFilterWithNull()
        {
            var path = new JPathParser("Blah[ ?( @.name==null ) ]");
            Assert.AreEqual(2, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[1]).Expression;
            Assert.AreEqual(QueryOperator.Equals, expressions.Operator);
            Assert.IsTrue(((JsonElement)expressions.Right!).ValueKind == JsonValueKind.Null);
        }

        [TestMethod]
        public void FilterWithScan()
        {
            var path = new JPathParser("[?(@..name<>null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            List<PathFilter> paths = (List<PathFilter>)expressions.Left;
            Assert.AreEqual("name", ((ScanFilter)paths[0]).Name);
        }

        [TestMethod]
        public void FilterWithNotEquals()
        {
            var path = new JPathParser("[?(@.name<>null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.NotEquals, expressions.Operator);
        }

        [TestMethod]
        public void FilterWithNotEquals2()
        {
            var path = new JPathParser("[?(@.name!=null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.NotEquals, expressions.Operator);
        }

        [TestMethod]
        public void FilterWithLessThan()
        {
            var path = new JPathParser("[?(@.name<null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.LessThan, expressions.Operator);
        }

        [TestMethod]
        public void FilterWithLessThanOrEquals()
        {
            var path = new JPathParser("[?(@.name<=null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.LessThanOrEquals, expressions.Operator);
        }

        [TestMethod]
        public void FilterWithGreaterThan()
        {
            var path = new JPathParser("[?(@.name>null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.GreaterThan, expressions.Operator);
        }

        [TestMethod]
        public void FilterWithGreaterThanOrEquals()
        {
            var path = new JPathParser("[?(@.name>=null)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.GreaterThanOrEquals, expressions.Operator);
        }

        [TestMethod]
        public void FilterWithInteger()
        {
            var path = new JPathParser("[?(@.name>=12)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(12, ((JsonElement)expressions.Right!).GetInt32());
        }

        [TestMethod]
        public void FilterWithNegativeInteger()
        {
            var path = new JPathParser("[?(@.name>=-12)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(-12, ((JsonElement)expressions.Right!).GetInt32());
        }

        [TestMethod]
        public void FilterWithFloat()
        {
            var path = new JPathParser("[?(@.name>=12.1)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(12.1d, ((JsonElement)expressions.Right!).GetDouble());
        }

        [TestMethod]
        public void FilterExistWithAnd()
        {
            var path = new JPathParser("[?(@.name&&@.title)]");
            CompositeExpression expressions = (CompositeExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.And, expressions.Operator);
            Assert.AreEqual(2, expressions.Expressions.Count);

            var first = (BooleanQueryExpression)expressions.Expressions[0];
            var firstPaths = (List<PathFilter>)first.Left;
            Assert.AreEqual("name", ((FieldFilter)firstPaths[0]).Name);
            Assert.AreEqual(QueryOperator.Exists, first.Operator);

            var second = (BooleanQueryExpression)expressions.Expressions[1];
            var secondPaths = (List<PathFilter>)second.Left;
            Assert.AreEqual("title", ((FieldFilter)secondPaths[0]).Name);
            Assert.AreEqual(QueryOperator.Exists, second.Operator);
        }

        [TestMethod]
        public void FilterExistWithAndOr()
        {
            var path = new JPathParser("[?(@.name&&@.title||@.pie)]");
            CompositeExpression andExpression = (CompositeExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(QueryOperator.And, andExpression.Operator);
            Assert.AreEqual(2, andExpression.Expressions.Count);

            var first = (BooleanQueryExpression)andExpression.Expressions[0];
            var firstPaths = (List<PathFilter>)first.Left;
            Assert.AreEqual("name", ((FieldFilter)firstPaths[0]).Name);
            Assert.AreEqual(QueryOperator.Exists, first.Operator);

            CompositeExpression orExpression = (CompositeExpression)andExpression.Expressions[1];
            Assert.AreEqual(2, orExpression.Expressions.Count);

            var orFirst = (BooleanQueryExpression)orExpression.Expressions[0];
            var orFirstPaths = (List<PathFilter>)orFirst.Left;
            Assert.AreEqual("title", ((FieldFilter)orFirstPaths[0]).Name);
            Assert.AreEqual(QueryOperator.Exists, orFirst.Operator);

            var orSecond = (BooleanQueryExpression)orExpression.Expressions[1];
            var orSecondPaths = (List<PathFilter>)orSecond.Left;
            Assert.AreEqual("pie", ((FieldFilter)orSecondPaths[0]).Name);
            Assert.AreEqual(QueryOperator.Exists, orSecond.Operator);
        }

        [TestMethod]
        public void FilterWithRoot()
        {
            var path = new JPathParser("[?($.name>=12.1)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            List<PathFilter> paths = (List<PathFilter>)expressions.Left;
            Assert.AreEqual(2, paths.Count);
            Assert.IsInstanceOfType(paths[0], typeof(RootFilter));
            Assert.IsInstanceOfType(paths[1], typeof(FieldFilter));
        }

        [TestMethod]
        public void BadOr1()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name||)]"), "Unexpected character while parsing path query: )");
        }

        [TestMethod]
        public void BaddOr2()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name|)]"), "Unexpected character while parsing path query: |");
        }

        [TestMethod]
        public void BaddOr3()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name|"), "Unexpected character while parsing path query: |");
        }

        [TestMethod]
        public void BaddOr4()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name||"), "Path ended with open query.");
        }

        [TestMethod]
        public void NoAtAfterOr()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name||s"), "Unexpected character while parsing path query: s");
        }

        [TestMethod]
        public void NoPathAfterAt()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name||@"), @"Path ended with open query.");
        }

        [TestMethod]
        public void NoPathAfterDot()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name||@."), @"Unexpected end while parsing path.");
        }

        [TestMethod]
        public void NoPathAfterDot2()
        {
            Assert.ThrowsException<JsonException>(() => new JPathParser("[?(@.name||@.)]"), @"Unexpected end while parsing path.");
        }

        [TestMethod]
        public void FilterWithFloatExp()
        {
            var path = new JPathParser("[?(@.name>=5.56789e+0)]");
            BooleanQueryExpression expressions = (BooleanQueryExpression)((QueryFilter)path.Filters[0]).Expression;
            Assert.AreEqual(5.56789e+0, ((JsonElement)expressions.Right!).GetDouble());
        }

        [TestMethod]
        public void MultiplePropertiesAndIndexers()
        {
            var path = new JPathParser("Blah[0]..Two.Three[1].Four");
            Assert.AreEqual(6, path.Filters.Count);
            Assert.AreEqual("Blah", ((FieldFilter)path.Filters[0]).Name);
            Assert.AreEqual(0, ((ArrayIndexFilter)path.Filters[1]).Index);
            Assert.AreEqual("Two", ((ScanFilter)path.Filters[2]).Name);
            Assert.AreEqual("Three", ((FieldFilter)path.Filters[3]).Name);
            Assert.AreEqual(1, ((ArrayIndexFilter)path.Filters[4]).Index);
            Assert.AreEqual("Four", ((FieldFilter)path.Filters[5]).Name);
        }

        [TestMethod]
        public void BadCharactersInIndexer()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("Blah[[0]].Two.Three[1].Four"); }, @"Unexpected character while parsing path indexer: [");
        }

        [TestMethod]
        public void UnclosedIndexer()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("Blah[0"); }, @"Path ended with open indexer.");
        }

        [TestMethod]
        public void IndexerOnly()
        {
            var path = new JPathParser("[111119990]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(111119990, ((ArrayIndexFilter)path.Filters[0]).Index);
        }

        [TestMethod]
        public void IndexerOnlyWithWhitespace()
        {
            var path = new JPathParser("[  10  ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(10, ((ArrayIndexFilter)path.Filters[0]).Index);
        }

        [TestMethod]
        public void MultipleIndexes()
        {
            var path = new JPathParser("[111119990,3]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(2, ((ArrayMultipleIndexFilter)path.Filters[0]).Indexes.Count());
            Assert.AreEqual(111119990, ((ArrayMultipleIndexFilter)path.Filters[0]).Indexes.First());
            Assert.AreEqual(3, ((ArrayMultipleIndexFilter)path.Filters[0]).Indexes.ElementAt(1));
        }

        [TestMethod]
        public void MultipleIndexesWithWhitespace()
        {
            var path = new JPathParser("[   111119990  ,   3   ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(2, ((ArrayMultipleIndexFilter)path.Filters[0]).Indexes.Count());
            Assert.AreEqual(111119990, ((ArrayMultipleIndexFilter)path.Filters[0]).Indexes.First());
            Assert.AreEqual(3, ((ArrayMultipleIndexFilter)path.Filters[0]).Indexes.ElementAt(1));
        }

        [TestMethod]
        public void MultipleQuotedIndexes()
        {
            var path = new JPathParser("['111119990','3']");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(2, ((FieldMultipleFilter)path.Filters[0]).Names.Count());
            Assert.AreEqual("111119990", ((FieldMultipleFilter)path.Filters[0]).Names.First());
            Assert.AreEqual("3", ((FieldMultipleFilter)path.Filters[0]).Names.ElementAt(1));
        }

        [TestMethod]
        public void MultipleQuotedIndexesWithWhitespace()
        {
            var path = new JPathParser("[ '111119990' , '3' ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(2, ((FieldMultipleFilter)path.Filters[0]).Names.Count());
            Assert.AreEqual("111119990", ((FieldMultipleFilter)path.Filters[0]).Names.First());
            Assert.AreEqual("3", ((FieldMultipleFilter)path.Filters[0]).Names.ElementAt(1));
        }

        [TestMethod]
        public void SlicingIndexAll()
        {
            var path = new JPathParser("[111119990:3:2]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(111119990, ((ArraySliceFilter)path.Filters[0]).Start);
            Assert.AreEqual(3, ((ArraySliceFilter)path.Filters[0]).End);
            Assert.AreEqual(2, ((ArraySliceFilter)path.Filters[0]).Step);
        }

        [TestMethod]
        public void SlicingIndex()
        {
            var path = new JPathParser("[111119990:3]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(111119990, ((ArraySliceFilter)path.Filters[0]).Start);
            Assert.AreEqual(3, ((ArraySliceFilter)path.Filters[0]).End);
            Assert.AreEqual(null, ((ArraySliceFilter)path.Filters[0]).Step);
        }

        [TestMethod]
        public void SlicingIndexNegative()
        {
            var path = new JPathParser("[-111119990:-3:-2]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(-111119990, ((ArraySliceFilter)path.Filters[0]).Start);
            Assert.AreEqual(-3, ((ArraySliceFilter)path.Filters[0]).End);
            Assert.AreEqual(-2, ((ArraySliceFilter)path.Filters[0]).Step);
        }

        [TestMethod]
        public void SlicingIndexEmptyStop()
        {
            var path = new JPathParser("[  -3  :  ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(-3, ((ArraySliceFilter)path.Filters[0]).Start);
            Assert.AreEqual(null, ((ArraySliceFilter)path.Filters[0]).End);
            Assert.AreEqual(null, ((ArraySliceFilter)path.Filters[0]).Step);
        }

        [TestMethod]
        public void SlicingIndexEmptyStart()
        {
            var path = new JPathParser("[ : 1 : ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(null, ((ArraySliceFilter)path.Filters[0]).Start);
            Assert.AreEqual(1, ((ArraySliceFilter)path.Filters[0]).End);
            Assert.AreEqual(null, ((ArraySliceFilter)path.Filters[0]).Step);
        }

        [TestMethod]
        public void SlicingIndexWhitespace()
        {
            var path = new JPathParser("[  -111119990  :  -3  :  -2  ]");
            Assert.AreEqual(1, path.Filters.Count);
            Assert.AreEqual(-111119990, ((ArraySliceFilter)path.Filters[0]).Start);
            Assert.AreEqual(-3, ((ArraySliceFilter)path.Filters[0]).End);
            Assert.AreEqual(-2, ((ArraySliceFilter)path.Filters[0]).Step);
        }

        [TestMethod]
        public void EmptyIndexer()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("[]"); }, "Array index expected.");
        }

        [TestMethod]
        public void IndexerCloseInProperty()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("]"); }, "Unexpected character while parsing path: ]");
        }

        [TestMethod]
        public void AdjacentIndexers()
        {
            var path = new JPathParser("[1][0][0][" + int.MaxValue + "]");
            Assert.AreEqual(4, path.Filters.Count);
            Assert.AreEqual(1, ((ArrayIndexFilter)path.Filters[0]).Index);
            Assert.AreEqual(0, ((ArrayIndexFilter)path.Filters[1]).Index);
            Assert.AreEqual(0, ((ArrayIndexFilter)path.Filters[2]).Index);
            Assert.AreEqual(int.MaxValue, ((ArrayIndexFilter)path.Filters[3]).Index);
        }

        [TestMethod]
        public void MissingDotAfterIndexer()
        {
            Assert.ThrowsException<JsonException>(() => { new JPathParser("[1]Blah"); }, "Unexpected character following indexer: B");
        }

        [TestMethod]
        public void PropertyFollowingEscapedPropertyName()
        {
            var path = new JPathParser("frameworks.dnxcore50.dependencies.['System.Xml.ReaderWriter'].source");
            Assert.AreEqual(5, path.Filters.Count);

            Assert.AreEqual("frameworks", ((FieldFilter)path.Filters[0]).Name);
            Assert.AreEqual("dnxcore50", ((FieldFilter)path.Filters[1]).Name);
            Assert.AreEqual("dependencies", ((FieldFilter)path.Filters[2]).Name);
            Assert.AreEqual("System.Xml.ReaderWriter", ((FieldFilter)path.Filters[3]).Name);
            Assert.AreEqual("source", ((FieldFilter)path.Filters[4]).Name);
        }
    }
}
