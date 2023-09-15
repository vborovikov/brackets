namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    internal class ScanMultipleFilter : PathFilter
    {
        private readonly IEnumerable<string> names;

        public ScanMultipleFilter(IEnumerable<string> names)
        {
            this.names = names;
        }

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var c in current)
            {
                JsonElement? value = c;

                foreach (var e in GetScanValues(c))
                {
                    if (e.Name != null)
                    {
                        foreach (var name in this.names)
                        {
                            if (e.Name == name)
                            {
                                yield return e.Value;
                            }
                        }
                    }
                }
            }
        }
    }
}
