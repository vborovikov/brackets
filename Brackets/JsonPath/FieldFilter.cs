namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    internal class FieldFilter : PathFilter
    {
        private readonly string name;

        public FieldFilter(string name)
        {
            this.name = name;
        }

        public string Name => this.name;

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                if (t.ValueKind == JsonValueKind.Object)
                {
                    if (this.name != null)
                    {
                        if (t.TryGetProperty(this.name, out var v))
                        {
                            yield return v;
                        }
                        else if (errorWhenNoMatch)
                        {
                            throw new JsonException($"Property '{this.name}' does not exist on BsonDocument.");
                        }
                    }
                    else
                    {
                        foreach (var p in t.EnumerateObject())
                        {
                            yield return p.Value;
                        }
                    }
                }
                else
                {
                    if (errorWhenNoMatch)
                    {
                        throw new JsonException($"Property '{this.name ?? "*"}' not valid on {t.GetType().Name}.");
                    }
                }
            }
        }
    }
}
