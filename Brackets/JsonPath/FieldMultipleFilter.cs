namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    internal class FieldMultipleFilter : PathFilter
    {
        private readonly IEnumerable<string> names;

        public FieldMultipleFilter(IEnumerable<string> names)
        {
            this.names = names;
        }

        public IEnumerable<string> Names => this.names;

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                if (t.ValueKind == JsonValueKind.Object)
                {
                    foreach (var name in this.names)
                    {
                        if (t.TryGetProperty(name, out var v))
                        {
                            yield return v;
                        }

                        if (errorWhenNoMatch)
                        {
                            throw new JsonException($"Property '{name}' does not exist on JsonDocument.");
                        }
                    }
                }
                else
                {
                    if (errorWhenNoMatch)
                    {
                        throw new JsonException($"Properties {string.Join(", ", this.names.Select(n => "'" + n + "'"))} not valid on {t.GetType().Name}.");
                    }
                }
            }
        }
    }
}
