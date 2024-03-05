#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

namespace Brackets.JPath;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

// https://www.rfc-editor.org/rfc/rfc9535.html

internal class JPathParser
{
    private static readonly char[] FloatCharacters = new[] { '.', 'E', 'e' };

    private readonly string expression;
    private readonly List<PathFilter> filters;
    private int currentIndex;

    public JPathParser(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        this.expression = expression;
        this.filters = new List<PathFilter>();

        ParseMain();
    }

    public IReadOnlyList<PathFilter> Filters => this.filters;

    private void ParseMain()
    {
        var currentPartStartIndex = this.currentIndex;

        EatWhitespace();

        if (this.expression.Length == this.currentIndex)
        {
            return;
        }

        if (this.expression[this.currentIndex] == '$')
        {
            if (this.expression.Length == 1)
            {
                return;
            }

            // only increment position for "$." or "$["
            // otherwise assume property that starts with $
            var c = this.expression[this.currentIndex + 1];
            if (c == '.' || c == '[')
            {
                this.currentIndex++;
                currentPartStartIndex = this.currentIndex;
            }
        }

        if (!ParsePath(this.filters, currentPartStartIndex, false))
        {
            var lastCharacterIndex = this.currentIndex;

            EatWhitespace();

            if (this.currentIndex < this.expression.Length)
            {
                throw new JsonException("Unexpected character while parsing path: " + this.expression[lastCharacterIndex]);
            }
        }
    }

    private bool ParsePath(List<PathFilter> filters, int currentPartStartIndex, bool query)
    {
        var scan = false;
        var followingIndexer = false;
        var followingDot = false;

        var ended = false;
        while (this.currentIndex < this.expression.Length && !ended)
        {
            var currentChar = this.expression[this.currentIndex];

            switch (currentChar)
            {
                case '[':
                case '(':
                    if (this.currentIndex > currentPartStartIndex)
                    {
                        var member = this.expression.Substring(currentPartStartIndex, this.currentIndex - currentPartStartIndex);
                        if (member == "*")
                        {
                            member = null;
                        }

                        filters.Add(CreatePathFilter(member, scan));
                        scan = false;
                    }

                    filters.Add(ParseIndexer(currentChar, scan));
                    scan = false;

                    this.currentIndex++;
                    currentPartStartIndex = this.currentIndex;
                    followingIndexer = true;
                    followingDot = false;
                    break;
                case ']':
                case ')':
                    ended = true;
                    break;
                case ' ':
                    if (this.currentIndex < this.expression.Length)
                    {
                        ended = true;
                    }
                    break;
                case '.':
                    if (this.currentIndex > currentPartStartIndex)
                    {
                        var member = this.expression.Substring(currentPartStartIndex, this.currentIndex - currentPartStartIndex);
                        if (member == "*")
                        {
                            member = null;
                        }

                        filters.Add(CreatePathFilter(member, scan));
                        scan = false;
                    }
                    if (this.currentIndex + 1 < this.expression.Length && this.expression[this.currentIndex + 1] == '.')
                    {
                        scan = true;
                        this.currentIndex++;
                    }
                    this.currentIndex++;
                    currentPartStartIndex = this.currentIndex;
                    followingIndexer = false;
                    followingDot = true;
                    break;
                default:
                    if (query && (currentChar == '=' || currentChar == '<' || currentChar == '!' || currentChar == '>' || currentChar == '|' || currentChar == '&'))
                    {
                        ended = true;
                    }
                    else
                    {
                        if (followingIndexer)
                        {
                            throw new JsonException("Unexpected character following indexer: " + currentChar);
                        }

                        this.currentIndex++;
                    }
                    break;
            }
        }

        var atPathEnd = (this.currentIndex == this.expression.Length);

        if (this.currentIndex > currentPartStartIndex)
        {
            var member = this.expression.Substring(currentPartStartIndex, this.currentIndex - currentPartStartIndex).TrimEnd();
            if (member == "*")
            {
                member = null;
            }
            filters.Add(CreatePathFilter(member, scan));
        }
        else
        {
            // no field name following dot in path and at end of base path/query
            if (followingDot && (atPathEnd || query))
            {
                throw new JsonException("Unexpected end while parsing path.");
            }
        }

        return atPathEnd;
    }

