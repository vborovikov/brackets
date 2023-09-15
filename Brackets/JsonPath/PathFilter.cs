namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    internal abstract class PathFilter
    {
        public abstract IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch);

        protected static JsonElement? GetTokenIndex(JsonElement t, bool errorWhenNoMatch, int index)
        {
            if (t.ValueKind == JsonValueKind.Array)
            {
                if (t.GetArrayLength() <= index)
                {
                    if (errorWhenNoMatch)
                    {
                        throw new JsonException($"Index {index} outside the bounds of BsonArray.");
                    }

                    return null;
                }

                return t[index];
            }
            else
            {
                if (errorWhenNoMatch)
                {
                    throw new JsonException($"Index {index} not valid on {t.GetType().Name}.");
                }

                return null;
            }
        }

        protected static IEnumerable<(string Name, JsonElement Value)> GetScanValues(JsonElement container)
        {
            yield return (null, container);
            if (container.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in container.EnumerateArray())
                {
                    foreach (var c in GetScanValues(e))
                    {
                        yield return c;
                    }
                }
            }
            else if (container.ValueKind == JsonValueKind.Object)
            {
                foreach (var e in container.EnumerateObject())
                {
                    yield return (e.Name, e.Value);
                    foreach (var c in GetScanValues(e.Value))
                    {
                        yield return c;
                    }
                }
            }
        }
    }
}
