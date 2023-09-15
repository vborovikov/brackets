namespace Brackets;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using JPath;
using Primitives;
using XPath;

public partial class Document
{
    private enum DataType
    {
        Undefined,
        Text,
        Number,
        Boolean,
        Query,
    }

    private sealed class DataElement : Element
    {
        public DataElement(DataType dataType, object value)
            : base(-1)
        {
            this.DataType = dataType;
            this.Value = value;
        }

        public DataType DataType { get; }

        public object Value { get; }

        public override bool TryGetValue<T>(out T value) => TryGetValue(this.Value, out value);

        public override string ToText()
        {
            return this.Value.ToString();
        }

        public override string ToString() => ToText();

        public override string ToDebugString() => ToText();

        public static DataElement From(int number) => new(DataType.Number, number);
        public static DataElement From(string text) => new(DataType.Text, text);
        public static DataElement From(bool flag) => new(DataType.Boolean, flag);
    }

    private abstract class PathQueryContext : IEnumerable<Element>
    {
        private sealed class PathQueryEmptyContext : PathQueryContext
        {
            public override Element GetSingleElement() => null;

            public override IEnumerator<Element> GetEnumerator() => Enumerable.Empty<Element>().GetEnumerator();
        }

        public static readonly PathQueryContext Empty = new PathQueryEmptyContext();

        protected PathQueryContext()
        {
        }

        public abstract Element GetSingleElement();

        public PathQuerySelectionContext.Selector GetSelector() => new(this);

        public abstract IEnumerator<Element> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class PathQueryElementContext : PathQueryContext
    {
        public PathQueryElementContext(Element element)
        {
            this.Element = element;
        }

        public Element Element { get; protected set; }

        public override Element GetSingleElement() => this.Element;

        public override IEnumerator<Element> GetEnumerator()
        {
            yield return this.Element;
        }
    }

    private class PathQueryCollectionContext : PathQueryContext
    {
        private readonly IEnumerable<Element> elements;

        public PathQueryCollectionContext(IEnumerable<Element> elements)
        {
            this.elements = elements;
        }

        public override Element GetSingleElement()
        {
            var element = default(Element);
            using var enumerator = this.elements.GetEnumerator();
            if (enumerator.MoveNext())
            {
                element = enumerator.Current;
            }
            if (enumerator.MoveNext())
            {
                return null;
            }

            return element;
        }

        public override IEnumerator<Element> GetEnumerator() => this.elements.GetEnumerator();
    }

    private class PathQuerySelectionContext : PathQueryElementContext
    {
        public struct Selector
        {
            private readonly PathQueryContext context;
            private readonly IEnumerator<Element> elementEnumerator;
            private int index;

            public Selector(PathQueryContext context)
            {
                this.context = context;
                this.elementEnumerator = this.context.GetEnumerator();
                this.index = -1;
                this.Current = new PathQuerySelectionContext(this.context, null, this.index);
            }

            public PathQuerySelectionContext Current { get; }

            public void Reset()
            {
                this.elementEnumerator.Reset();
                this.index = -1;
            }

            public bool MoveNext()
            {
                if (this.elementEnumerator.MoveNext())
                {
                    this.Current.Element = this.elementEnumerator.Current;
                    this.Current.Index = ++this.index;
                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                this.elementEnumerator.Dispose();
            }

            public Selector GetEnumerator() => this;

        }

        private PathQuerySelectionContext(PathQueryContext group, Element element, int index)
            : base(element)
        {
            this.Group = group;
            this.Index = index;
        }

        public PathQueryContext Group { get; }

        public int Index { get; private set; }
    }

    private abstract class PathQuery
    {
        public abstract DataType ResultType { get; }

        public IEnumerable<Element> Run(Document document)
        {
            return Run(new PathQueryElementContext(document.root));
        }

        protected internal PathQueryContext Run(PathQueryContext context)
        {
            return RunOverride(context);
        }

        protected internal Element RunScalar(PathQueryContext context)
        {
            var result = Run(context);
            return result.GetSingleElement();
        }

        protected internal bool TryEvaluate<TResult>(PathQueryContext context, out TResult result)
        {
            var resultElement = RunScalar(context);
            if (resultElement is null)
            {
                result = default;
                return false;
            }

            return resultElement.TryGetValue(out result);
        }