    private static PathFilter CreatePathFilter(string member, bool scan)
    {
        var filter = (scan) ? (PathFilter)new ScanFilter(member) : new FieldFilter(member);
        return filter;
    }

    private PathFilter ParseIndexer(char indexerOpenChar, bool scan)
    {
        this.currentIndex++;

        var indexerCloseChar = (indexerOpenChar == '[') ? ']' : ')';

        EnsureLength("Path ended with open indexer.");

        EatWhitespace();

        if (this.expression[this.currentIndex] == '\'')
        {
            return ParseQuotedField(indexerCloseChar, scan);
        }
        else if (this.expression[this.currentIndex] == '?')
        {
            return ParseQuery(indexerCloseChar, scan);
        }
        else
        {
            return ParseArrayIndexer(indexerCloseChar);
        }
    }

    private PathFilter ParseArrayIndexer(char indexerCloseChar)
    {
        var start = this.currentIndex;
        int? end = null;
        List<int> indexes = null;
        var colonCount = 0;
        int? startIndex = null;
        int? endIndex = null;
        int? step = null;

        while (this.currentIndex < this.expression.Length)
        {
            var currentCharacter = this.expression[this.currentIndex];

            if (currentCharacter == ' ')
            {
                end = this.currentIndex;
                EatWhitespace();
                continue;
            }

            if (currentCharacter == indexerCloseChar)
            {
                var length = (end ?? this.currentIndex) - start;

                if (indexes != null)
                {
                    if (length == 0)
                    {
                        throw new JsonException("Array index expected.");
                    }

                    var indexer = this.expression.Substring(start, length);
                    var index = Convert.ToInt32(indexer, CultureInfo.InvariantCulture);

                    indexes.Add(index);
                    return new ArrayMultipleIndexFilter(indexes);
                }
                else if (colonCount > 0)
                {
                    if (length > 0)
                    {
                        var indexer = this.expression.Substring(start, length);
                        var index = Convert.ToInt32(indexer, CultureInfo.InvariantCulture);

                        if (colonCount == 1)
                        {
                            endIndex = index;
                        }
                        else
                        {
                            step = index;
                        }
                    }

                    return new ArraySliceFilter { Start = startIndex, End = endIndex, Step = step };
                }
                else
                {
                    if (length == 0)
                    {
                        throw new JsonException("Array index expected.");
                    }

                    var indexer = this.expression.Substring(start, length);
                    var index = Convert.ToInt32(indexer, CultureInfo.InvariantCulture);

                    return new ArrayIndexFilter { Index = index };
                }
            }
            else if (currentCharacter == ',')
            {
                var length = (end ?? this.currentIndex) - start;

                if (length == 0)
                {
                    throw new JsonException("Array index expected.");
                }

                if (indexes == null)
                {
                    indexes = new List<int>();
                }

                var indexer = this.expression.Substring(start, length);
                indexes.Add(Convert.ToInt32(indexer, CultureInfo.InvariantCulture));

                this.currentIndex++;

                EatWhitespace();

                start = this.currentIndex;
                end = null;
            }
            else if (currentCharacter == '*')
            {
                this.currentIndex++;
                EnsureLength("Path ended with open indexer.");
                EatWhitespace();

                if (this.expression[this.currentIndex] != indexerCloseChar)
                {
                    throw new JsonException("Unexpected character while parsing path indexer: " + currentCharacter);
                }

                return new ArrayIndexFilter();
            }
            else if (currentCharacter == ':')
            {
                var length = (end ?? this.currentIndex) - start;

                if (length > 0)
                {
                    var indexer = this.expression.Substring(start, length);
                    var index = Convert.ToInt32(indexer, CultureInfo.InvariantCulture);

                    if (colonCount == 0)
                    {
                        startIndex = index;
                    }
                    else if (colonCount == 1)
                    {
                        endIndex = index;
                    }
                    else
                    {
                        step = index;
                    }
                }

                colonCount++;

                this.currentIndex++;

                EatWhitespace();

                start = this.currentIndex;
                end = null;
            }
            else if (!char.IsDigit(currentCharacter) && currentCharacter != '-')
            {
                throw new JsonException("Unexpected character while parsing path indexer: " + currentCharacter);
            }
            else
            {
                if (end != null)
                {
                    throw new JsonException("Unexpected character while parsing path indexer: " + currentCharacter);
                }

                this.currentIndex++;
            }
        }

        throw new JsonException("Path ended with open indexer.");
    }

