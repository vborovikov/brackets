namespace Brackets.JPath
{
    using System.Text.Json;

    internal abstract class QueryExpression
    {
        public QueryExpression(QueryOperator @operator)
        {
            this.Operator = @operator;
        }

        public QueryOperator Operator { get; }

        public abstract bool IsMatch(JsonElement root, JsonElement t);
    }
}
