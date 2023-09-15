namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    internal class ScanFilter : PathFilter
    {
        private readonly string name;

        public ScanFilter(string name)
        {
            this.name = name;
        }

        public string Name => this.name;

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var c in current)
            {
                foreach (var e in GetScanValues(c))
                {
                    if (e.Name == this.name)
                    {
                        yield return e.Value;
                    }
                }
            }
        }
    }
}