    private void EatWhitespace()
    {
        while (this.currentIndex < this.expression.Length)
        {
            if (this.expression[this.currentIndex] != ' ')
            {
                break;
            }

            this.currentIndex++;
        }
    }

    private PathFilter ParseQuery(char indexerCloseChar, bool scan)
    {
        this.currentIndex++;
        EnsureLength("Path ended with open indexer.");

        if (this.expression[this.currentIndex] != '(')
        {
            throw new JsonException("Unexpected character while parsing path indexer: " + this.expression[this.currentIndex]);
        }

        this.currentIndex++;

        var expression = ParseExpression();

        this.currentIndex++;
        EnsureLength("Path ended with open indexer.");
        EatWhitespace();

        if (this.expression[this.currentIndex] != indexerCloseChar)
        {
            throw new JsonException("Unexpected character while parsing path indexer: " + this.expression[this.currentIndex]);
        }

        if (!scan)
        {
            return new QueryFilter(expression);
        }
        else
        {
            return new QueryScanFilter(expression);
        }
    }

    private bool TryParseExpression(out List<PathFilter> expressionPath)
    {
        if (this.expression[this.currentIndex] == '$')
        {
            expressionPath = new List<PathFilter> { RootFilter.Instance };
        }
        else if (this.expression[this.currentIndex] == '@')
        {
            expressionPath = new List<PathFilter>();
        }
        else
        {
            expressionPath = null;
            return false;
        }

        this.currentIndex++;

        if (ParsePath(expressionPath!, this.currentIndex, true))
        {
            throw new JsonException("Path ended with open query.");
        }

        return true;
    }

    private JsonException CreateUnexpectedCharacterException()
    {
        return new JsonException("Unexpected character while parsing path query: " + this.expression[this.currentIndex]);
    }

    // ick
    private JsonElement CreateValue(object value)
    {
        var doc = value == null ? "null" : JsonSerializer.Serialize(value);
        return JsonDocument.Parse(doc).RootElement;
    }

    private object ParseSide()
    {
        EatWhitespace();

        if (TryParseExpression(out var expressionPath))
        {
            EatWhitespace();
            EnsureLength("Path ended with open query.");

            return expressionPath!;
        }

        if (TryParseValue(out var value))
        {
            EatWhitespace();
            EnsureLength("Path ended with open query.");

            return CreateValue(value);
        }

        throw CreateUnexpectedCharacterException();
    }

