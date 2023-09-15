namespace Brackets.JPath
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    internal class ArraySliceFilter : PathFilter
    {
        public int? Start { get; init; }
        public int? End { get; init; }
        public int? Step { get; init; }

        public override IEnumerable<JsonElement> ExecuteFilter(JsonElement root, IEnumerable<JsonElement> current, bool errorWhenNoMatch)
        {
            if (this.Step == 0)
            {
                throw new JsonException("Step cannot be zero.");
            }

            foreach (var t in current)
            {
                if (t.ValueKind == JsonValueKind.Array)
                {
                    var count = t.GetArrayLength();

                    // set defaults for null arguments
                    var stepCount = this.Step ?? 1;
                    var startIndex = this.Start ?? ((stepCount > 0) ? 0 : count - 1);
                    var stopIndex = this.End ?? ((stepCount > 0) ? count : -1);

                    // start from the end of the list if start is negative
                    if (this.Start < 0)
                    {
                        startIndex = count + startIndex;
                    }

                    // end from the start of the list if stop is negative
                    if (this.End < 0)
                    {
                        stopIndex = count + stopIndex;
                    }

                    // ensure indexes keep within collection bounds
                    startIndex = Math.Max(startIndex, (stepCount > 0) ? 0 : int.MinValue);
                    startIndex = Math.Min(startIndex, (stepCount > 0) ? count : count - 1);
                    stopIndex = Math.Max(stopIndex, -1);
                    stopIndex = Math.Min(stopIndex, count);

                    var positiveStep = (stepCount > 0);

                    if (IsValid(startIndex, stopIndex, positiveStep))
                    {
                        for (var i = startIndex; IsValid(i, stopIndex, positiveStep); i += stepCount)
                        {
                            yield return t[i];
                        }
                    }
                    else
                    {
                        if (errorWhenNoMatch)
                        {
                            throw new JsonException(
                                $"Array slice of {(this.Start != null ? this.Start.GetValueOrDefault().ToString() : "*")} to " +
                                $"{(this.End != null ? this.End.GetValueOrDefault().ToString() : " * ")} returned no results.");
                        }
                    }
                }
                else
                {
                    if (errorWhenNoMatch)
                    {
                        throw new JsonException($"Array slice is not valid on {t.GetType().Name}.");
                    }
                }
            }
        }

        private static bool IsValid(int index, int stopIndex, bool positiveStep)
        {
            if (positiveStep)
            {
                return (index < stopIndex);
            }

            return (index > stopIndex);
        }
    }
}
