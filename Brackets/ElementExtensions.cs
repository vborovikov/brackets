namespace Brackets;

using System;

public static class ElementExtensions
{
    public static Element PreviousSibling(this Element element)
    {
        if (IsFirstOrSingleSibling(element))
            throw new InvalidOperationException("Element has no previous sibling.");

        return element.Prev;
    }

    public static Element? PreviousSiblingOrDefault(this Element element) => IsFirstOrSingleSibling(element) ? default : element.Prev;

    public static Element NextSibling(this Element element)
    {
        if (IsLastOrSingleSibling(element))
            throw new InvalidOperationException("Element has no next sibling.");

        return element.Next;
    }

    public static Element? NextSiblingOrDefault(this Element element) => IsLastOrSingleSibling(element) ? default : element.Next;

    private static bool IsFirstOrSingleSibling(Element element)
    {
        return
            element.Prev == element ||
            element.Parent?.FirstAttribute == element ||
            (element.Parent as ParentTag)?.Child == element;
    }

    private static bool IsLastOrSingleSibling(Element element)
    {
        return
            element.Next == element ||
            element.Parent?.FirstAttribute?.Prev == element ||
            (element.Parent as ParentTag)?.Child?.Prev == element;
    }

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

    public static Attribute First(this ITagAttributes attributes)
    {
        if (attributes is not Tag tag || tag.FirstAttribute is null)
            throw new InvalidOperationException("Sequence contains no elements.");

        return tag.FirstAttribute;
    }

    public static Element First(this Document document, Func<Element, bool> predicate) => document.Root.First(predicate);

    public static Element First(this ParentTag root, Func<Element, bool> predicate) => First(root.Child, predicate);

    public static Attribute First(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        (Attribute)First((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static Element First(Element? first, Func<Element, bool> predicate)
    {
        if (first is null)
            throw new InvalidOperationException("Sequence contains no elements.");

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

    public static Attribute Last(this ITagAttributes attributes)
    {
        if (attributes is not Tag tag || tag.FirstAttribute is null)
            throw new InvalidOperationException("Sequence contains no elements.");

        return (Attribute)tag.FirstAttribute.Prev;
    }

    public static Element Last(this Document document, Func<Element, bool> predicate) => document.Root.Last(predicate);

    public static Element Last(this ParentTag root, Func<Element, bool> predicate) => Last(root.Child?.Prev, predicate);

    public static Attribute Last(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        (Attribute)Last((attributes as Tag)?.FirstAttribute?.Prev, el => predicate((Attribute)el));

    private static Element Last(Element? last, Func<Element, bool> predicate)
    {
        if (last is null)
            throw new InvalidOperationException("Sequence contains no elements.");

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

    public static Attribute? FirstOrDefault(this ITagAttributes attributes) => (attributes as Tag)?.FirstAttribute;

    public static Element? FirstOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.FirstOrDefault(predicate);

    public static Element? FirstOrDefault(this ParentTag root, Func<Element, bool> predicate) =>
        FirstOrDefault(root.Child, predicate);

    public static Attribute? FirstOrDefault(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        (Attribute?)FirstOrDefault((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static Element? FirstOrDefault(Element? first, Func<Element, bool> predicate)
    {
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

    public static Element? LastOrDefault(this ParentTag root, Func<Element, bool> predicate) => LastOrDefault(root.Child?.Prev, predicate);

    public static Attribute? LastOrDefault(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        (Attribute?)LastOrDefault((attributes as Tag)?.FirstAttribute?.Prev, el => predicate((Attribute)el));

    private static Element? LastOrDefault(Element? last, Func<Element, bool> predicate)
    {
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

    public static Attribute Single(this ITagAttributes attributes)
    {
        if (attributes is not Tag tag || tag.FirstAttribute is not Attribute single)
            throw new InvalidOperationException("Sequence contains no elements.");
        if (single.Next != single)
            throw new InvalidOperationException("Sequence contains more than one element.");

        return single;
    }

    public static Element Single(this Document document, Func<Element, bool> predicate) => document.Root.Single(predicate);

    public static Element Single(this ParentTag root, Func<Element, bool> predicate) => Single(root.Child, predicate);

    public static Attribute Single(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        (Attribute)Single((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static Element Single(Element? first, Func<Element, bool> predicate)
    {
        if (first is null)
            throw new InvalidOperationException("Sequence contains no elements.");

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

    public static Element? SingleOrDefault(this ParentTag root) => SingleOrDefault(root.Child);

    public static Attribute? SingleOrDefault(this ITagAttributes attributes) => (Attribute?)SingleOrDefault((attributes as Tag)?.FirstAttribute);

    private static Element? SingleOrDefault(Element? first)
    {
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

    public static Element? SingleOrDefault(this ParentTag root, Func<Element, bool> predicate) => SingleOrDefault(root.Child, predicate);

    public static Attribute? SingleOrDefault(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        (Attribute?)SingleOrDefault((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static Element? SingleOrDefault(Element? first, Func<Element, bool> predicate)
    {
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

    public static bool All(this ParentTag root, Func<Element, bool> predicate) => All(root.Child, predicate);

    public static bool All(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        All((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static bool All(Element? first, Func<Element, bool> predicate)
    {
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

    public static bool Any(this ParentTag root, Func<Element, bool> predicate) => Any(root.Child, predicate);

    public static bool Any(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        Any((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static bool Any(Element? first, Func<Element, bool> predicate)
    {
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

    public static bool Any(this ITagAttributes attributes) => attributes is Tag { FirstAttribute: not null };

    public static int Count(this Document document, Func<Element, bool> predicate) => document.Root.Count(predicate);

    public static int Count(this ParentTag root, Func<Element, bool> predicate) => Count(root.Child, predicate);

    public static int Count(this ITagAttributes attributes, Func<Attribute, bool> predicate) =>
        Count((attributes as Tag)?.FirstAttribute, el => predicate((Attribute)el));

    private static int Count(Element? first, Func<Element, bool> predicate)
    {
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

    public static int Count(this ParentTag root) => Count(root.Child);

    public static int Count(this ITagAttributes attributes) => Count((attributes as Tag)?.FirstAttribute);

    private static int Count(Element? first)
    {
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