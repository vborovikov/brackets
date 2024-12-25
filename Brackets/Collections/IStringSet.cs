namespace Brackets.Collections;

using System;
using System.Diagnostics.CodeAnalysis;

interface IStringSet<T> : IEnumerable<T>
    where T : notnull
{
    int Count { get; }

    bool Add(string key, T value);
    bool TryRemove(string key);
    bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value);
}