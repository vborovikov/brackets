namespace Brackets;

using System;

public static class ElementExtensions
{
    public static bool Contains(this Element element, ReadOnlySpan<char> text)
    {
        if (element is Attribute attribute)
            return attribute.HasValue && attribute.Value.Contains(text, StringComparison.CurrentCultureIgnoreCase);

        if (element is Content content)
            return content.Contains(text);

        if (element is ParentTag parent)
        {
            foreach (var child in parent)
            {
                if (child is Content childContent && childContent.Contains(text))
                    return true;
            }
        }

        return false;
    }

    public static Element First(this Document document) => document.Root.First();

    public static Element First(this ParentTag root)
    {
        return root.Child ?? throw new InvalidOperationException("Sequence contains no elements.");
    }

    public static Element First(this Document document, Func<Element, bool> predicate) => document.Root.First(predicate);

    public static Element First(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child ?? throw new InvalidOperationException("Sequence contains no elements.");

        var current = first;
        do
        {
            if (predicate(current))
                return current;
            current = current.Next;
        }
        while (current != first);

        throw new InvalidOperationException("No element satisfies the condition.");
    }

    public static Element Last(this Document document) => document.Root.Last();

    /// <summary>
    /// Returns the last element in the parent-child relationship of elements, or a default value if the sequence is empty.
    /// </summary>
    /// <param name="root">The root element.</param>
    /// <returns>The last child element of the root element, or null if the root has no children.</returns>
    public static Element Last(this ParentTag root)
    {
        var first = root.Child ?? throw new InvalidOperationException("Sequence contains no elements.");
        return first.Prev;
    }

    public static Element Last(this Document document, Func<Element, bool> predicate) => document.Root.Last(predicate);

    public static Element Last(this ParentTag root, Func<Element, bool> predicate)
    {
        var last = root.Child?.Prev ?? throw new InvalidOperationException("Sequence contains no elements.");

        var current = last;
        do
        {
            if (predicate(current))
                return current;
            current = current.Prev;
        }
        while (current != last);

        throw new InvalidOperationException("No element satisfies the condition.");
    }

    public static Element? FirstOrDefault(this Document document) => document.Root.FirstOrDefault();

    public static Element? FirstOrDefault(this ParentTag root) => root.Child;

    public static Element? FirstOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.FirstOrDefault(predicate);

    public static Element? FirstOrDefault(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child;
        if (first is null)
            return null;

        var current = first;
        do
        {
            if (predicate(current))
                return current;
            current = current.Next;
        }
        while (current != first);

        return null;
    }

    public static Element? LastOrDefault(this Document document) => document.Root.LastOrDefault();

    public static Element? LastOrDefault(this ParentTag root) => root.Child?.Prev;

    public static Element? LastOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.LastOrDefault(predicate);

    public static Element? LastOrDefault(this ParentTag root, Func<Element, bool> predicate)
    {
        var last = root.Child?.Prev;
        if (last is null)
            return null;

        var current = last;
        do
        {
            if (predicate(current))
                return current;
            current = current.Prev;
        }
        while (current != last);

        return null;
    }

    public static Element Single(this Document document) => document.Root.Single();

    public static Element Single(this ParentTag root)
    {
        var single = root.Child ?? throw new InvalidOperationException("Sequence contains no elements.");
        if (single.Next != single)
            throw new InvalidOperationException("Sequence contains more than one element.");

        return single;
    }

    public static Element Single(this Document document, Func<Element, bool> predicate) => document.Root.Single(predicate);

    public static Element Single(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child ?? throw new InvalidOperationException("Sequence contains no elements.");

        var single = default(Element);
        var current = first;
        do
        {
            if (predicate(current))
            {
                if (single is not null)
                    throw new InvalidOperationException("Sequence contains more than one element.");
                single = current;
            }
            current = current.Next;
        }
        while (current != first);

        if (single is null)
            throw new InvalidOperationException("Sequence contains no matching elements.");

        return single;
    }

    public static Element? SingleOrDefault(this Document document) => document.Root.SingleOrDefault();

    public static Element? SingleOrDefault(this ParentTag root)
    {
        var first = root.Child;
        if (first is null)
            return null;

        var single = default(Element);
        var current = first;
        do
        {
            if (single is not null)
                throw new InvalidOperationException("Sequence contains more than one element.");
            single = current;
            current = current.Next;
        }
        while (current != first);

        return single;
    }

    public static Element? SingleOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.SingleOrDefault(predicate);

    public static Element? SingleOrDefault(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child;
        if (first is null)
            return null;

        var single = default(Element);
        var current = first;
        do
        {
            if (predicate(current))
            {
                if (single is not null)
                    throw new InvalidOperationException("Sequence contains more than one element.");
                single = current;
            }
            current = current.Next;
        }
        while (current != first);

        return single;
    }

    public static bool All(this Document document, Func<Element, bool> predicate) => document.Root.All(predicate);

    public static bool All(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child;
        if (first is null)
            return true;

        var current = first;
        do
        {
            if (!predicate(current))
                return false;
            current = current.Next;
        }
        while (current != first);

        return true;
    }

    public static bool Any(this Document document, Func<Element, bool> predicate) => document.Root.Any(predicate);

    public static bool Any(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child;
        if (first is null)
            return false;

        var current = first;
        do
        {
            if (predicate(current))
                return true;
            current = current.Next;
        }
        while (current != first);

        return false;
    }

    public static bool Any(this Document document) => document.Root.Any();

    public static bool Any(this ParentTag root) => root.Child is not null;

    public static int Count(this Document document, Func<Element, bool> predicate) => document.Root.Count(predicate);

    public static int Count(this ParentTag root, Func<Element, bool> predicate)
    {
        var first = root.Child;
        if (first is null)
            return 0;

        var current = first;
        int count = 0;
        do
        {
            if (predicate(current))
                count++;
            current = current.Next;
        }
        while (current != first);

        return count;
    }

    public static int Count(this Document document) => document.Root.Count();

    public static int Count(this ParentTag root)
    {
        var first = root.Child;
        if (first is null)
            return 0;

        var current = first;
        int count = 0;
        do
        {
            count++;
            current = current.Next;
        }
        while (current != first);

        return count;
    }
}