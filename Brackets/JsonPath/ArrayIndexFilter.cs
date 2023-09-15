namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    internal class ArrayIndexFilter : PathFilter
    {
        public int? Index { get; init; }

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                if (this.Index != null)
                {
                    var v = GetTokenIndex(t, errorWhenNoMatch, this.Index.GetValueOrDefault());

                    if (v.HasValue)
                    {
                        yield return v.Value;
                    }
                }
                else
                {
                    if (t.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var v in t.EnumerateArray())
                        {
                            yield return v;
                        }
                    }
                    else
                    {
                        if (errorWhenNoMatch)
                        {
                            throw new JsonException($"Index * not valid on {t.GetType().Name}.");
                        }
                    }
                }
            }
        }
    }
}
