namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    internal class QueryScanFilter : PathFilter
    {
        private readonly QueryExpression expression;

        public QueryScanFilter(QueryExpression expression)
        {
            this.expression = expression;
        }

        public QueryExpression Expression => this.expression;

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                foreach (var d in GetScanValues(t))
                {
                    if (this.expression.IsMatch(root, d.Value))
                    {
                        yield return d.Value;
                    }
                }
            }
        }
    }
}
