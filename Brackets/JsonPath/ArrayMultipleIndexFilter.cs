namespace Brackets.JPath
{
    using System.Text.Json;
    using System.Collections.Generic;

    internal class ArrayMultipleIndexFilter : PathFilter
    {
        private readonly IEnumerable<int> indexes;

        public ArrayMultipleIndexFilter(IEnumerable<int> indexes)
        {
            this.indexes = indexes;
        }

        public IEnumerable<int> Indexes => this.indexes;

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                foreach (var i in this.indexes)
                {
                    var v = GetTokenIndex(t, errorWhenNoMatch, i);

                    if (v.HasValue)
                    {
                        yield return v.Value;
                    }
                }
            }
        }
    }
}