    private QueryExpression ParseExpression()
    {
        QueryExpression rootExpression = null;
        CompositeExpression parentExpression = null;

        while (this.currentIndex < this.expression.Length)
        {
            var left = ParseSide();
            object right = null;

            QueryOperator op;
            if (this.expression[this.currentIndex] == ')'
                || this.expression[this.currentIndex] == '|'
                || this.expression[this.currentIndex] == '&')
            {
                op = QueryOperator.Exists;
            }
            else
            {
                op = ParseOperator();

                right = ParseSide();
            }

            var booleanExpression = new BooleanQueryExpression(op, left, right);

            if (this.expression[this.currentIndex] == ')')
            {
                if (parentExpression != null)
                {
                    parentExpression.Expressions.Add(booleanExpression);
                    return rootExpression!;
                }

                return booleanExpression;
            }
            if (this.expression[this.currentIndex] == '&')
            {
                if (!Match("&&"))
                {
                    throw CreateUnexpectedCharacterException();
                }

                if (parentExpression == null || parentExpression.Operator != QueryOperator.And)
                {
                    var andExpression = new CompositeExpression(QueryOperator.And);

                    parentExpression?.Expressions.Add(andExpression);

                    parentExpression = andExpression;

                    if (rootExpression == null)
                    {
                        rootExpression = parentExpression;
                    }
                }

                parentExpression.Expressions.Add(booleanExpression);
            }
            if (this.expression[this.currentIndex] == '|')
            {
                if (!Match("||"))
                {
                    throw CreateUnexpectedCharacterException();
                }

                if (parentExpression == null || parentExpression.Operator != QueryOperator.Or)
                {
                    var orExpression = new CompositeExpression(QueryOperator.Or);

                    parentExpression?.Expressions.Add(orExpression);

                    parentExpression = orExpression;

                    if (rootExpression == null)
                    {
                        rootExpression = parentExpression;
                    }
                }

                parentExpression.Expressions.Add(booleanExpression);
            }
        }

        throw new JsonException("Path ended with open query.");
    }

    private bool TryParseValue(out object value)
    {
        var currentChar = this.expression[this.currentIndex];
        if (currentChar == '\'')
        {
            value = ReadQuotedString();
            return true;
        }
        else if (char.IsDigit(currentChar) || currentChar == '-')
        {
            var sb = new StringBuilder();
            sb.Append(currentChar);

            this.currentIndex++;
            while (this.currentIndex < this.expression.Length)
            {
                currentChar = this.expression[this.currentIndex];
                if (currentChar == ' ' || currentChar == ')')
                {
                    var numberText = sb.ToString();

                    if (numberText.IndexOfAny(FloatCharacters) != -1)
                    {
                        var result = double.TryParse(numberText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d);
                        value = d;
                        return result;
                    }
                    else
                    {
                        var result = long.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l);
                        value = l;
                        return result;
                    }
                }
                else
                {
                    sb.Append(currentChar);
                    this.currentIndex++;
                }
            }
        }
        else if (currentChar == 't')
        {
            if (Match("true"))
            {
                value = true;
                return true;
            }
        }
        else if (currentChar == 'f')
        {
            if (Match("false"))
            {
                value = false;
                return true;
            }
        }
        else if (currentChar == 'n')
        {
            if (Match("null"))
            {
                value = null;
                return true;
            }
        }
        else if (currentChar == '/')
        {
            value = ReadRegexString();
            return true;
        }