        protected abstract PathQueryContext RunOverride(PathQueryContext context);

        protected static IEnumerable<Element> AsChildren(Element element)
        {
            return element as IEnumerable<Element> ?? Nothing();
        }

        protected static IEnumerable<Element> AsSelf(Element element)
        {
            return Enumerable.Repeat(element, 1);
        }

        protected static IEnumerable<Element> Nothing() => Array.Empty<Element>();
    }

    private class PathConst : PathQuery
    {
        public PathConst(DataElement value)
        {
            this.Value = value;
            this.Result = new PathQueryElementContext(this.Value);
        }

        public DataElement Value { get; }

        public override DataType ResultType => this.Value.DataType;

        public PathQueryContext Result { get; }

        public override string ToString()
        {
            return this.ResultType == DataType.Text ? $"'{this.Value}'" : this.Value.ToString();
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            return this.Result;
        }
    }

    private class PathAxis : PathQuery
    {
        private readonly XPathAxis axis;
        private readonly XPathNodeType nodeType;
        private readonly string prefix;
        private readonly string name;
        private readonly bool skipNameTest;

        public PathAxis(XPathAxis axis, XPathNodeType nodeType, string prefix, string name)
        {
            this.axis = axis;
            this.nodeType = nodeType;
            this.prefix = prefix;
            this.name = name;
            this.skipNameTest = String.IsNullOrEmpty(this.name);
        }

        public override DataType ResultType => DataType.Query;

        public override string ToString()
        {
            return this.nodeType switch
            {
                XPathNodeType.Attribute => String.Concat("@", this.name),
                XPathNodeType.Text => "text()",
                XPathNodeType.Root => "/",
                XPathNodeType.All when this.axis == XPathAxis.DescendantOrSelf => "/",
                XPathNodeType.All when this.axis == XPathAxis.Parent => "..",
                XPathNodeType.All when this.axis == XPathAxis.Self => ".",
                _ when this.skipNameTest => $"{this.axis}::{this.nodeType}",
                _ => this.name,
            };
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            return new PathQueryCollectionContext(context
                .Select(Enumerate)
                .SelectMany(el => el)
                .Where(Filter));
        }

        private bool Filter(Element element)
        {
            switch (this.nodeType)
            {
                case XPathNodeType.Root:
                    return element is Root;

                case XPathNodeType.Element:
                    {
                        var tag = element as Tag;
                        if (tag is null)
                            return false;
                        if (this.skipNameTest)
                            return true;

                        return String.Equals(tag.Name, this.name, StringComparison.OrdinalIgnoreCase);
                    }

                case XPathNodeType.Attribute:
                    {
                        var attr = element as Attribute;
                        if (attr is null)
                            return false;
                        if (this.skipNameTest)
                            return true;

                        return String.Equals(attr.Name, this.name, StringComparison.OrdinalIgnoreCase);
                    }

                case XPathNodeType.Namespace:
                    return false;

                case XPathNodeType.Text:
                    return element is Content || element is Attribute || element is DataElement;

                case XPathNodeType.All:
                    return true;

                default:
                    return false;
            }
        }

        private IEnumerable<Element> Enumerate(Element element)
        {
            return this.axis switch
            {
                XPathAxis.Self => EnumerateSelf(element),
                XPathAxis.Parent => AsSelf(element.Parent),
                XPathAxis.Child => EnumerateChildren(element),
                XPathAxis.Ancestor => EnumerateAncestors(element, includeSelf: false),
                XPathAxis.AncestorOrSelf => EnumerateAncestors(element, includeSelf: true),
                XPathAxis.Descendant => EnumerateDescendants(element, includeSelf: false),
                XPathAxis.DescendantOrSelf => EnumerateDescendants(element, includeSelf: true),
                XPathAxis.Following => EnumerateFollowingElements(element),
                XPathAxis.FollowingSibling => EnumerateFollowingSiblings(element),
                XPathAxis.Preceding => EnumeratePrecedingElements(element),
                XPathAxis.PrecedingSibling => EnumeratePrecedingSiblings(element),
                XPathAxis.Attribute => EnumerateAttributes(element),
                XPathAxis.Namespace => EnumerateNamespaces(element),
                _ => Nothing(),
            };
        }

