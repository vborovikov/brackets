namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    internal sealed class RootFilter : PathFilter
    {
        public static readonly RootFilter Instance = new();

        private RootFilter()
        {
        }

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            return Enumerable.Repeat(root, 1);
        }
    }
}
