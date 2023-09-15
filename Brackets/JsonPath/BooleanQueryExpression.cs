namespace Brackets.JPath
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    internal class BooleanQueryExpression : QueryExpression
    {
        private static readonly JsonValueKind[] valueKindSortOrder = new[]
        {
            JsonValueKind.Undefined,
            JsonValueKind.Null,
            JsonValueKind.Number,
            JsonValueKind.String,
            JsonValueKind.Object,
            JsonValueKind.Array,
            JsonValueKind.False,
            JsonValueKind.True
        };

        private readonly object left;
        private readonly object right;

        public BooleanQueryExpression(QueryOperator @operator, object left, object right) : base(@operator)
        {
            this.left = left;
            this.right = right;
        }

        public object Left => this.left;

        public object Right => this.right;

        private static IEnumerable<JsonElement> GetResult(JsonElement root, JsonElement t, object o)
        {
            if (o is JsonElement resultToken)
            {
                return new[] { resultToken };
            }

            if (o is List<PathFilter> pathFilters)
            {
                return JPathParser.Evaluate(pathFilters, root, t, false);
            }

            return Array.Empty<JsonElement>();
        }

        public override bool IsMatch(JsonElement root, JsonElement t)
        {
            if (this.Operator == QueryOperator.Exists)
            {
                return GetResult(root, t, this.left).Any();
            }

            using (var leftResults = GetResult(root, t, this.left).GetEnumerator())
            {
                if (leftResults.MoveNext())
                {
                    var rightResultsEn = GetResult(root, t, this.right);
                    var rightResults = rightResultsEn as ICollection<JsonElement> ?? rightResultsEn.ToList();

                    do
                    {
                        var leftResult = leftResults.Current;
                        foreach (var rightResult in rightResults)
                        {
                            if (MatchTokens(leftResult, rightResult))
                            {
                                return true;
                            }
                        }
                    } while (leftResults.MoveNext());
                }
            }

            return false;
        }

        private static bool TryGetNumberValue(JsonElement value, out double num)
        {
            if (value.ValueKind == JsonValueKind.Number)
            {
                num = value.GetDouble();
                return true;
            }
            if (value.ValueKind == JsonValueKind.String &&
                Double.TryParse(value.GetString(), out num))
            {
                return true;
            }
            num = default;
            return false;
        }

        private static int Compare(JsonElement leftValue, JsonElement rightValue)
        {
            if (leftValue.ValueKind == rightValue.ValueKind)
            {
                switch (leftValue.ValueKind)
                {
                    case JsonValueKind.False:
                    case JsonValueKind.True:
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return 0;
                    case JsonValueKind.String:
                        return leftValue.GetString().CompareTo(rightValue.GetString());
                    case JsonValueKind.Number:
                        return leftValue.GetDouble().CompareTo(rightValue.GetDouble());
                    default:
                        throw new InvalidOperationException($"Unknown json value kind: {leftValue.ValueKind}");
                }
            }

            // num/string comparison
            if (TryGetNumberValue(leftValue, out var leftNum) &&
                TryGetNumberValue(rightValue, out var rightNum))
            {
                return leftNum.CompareTo(rightNum);
            }

            return Array.IndexOf(valueKindSortOrder, leftValue.ValueKind).CompareTo(
                Array.IndexOf(valueKindSortOrder, rightValue.ValueKind));
        }

        private static bool IsArrayOrObject(JsonElement v) =>
            v.ValueKind == JsonValueKind.Array || v.ValueKind == JsonValueKind.Object;

        private bool MatchTokens(JsonElement leftValue, JsonElement rightValue)
        {
            if (!IsArrayOrObject(leftValue) && !IsArrayOrObject(rightValue))
            {
                switch (this.Operator)
                {
                    case QueryOperator.RegexEquals:
                        if (RegexEquals(leftValue, rightValue))
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.Equals:
                        if (EqualsWithStringCoercion(leftValue, rightValue))
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.StrictEquals:
                        if (EqualsWithStrictMatch(leftValue, rightValue))
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.NotEquals:
                        if (!EqualsWithStringCoercion(leftValue, rightValue))
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.StrictNotEquals:
                        if (!EqualsWithStrictMatch(leftValue, rightValue))
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.GreaterThan:
                        if (Compare(leftValue, rightValue) > 0)
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.GreaterThanOrEquals:
                        if (Compare(leftValue, rightValue) >= 0)
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.LessThan:
                        if (Compare(leftValue, rightValue) < 0)
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.LessThanOrEquals:
                        if (Compare(leftValue, rightValue) <= 0)
                        {
                            return true;
                        }
                        break;
                    case QueryOperator.Exists:
                        return true;
                }
            }
            else
            {
                switch (this.Operator)
                {
                    case QueryOperator.Exists:
                    // you can only specify primitive types in a comparison
                    // notequals will always be true
                    case QueryOperator.NotEquals:
                        return true;
                }
            }

            return false;
        }

        private static bool RegexEquals(JsonElement input, JsonElement pattern)
        {
            if (input.ValueKind != JsonValueKind.String || pattern.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var regexText = pattern.GetString();
            var patternOptionDelimiterIndex = regexText.LastIndexOf('/');

            var patternText = regexText.Substring(1, patternOptionDelimiterIndex - 1);
            var optionsText = regexText.Substring(patternOptionDelimiterIndex + 1);

            return Regex.IsMatch(input.GetString(), patternText, GetRegexOptions(optionsText));
        }

        private static bool EqualsWithStringCoercion(JsonElement value, JsonElement queryValue)
        {
            if (value.Equals(queryValue))
            {
                return true;
            }

            // Handle comparing an integer with a float
            // e.g. Comparing 1 and 1.0
            if (value.ValueKind == JsonValueKind.Number && queryValue.ValueKind == JsonValueKind.Number)
            {
                return value.GetDouble() == queryValue.GetDouble();
            }

            if (queryValue.ValueKind != JsonValueKind.String)
            {
                return false;
            }
            
            return string.Equals(value.ToString(), queryValue.GetString(), StringComparison.Ordinal);
        }

        private static bool EqualsWithStrictMatch(JsonElement value, JsonElement queryValue)
        {
            if (value.ValueKind != queryValue.ValueKind)
            {
                return false;
            }

            // Handle comparing an integer with a float
            // e.g. Comparing 1 and 1.0
            return Compare(value, queryValue) == 0;
        }

        private static RegexOptions GetRegexOptions(string optionsText)
        {
            var options = RegexOptions.None;

            for (var i = 0; i < optionsText.Length; i++)
            {
                switch (optionsText[i])
                {
                    case 'i':
                        options |= RegexOptions.IgnoreCase;
                        break;
                    case 'm':
                        options |= RegexOptions.Multiline;
                        break;
                    case 's':
                        options |= RegexOptions.Singleline;
                        break;
                    case 'x':
                        options |= RegexOptions.ExplicitCapture;
                        break;
                }
            }

            return options;
        }
    }
}