        private IEnumerable<Element> EnumerateChildren(Element element)
        {
            // to evaluate '@attr/text()' as the attribute value
            if (element is Attribute attr &&
                this.nodeType == XPathNodeType.Text &&
                attr.TryGetValue<string>(out var text))
            {
                return AsSelf(DataElement.From(text));
            }

            return AsChildren(element);
        }

        private IEnumerable<Element> EnumerateNamespaces(Element element)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Element> EnumerateAttributes(Element element)
        {
            if (element is Tag tag)
                return tag.Attributes;

            return Nothing();
        }

        private IEnumerable<Element> EnumerateSelf(Element element)
        {
            if (this.nodeType == XPathNodeType.Root)
            {
                // only Root has no parent
                while (element.Parent is not null)
                {
                    element = element.Parent;
                }
            }
            
            return AsSelf(element);
        }

        private IEnumerable<Element> EnumeratePrecedingElements(Element element)
        {
            // this element siblings
            foreach (var elementSibling in EnumeratePrecedingSiblings(element))
                yield return elementSibling;

            // and parent siblings and so on
            foreach (var ancestor in EnumerateAncestors(element, includeSelf: false))
                foreach (var ancestorSibling in EnumeratePrecedingSiblings(ancestor))
                    yield return ancestorSibling;
        }

        private IEnumerable<Element> EnumeratePrecedingSiblings(Element element)
        {
            var lastSibling = ((ParentTag)element.Parent).Child.Prev;

            while (element.Prev != lastSibling)
            {
                yield return element.Prev;
                element = element.Prev;
            }
        }

        private IEnumerable<Element> EnumerateFollowingElements(Element element)
        {
            // this element siblings
            foreach (var elementSibling in EnumerateFollowingSiblings(element))
                yield return elementSibling;

            // and parent siblings and so on
            foreach (var ancestor in EnumerateAncestors(element, includeSelf: false))
                foreach (var ancestorSibling in EnumerateFollowingSiblings(ancestor))
                    yield return ancestorSibling;
        }

        private IEnumerable<Element> EnumerateFollowingSiblings(Element element)
        {
            var firstSibling = ((ParentTag)element.Parent).Child;

            while (element.Next != firstSibling)
            {
                yield return element.Next;
                element = element.Next;
            }
        }

        private IEnumerable<Element> EnumerateDescendants(Element element, bool includeSelf)
        {
            if (includeSelf)
                yield return element;

            if (element is ParentTag parent)
                foreach (var descendant in parent.FindAll(_ => true))
                    yield return descendant;
        }

        private IEnumerable<Element> EnumerateAncestors(Element element, bool includeSelf)
        {
            if (includeSelf)
                yield return element;

            while (element.Parent != null)
            {
                yield return element.Parent;
                element = element.Parent;
            }
        }
    }

    private class PathJoin : PathQuery
    {
        public PathJoin(PathQuery left, PathQuery right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override DataType ResultType => DataType.Query;

        public PathQuery Left { get; }
        public PathQuery Right { get; }

        public override string ToString()
        {
            return $"{this.Left}/{this.Right}";
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            var result = this.Right.Run(this.Left.Run(context));
            return result;
        }
    }

    private class PathUnion : PathQuery
    {
        public PathUnion(PathQuery left, PathQuery right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override DataType ResultType => DataType.Query;

        public PathQuery Left { get; }
        public PathQuery Right { get; }

        public override string ToString()
        {
            return $"{this.Left}|{this.Right}";
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            var leftElements = this.Left.Run(context);
            var rightElements = this.Right.Run(context);

            //todo: use ElementEqualityComparer
            return new PathQueryCollectionContext(leftElements.Union(rightElements));
        }
    }

    private class PathPredicate : PathQuery
    {
        public PathPredicate(PathQuery query, PathQuery condition)
        {
            this.Query = query;
            this.Condition = condition;
        }

        public override DataType ResultType => DataType.Query;

        public PathQuery Query { get; }
        public PathQuery Condition { get; }

        public override string ToString()
        {
            return $"{this.Query}[{this.Condition}]";
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            var elements = this.Query.Run(context);
            return new PathQueryCollectionContext(this.Condition.ResultType switch
            {
                DataType.Query => EvaluateQuery(elements),
                DataType.Boolean => EvaluateBoolean(elements),
                DataType.Number => EvaluateNumber(elements),
                DataType.Text => EvaluateText(elements),
                _ => Nothing()
            });
        }

