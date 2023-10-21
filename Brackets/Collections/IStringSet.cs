namespace Brackets.Collections;

using System;
using System.Diagnostics.CodeAnalysis;

public interface IStringSet<T> : IEnumerable<T>
    where T : notnull
{
    int Count { get; }

    bool Add(string key, T value);
    bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value);
}