        value = null;
        return false;
    }

    private string ReadQuotedString()
    {
        var sb = new StringBuilder();

        this.currentIndex++;
        while (this.currentIndex < this.expression.Length)
        {
            var currentChar = this.expression[this.currentIndex];
            if (currentChar == '\\' && this.currentIndex + 1 < this.expression.Length)
            {
                this.currentIndex++;
                currentChar = this.expression[this.currentIndex];

                char resolvedChar;
                switch (currentChar)
                {
                    case 'b':
                        resolvedChar = '\b';
                        break;
                    case 't':
                        resolvedChar = '\t';
                        break;
                    case 'n':
                        resolvedChar = '\n';
                        break;
                    case 'f':
                        resolvedChar = '\f';
                        break;
                    case 'r':
                        resolvedChar = '\r';
                        break;
                    case '\\':
                    case '"':
                    case '\'':
                    case '/':
                        resolvedChar = currentChar;
                        break;
                    default:
                        throw new JsonException(@"Unknown escape character: \" + currentChar);
                }

                sb.Append(resolvedChar);

                this.currentIndex++;
            }
            else if (currentChar == '\'')
            {
                this.currentIndex++;
                return sb.ToString();
            }
            else
            {
                this.currentIndex++;
                sb.Append(currentChar);
            }
        }

        throw new JsonException("Path ended with an open string.");
    }

    private string ReadRegexString()
    {
        var startIndex = this.currentIndex;

        this.currentIndex++;
        while (this.currentIndex < this.expression.Length)
        {
            var currentChar = this.expression[this.currentIndex];

            // handle escaped / character
            if (currentChar == '\\' && this.currentIndex + 1 < this.expression.Length)
            {
                this.currentIndex += 2;
            }
            else if (currentChar == '/')
            {
                this.currentIndex++;

                while (this.currentIndex < this.expression.Length)
                {
                    currentChar = this.expression[this.currentIndex];

                    if (char.IsLetter(currentChar))
                    {
                        this.currentIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                return this.expression.Substring(startIndex, this.currentIndex - startIndex);
            }
            else
            {
                this.currentIndex++;
            }
        }

        throw new JsonException("Path ended with an open regex.");
    }

    private bool Match(string s)
    {
        var currentPosition = this.currentIndex;
        for (var i = 0; i < s.Length; i++)
        {
            if (currentPosition < this.expression.Length && this.expression[currentPosition] == s[i])
            {
                currentPosition++;
            }
            else
            {
                return false;
            }
        }

        this.currentIndex = currentPosition;
        return true;
    }

    private QueryOperator ParseOperator()
    {
        if (this.currentIndex + 1 >= this.expression.Length)
        {
            throw new JsonException("Path ended with open query.");
        }

        if (Match("==="))
        {
            return QueryOperator.StrictEquals;
        }

        if (Match("=="))
        {
            return QueryOperator.Equals;
        }

        if (Match("=~"))
        {
            return QueryOperator.RegexEquals;
        }

        if (Match("!=="))
        {
            return QueryOperator.StrictNotEquals;
        }

        if (Match("!=") || Match("<>"))
        {
            return QueryOperator.NotEquals;
        }
        if (Match("<="))
        {
            return QueryOperator.LessThanOrEquals;
        }
        if (Match("<"))
        {
            return QueryOperator.LessThan;
        }
        if (Match(">="))
        {
            return QueryOperator.GreaterThanOrEquals;
        }
        if (Match(">"))
        {
            return QueryOperator.GreaterThan;
        }

        throw new JsonException("Could not read query operator.");
    }

    private PathFilter ParseQuotedField(char indexerCloseChar, bool scan)
    {
        List<string> fields = null;

        while (this.currentIndex < this.expression.Length)
        {
            var field = ReadQuotedString();

            EatWhitespace();
            EnsureLength("Path ended with open indexer.");

            if (this.expression[this.currentIndex] == indexerCloseChar)
            {
                if (fields != null)
                {
                    fields.Add(field);
                    return (scan)
                        ? new ScanMultipleFilter(fields)
                        : new FieldMultipleFilter(fields);
                }
                else
                {
                    return CreatePathFilter(field, scan);
                }
            }
            else if (this.expression[this.currentIndex] == ',')
            {
                this.currentIndex++;
                EatWhitespace();

                if (fields == null)
                {
                    fields = new List<string>();
                }

                fields.Add(field);
            }
            else
            {
                throw new JsonException("Unexpected character while parsing path indexer: " + this.expression[this.currentIndex]);
            }
        }

        throw new JsonException("Path ended with open indexer.");
    }

    private void EnsureLength(string message)
    {
        if (this.currentIndex >= this.expression.Length)
        {
            throw new JsonException(message);
        }
    }

    internal IEnumerable<JsonElement> Evaluate(JsonElement root, JsonElement t, bool errorWhenNoMatch)
    {
        return Evaluate(this.filters, root, t, errorWhenNoMatch);
    }

    internal static IEnumerable<JsonElement> Evaluate(List<PathFilter> filters, JsonElement root, JsonElement t, bool errorWhenNoMatch)
    {
        IEnumerable<JsonElement> current = new[] { t };
        foreach (var filter in filters)
        {
            current = filter.ExecuteFilter(root, current, errorWhenNoMatch);
        }

        return current;
    }
}