        private IEnumerable<Element> EvaluateQuery(PathQueryContext elements)
        {
            foreach (var selectedContext in elements.GetSelector())
            {
                var conditionEvaluation = this.Condition.Run(selectedContext);
                if (conditionEvaluation.Any())
                    yield return selectedContext.Element;
            }
        }

        private IEnumerable<Element> EvaluateBoolean(PathQueryContext elements)
        {
            foreach (var selectedContext in elements.GetSelector())
            {
                if (this.Condition.TryEvaluate<bool>(selectedContext, out var test) && test)
                {
                    yield return selectedContext.Element;
                }
            }
        }

        private IEnumerable<Element> EvaluateNumber(PathQueryContext elements)
        {
            if (this.Condition.TryEvaluate<int>(elements, out var number))
            {
                var element = elements.ElementAtOrDefault(number - 1);
                if (element is not null)
                    yield return element;
            }
        }

        private IEnumerable<Element> EvaluateText(PathQueryContext elements)
        {
            foreach (var selectedContext in elements.GetSelector())
            {
                if (this.Condition.TryEvaluate<string>(selectedContext, out var text) &&
                    selectedContext.Element.Contains(text))
                {
                    yield return selectedContext.Element;
                }
            }
        }
    }

    private class PathOperator : PathQuery
    {
        public PathOperator(XPathOperator @operator, PathQuery left, PathQuery right)
        {
            this.Operator = @operator;
            this.Left = left;
            this.Right = right;
        }

        public override DataType ResultType => this.Operator switch
        {
            XPathOperator.Or or
            XPathOperator.And or
            XPathOperator.Eq or
            XPathOperator.Ne or
            XPathOperator.Lt or
            XPathOperator.Le or
            XPathOperator.Gt or
            XPathOperator.Ge => DataType.Boolean,
            XPathOperator.Plus =>
                this.Left.ResultType == DataType.Text && this.Right.ResultType == DataType.Text ? DataType.Text : DataType.Number,
            XPathOperator.Minus or
            XPathOperator.Multiply or
            XPathOperator.Divide or
            XPathOperator.Modulo or
            XPathOperator.UnaryMinus => DataType.Number,
            _ => DataType.Text
        };

        public XPathOperator Operator { get; }
        public PathQuery Left { get; }
        public PathQuery Right { get; }

        public override string ToString()
        {
            var opStr = this.Operator switch
            {
                XPathOperator.Or => " or ",
                XPathOperator.And => " and ",
                XPathOperator.Eq => "=",
                XPathOperator.Ne => "!=",
                XPathOperator.Lt => "<",
                XPathOperator.Le => "<=",
                XPathOperator.Gt => ">",
                XPathOperator.Ge => ">=",
                XPathOperator.Plus => "+",
                XPathOperator.Minus => "-",
                XPathOperator.Multiply => "*",
                XPathOperator.Divide => "/",
                XPathOperator.Modulo => "%",
                XPathOperator.UnaryMinus => "-",
                _ => "~",
            };

            return String.Concat(this.Left, opStr, this.Right);
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            // determine the type of operands
            // evaluate the operator applicability
            // evaluate operands
            // apply the operator

            var left = this.Left.RunScalar(context);
            var right = this.Right.RunScalar(context);

            if (left is not null && right is not null)
            {
                if (this.Operator == XPathOperator.Or || this.Operator == XPathOperator.And)
                {
                    if (left.TryGetValue<bool>(out var leftBool) &&
                        right.TryGetValue<bool>(out var rightBool))
                    {
                        return new PathQueryElementContext(DataElement.From(
                            this.Operator switch
                            {
                                XPathOperator.Or => leftBool || rightBool,
                                XPathOperator.And => leftBool && rightBool,
                                _ => false,
                            }));
                    }
                }

                if (left.TryGetValue<int>(out var leftNumber) &&
                    right.TryGetValue<int>(out var rightNumber))
                {
                    return ApplyOperator(leftNumber, rightNumber);
                }

                if (left.TryGetValue<string>(out var leftString) &&
                    right.TryGetValue<string>(out var rightString))
                {
                    return ApplyOperator(leftString, rightString);
                }
            }

            return PathQueryContext.Empty;
        }

