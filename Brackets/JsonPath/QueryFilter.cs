namespace Brackets.JPath
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    internal class QueryFilter : PathFilter
    {
        private readonly QueryExpression expression;

        public QueryFilter(QueryExpression expression)
        {
            this.expression = expression;
        }

        public QueryExpression Expression => this.expression;

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                if (t.ValueKind == JsonValueKind.Array)
                {
                    foreach (var v in t.EnumerateArray())
                    {
                        if (this.expression.IsMatch(root, v))
                        {
                            yield return v;
                        }
                    }
                }
                else if (t.ValueKind == JsonValueKind.Object)
                {
                    foreach (var v in t.EnumerateObject())
                    {
                        if (this.expression.IsMatch(root, v.Value))
                        {
                            yield return v.Value;
                        }
                    }
                }
            }
        }
    }
}
