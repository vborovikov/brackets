namespace Brackets.JPath
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    internal class CompositeExpression : QueryExpression
    {
        public CompositeExpression(QueryOperator @operator) : base(@operator)
        {
            this.Expressions = new List<QueryExpression>();
        }

        public IList<QueryExpression> Expressions { get; }

        public override bool IsMatch(JsonElement root, JsonElement t)
        {
            switch (this.Operator)
            {
                case QueryOperator.And:
                    foreach (var e in this.Expressions)
                    {
                        if (!e.IsMatch(root, t))
                        {
                            return false;
                        }
                    }
                    return true;
                case QueryOperator.Or:
                    foreach (var e in this.Expressions)
                    {
                        if (e.IsMatch(root, t))
                        {
                            return true;
                        }
                    }
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