        private PathQueryContext ApplyOperator(string leftString, string rightString)
        {
            (DataType Type, object Value) result = this.Operator switch
            {
                XPathOperator.Eq => (DataType.Boolean, String.Compare(leftString, rightString, ignoreCase: true) == 0),
                XPathOperator.Ne => (DataType.Boolean, String.Compare(leftString, rightString, ignoreCase: true) != 0),
                XPathOperator.Lt => (DataType.Boolean, String.Compare(leftString, rightString, ignoreCase: true) < 0),
                XPathOperator.Le => (DataType.Boolean, String.Compare(leftString, rightString, ignoreCase: true) <= 0),
                XPathOperator.Gt => (DataType.Boolean, String.Compare(leftString, rightString, ignoreCase: true) > 0),
                XPathOperator.Ge => (DataType.Boolean, String.Compare(leftString, rightString, ignoreCase: true) >= 0),
                XPathOperator.Plus => (DataType.Text, leftString + rightString),
                _ => (DataType.Text, String.Empty),
            };

            return new PathQueryElementContext(new DataElement(result.Type, result.Value));
        }

        private PathQueryContext ApplyOperator(int leftNumber, int rightNumber)
        {
            (DataType Type, object Value) result = this.Operator switch
            {
                XPathOperator.Eq => (DataType.Boolean, leftNumber == rightNumber),
                XPathOperator.Ne => (DataType.Boolean, leftNumber != rightNumber),
                XPathOperator.Lt => (DataType.Boolean, leftNumber < rightNumber),
                XPathOperator.Le => (DataType.Boolean, leftNumber <= rightNumber),
                XPathOperator.Gt => (DataType.Boolean, leftNumber > rightNumber),
                XPathOperator.Ge => (DataType.Boolean, leftNumber >= rightNumber),
                XPathOperator.Plus => (DataType.Number, leftNumber + rightNumber),
                XPathOperator.Minus => (DataType.Number, leftNumber - rightNumber),
                XPathOperator.Multiply => (DataType.Number, leftNumber * rightNumber),
                XPathOperator.Divide => (DataType.Number, leftNumber / rightNumber),
                XPathOperator.Modulo => (DataType.Number, leftNumber % rightNumber),
                XPathOperator.UnaryMinus => (DataType.Number, -leftNumber),
                _ => (DataType.Number, 0),
            };

            return new PathQueryElementContext(new DataElement(result.Type, result.Value));
        }
    }

    private class PathFunction : PathQuery
    {
        private static readonly Dictionary<string, Func<PathQueryContext, IEnumerable<PathQuery>, PathQueryContext>> knownQueryFunctions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            };

        private static readonly Dictionary<string, Func<PathQueryContext, IEnumerable<PathQuery>, int>> knownNumberFunctions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(XPathFunctions.Count), XPathFunctions.Count },
                { nameof(XPathFunctions.Last), XPathFunctions.Last },
                { nameof(XPathFunctions.Position), XPathFunctions.Position },
            };

        private static readonly Dictionary<string, Func<PathQueryContext, IEnumerable<PathQuery>, bool>> knownBooleanFunctions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(XPathFunctions.Contains), XPathFunctions.Contains },
                { nameof(XPathFunctions.IsNumber), XPathFunctions.IsNumber },
            };

        private static readonly Dictionary<string, Func<PathQueryContext, IEnumerable<PathQuery>, string>> knownStringFunctions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(XPathFunctions.Name), XPathFunctions.Name },
                { nameof(XPathFunctions.Trim), XPathFunctions.Trim },
                { nameof(XPathFunctions.NormalizeSpace), XPathFunctions.NormalizeSpace },
                { "normalize-space", XPathFunctions.NormalizeSpace },
                { "string", XPathFunctions.Text },
                { nameof(XPathFunctions.Unescape), XPathFunctions.Unescape },
                { nameof(XPathFunctions.Json), XPathFunctions.Json },
                { nameof(XPathFunctions.Replace), XPathFunctions.Replace },
            };

        public PathFunction(string prefix, string name, IEnumerable<PathQuery> arguments)
        {
            this.Prefix = prefix;
            this.Name = name;
            this.Arguments = arguments;
        }

        public override DataType ResultType =>
            knownNumberFunctions.ContainsKey(this.Name) ? DataType.Number :
            knownBooleanFunctions.ContainsKey(this.Name) ? DataType.Boolean :
            knownQueryFunctions.ContainsKey(this.Name) ? DataType.Query :
            knownStringFunctions.ContainsKey(this.Name) ? DataType.Text :
            DataType.Undefined;

        public string Prefix { get; }
        public string Name { get; }
        public IEnumerable<PathQuery> Arguments { get; }

        public override string ToString()
        {
            return $"{this.Name}({String.Join(',', this.Arguments)})";
        }

        protected override PathQueryContext RunOverride(PathQueryContext context)
        {
            // https://www.javatpoint.com/xpath-operators

            if (TryRunNumberFunction(context, out var number))
                return new PathQueryElementContext(DataElement.From(number));
            if (TryRunBooleanFunction(context, out var flag))
                return new PathQueryElementContext(DataElement.From(flag));
            if (TryRunStringFunction(context, out var text))
                return new PathQueryElementContext(DataElement.From(text));
            if (TryRunQueryFunction(context, out var elements))
                return elements;

            return PathQueryContext.Empty;
        }

        private bool TryRunStringFunction(PathQueryContext context, out string text)
        {
            if (knownStringFunctions.TryGetValue(this.Name, out var func))
            {
                text = func(context, this.Arguments);
                return true;
            }

            text = default;
            return false;
        }

        private bool TryRunQueryFunction(PathQueryContext context, out PathQueryContext elements)
        {
            if (knownQueryFunctions.TryGetValue(this.Name, out var func))
            {
                elements = func(context, this.Arguments);
                return true;
            }

            elements = PathQueryContext.Empty;
            return false;
        }

        private bool TryRunBooleanFunction(PathQueryContext context, out bool flag)
        {
            if (knownBooleanFunctions.TryGetValue(this.Name, out var func))
            {
                flag = func(context, this.Arguments);
                return true;
            }

            flag = default;
            return false;
        }

        private bool TryRunNumberFunction(PathQueryContext context, out int number)
        {
            if (knownNumberFunctions.TryGetValue(this.Name, out var func))
            {
                number = func(context, this.Arguments);
                return true;
            }

            number = default;
            return false;
        }
    }

    private static class XPathFunctions
    {
        public static string Name(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var ctx = args.FirstOrDefault()?.Run(context) ?? context;
            var element = ctx.GetSingleElement();
            return element switch
            {
                Tag tag => tag.Name,
                Attribute attr => attr.Name,
                _ => String.Empty,
            };
        }

        public static int Count(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var query = args.FirstOrDefault();
            if (query is null)
                return 0;

            var elements = query.Run(context);
            return elements.Count();
        }

        public static int Last(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            if (context.GetSingleElement() is IEnumerable<Element> collection)
            {
                return collection.Count();
            }

            return context.Count();
        }

        public static int Position(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            if (context is PathQuerySelectionContext selectionContext)
                return selectionContext.Index + 1;

            return -1;
        }

        /// <summary>
        /// contains(string1, string2)
        /// </summary>
        /// <param name="context">The XPath query current context.</param>
        /// <param name="args">The function arguments.</param>
        /// <returns><c>true</c> when the first string contains the second string, <c>false</c> otherwise.</returns>
        public static bool Contains(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var query = args.ElementAtOrDefault(0);
            var textArg = args.ElementAtOrDefault(1);
            if (query is null || textArg is null)
                return false;

            if (textArg.TryEvaluate<string>(context, out var text))
            {
                var elements = query.Run(context);
                if (elements.Any())
                {
                    var singleElement = elements.GetSingleElement();
                    if (singleElement is not null && singleElement.TryGetValue<string>(out var content))
                        return content.Contains(text, StringComparison.CurrentCultureIgnoreCase);

                    foreach (var element in elements)
                    {
                        if (!element.Contains(text))
                            return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// trim(string)
        /// </summary>
        /// <param name="context">The XPath query current context.</param>
        /// <param name="args">The function arguments.</param>
        /// <returns>trims the leading and trailing space from the string.</returns>
        public static string Trim(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var text = Text(context, args);
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;

            return WebUtility.HtmlDecode(text).Trim();
        }


        /// <summary>
        /// normalize-space(string)
        /// </summary>
        /// <param name="context">The XPath query current context.</param>
        /// <param name="args">The function arguments.</param>
        /// <returns>trims the leading and trailing space from the string.</returns>
        public static string NormalizeSpace(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var text = Text(context, args);
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;

            return
                // replace html entities
                WebUtility.HtmlDecode(text)
                // normalize multiple whitespaces
                .NormalizeWhiteSpace(" \t\xA0")
                // remove single whitespace lines
                .Replace(" \r\n", String.Empty, StringComparison.Ordinal)
                .Replace(" \n", String.Empty, StringComparison.Ordinal)
                // normalize multiple empty lines
                .NormalizeWhiteSpace("\r\n", Environment.NewLine)
                // remove all leading and trailing whitespace
                .Trim();
        }

        /// <summary>
        /// string([object])
        /// The <c>string</c> function converts the given argument to a string.
        /// https://developer.mozilla.org/en-US/docs/Web/XPath/Functions/string
        /// </summary>
        /// <param name="context">The XPath query current context.</param>
        /// <param name="args">The function arguments.</param>
        /// <returns>A string.</returns>
        public static string Text(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var elements = args.FirstOrDefault()?.Run(context) ?? context;
            return Element.ToText(elements);
        }

        public static string Unescape(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var text = Text(context, args);
            return Regex.Unescape(text);
        }

        public static string Json(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var pathElement = (args.ElementAtOrDefault(1)?.Run(context) ?? args.FirstOrDefault()?.Run(context))?.GetSingleElement();
            if (pathElement is not null && pathElement.TryGetValue<string>(out var path))
            {
                try
                {
                    var text = Text(context, args);
                    var json = JsonDocument.Parse(text);
                    var token = json.SelectToken(path);

                    return token?.GetString();
                }
                catch (Exception ex)
                {
                    //System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return String.Empty;
        }

        public static bool IsNumber(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var element = (args.FirstOrDefault()?.Run(context) ?? context).GetSingleElement();
            return element is not null && element.TryGetValue<decimal>(out _);
        }

        public static string Replace(PathQueryContext context, IEnumerable<PathQuery> args)
        {
            var text = args.ElementAtOrDefault(0);
            var what = args.ElementAtOrDefault(1);
            var that = args.ElementAtOrDefault(2);

            if (text is null || what is null || that is null)
                return String.Empty;

            if (what.TryEvaluate<string>(context, out var whatStr) && that.TryEvaluate<string>(context, out var thatStr))
            {
                var textElement = text.Run(context).GetSingleElement();
                if (textElement is not null && textElement.TryGetValue(out string textStr))
                {
                    return textStr.Replace(whatStr, thatStr, StringComparison.CurrentCultureIgnoreCase);
                }
            }

            return String.Empty;
        }
    }

    private sealed class PathQueryBuilder : IXPathBuilder<PathQuery>
    {
        public static readonly PathQueryBuilder Instance = new();

        private PathQueryBuilder()
        {
        }

        public void Begin()
        {
        }

        public PathQuery End(PathQuery result)
        {
            return result;
        }

        public PathQuery Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string prefix, string name)
        {
            return new PathAxis(xpathAxis, nodeType, prefix, name);
        }

        public PathQuery Function(string prefix, string name, IEnumerable<PathQuery> args)
        {
            return new PathFunction(prefix, name, args);
        }

        public PathQuery Join(PathQuery left, PathQuery right)
        {
            return new PathJoin(left, right);
        }

        public PathQuery Union(PathQuery left, PathQuery right)
        {
            return new PathUnion(left, right);
        }

        public PathQuery Number(string value)
        {
            return new PathConst(DataElement.From(Int32.Parse(value)));
        }

        public PathQuery Operator(XPathOperator op, PathQuery left, PathQuery right)
        {
            return new PathOperator(op, left, right);
        }

        public PathQuery Predicate(PathQuery node, PathQuery condition, bool reverseStep)
        {
            return new PathPredicate(node, condition);
        }

        public PathQuery Text(string value)
        {
            return new PathConst(DataElement.From(value));
        }

        public PathQuery Variable(string prefix, string name)
        {
            throw new NotSupportedException();
        }
    }
}
