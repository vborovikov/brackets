﻿namespace Brackets;

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

    public static Element PreviousSibling(this Element element, Func<Element, bool> predicate)
    {
        if (PreviousSiblingOrDefault(element, predicate) is not Element prev)
            throw new InvalidOperationException("Element has no previous sibling.");

        return prev;
    }

    public static Element? PreviousSiblingOrDefault(this Element element, Func<Element, bool> predicate)
    {
        var first = element is Attr ? element.Parent?.FirstAttribute : (element.Parent as ParentTag)?.Child;
        if (element.Parent is null || first is null)
            return default;

        while (element != first)
        {
            element = element.Prev;
            if (predicate(element))
                return element;
        }

        return default;
    }

    public static Element NextSibling(this Element element, Func<Element, bool> predicate)
    {
        if (NextSiblingOrDefault(element, predicate) is not Element next)
            throw new InvalidOperationException("Element has no next sibling.");

        return next;
    }

    public static Element? NextSiblingOrDefault(this Element element, Func<Element, bool> predicate)
    {
        var last = element is Attr ? element.Parent?.FirstAttribute?.Prev : (element.Parent as ParentTag)?.Child?.Prev;
        if (element.Parent is null || last is null)
            return default;

        while (element != last)
        {
            element = element.Next;
            if (predicate(element))
                return element;
        }

        return default;
    }

    public static TElement PreviousSibling<TElement>(this Element element) where TElement : Element =>
        (TElement)element.PreviousSibling(e => e is TElement);

    public static TElement? PreviousSiblingOrDefault<TElement>(this Element element) where TElement : Element =>
        (TElement?)element.PreviousSiblingOrDefault(e => e is TElement);

    public static TElement NextSibling<TElement>(this Element element) where TElement : Element =>
        (TElement)element.NextSibling(e => e is TElement);

    public static TElement? NextSiblingOrDefault<TElement>(this Element element) where TElement : Element =>
        (TElement?)element.NextSiblingOrDefault(e => e is TElement);

    public static TElement PreviousSibling<TElement>(this Element element, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)element.PreviousSibling(e => e is TElement sibling && predicate(sibling));

    public static TElement? PreviousSiblingOrDefault<TElement>(this Element element, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)element.PreviousSiblingOrDefault(e => e is TElement sibling && predicate(sibling));

    public static TElement NextSibling<TElement>(this Element element, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)element.NextSibling(e => e is TElement sibling && predicate(sibling));

    public static TElement? NextSiblingOrDefault<TElement>(this Element element, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)element.NextSiblingOrDefault(e => e is TElement sibling && predicate(sibling));

    public static bool Contains(this Element element, ReadOnlySpan<char> text)
    {
        if (element is Attr attribute)
            return attribute.HasValue && attribute.Value.Contains(text, StringComparison.CurrentCultureIgnoreCase);

        if (element is Content content)
            return content.Contains(text);

        if (element is ParentTag parent)
        {
            foreach (var child in parent.GetEnumerator())
            {
                if (child is Content childContent && childContent.Contains(text))
                    return true;
            }
        }

        return false;
    }

    public static Element First(this Document document) => document.Root.First();

    public static Element First(this ParentTag parent) => parent.Child ?? throw new InvalidOperationException("Sequence contains no elements.");

    private static Element? Child(this IRoot root) => (root as IParent)?.Child;

    public static Element First(this IRoot root) => root.Child() ?? throw new InvalidOperationException("Sequence contains no elements.");

    public static Attr First(this in Attr.List attributes)
    {
        return attributes.First ?? throw new InvalidOperationException("Sequence contains no elements.");
    }

    public static Element First(this Document document, Func<Element, bool> predicate) => document.Root.First(predicate);

    public static Element First(this ParentTag parent, Func<Element, bool> predicate) => FindFirst(parent.Child, predicate);

    public static Element First(this IRoot root, Func<Element, bool> predicate) => FindFirst(root.Child(), predicate);

    public static Attr First(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr)FindFirst(attributes.First, el => predicate((Attr)el));

    private static Element FindFirst(Element? first, Func<Element, bool> predicate)
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
    /// <param name="parent">The root element.</param>
    /// <returns>The last child element of the root element, or null if the root has no children.</returns>
    public static Element Last(this ParentTag parent)
    {
        var first = parent.Child ?? throw new InvalidOperationException("Sequence contains no elements.");
        return first.Prev;
    }

    public static Element Last(this IRoot root)
    {
        var first = root.Child() ?? throw new InvalidOperationException("Sequence contains no elements.");
        return first.Prev;
    }

    public static Attr Last(this in Attr.List attributes)
    {
        if (attributes.First is null)
            throw new InvalidOperationException("Sequence contains no elements.");

        return (Attr)attributes.First.Prev;
    }

    public static Element Last(this Document document, Func<Element, bool> predicate) => document.Root.Last(predicate);

    public static Element Last(this ParentTag parent, Func<Element, bool> predicate) => FindLast(parent.Child?.Prev, predicate);

    public static Element Last(this IRoot root, Func<Element, bool> predicate) => FindLast(root.Child()?.Prev, predicate);

    public static Attr Last(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr)FindLast(attributes.First?.Prev, el => predicate((Attr)el));

    private static Element FindLast(Element? last, Func<Element, bool> predicate)
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

    public static Element? FirstOrDefault(this ParentTag parent) => parent.Child;

    public static Element? FirstOrDefault(this IRoot root) => root.Child();

    public static Attr? FirstOrDefault(this in Attr.List attributes) => attributes.First;

    public static Element? FirstOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.FirstOrDefault(predicate);

    public static Element? FirstOrDefault(this ParentTag parent, Func<Element, bool> predicate) =>
        FindFirstOrDefault(parent.Child, predicate);

    public static Element? FirstOrDefault(this IRoot root, Func<Element, bool> predicate) =>
        FindFirstOrDefault(root.Child(), predicate);

    public static Attr? FirstOrDefault(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr?)FindFirstOrDefault(attributes.First, el => predicate((Attr)el));

    private static Element? FindFirstOrDefault(Element? first, Func<Element, bool> predicate)
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

    public static Element? LastOrDefault(this ParentTag parent) => parent.Child?.Prev;

    public static Element? LastOrDefault(this IRoot root) => root.Child()?.Prev;

    public static Element? LastOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.LastOrDefault(predicate);

    public static Element? LastOrDefault(this ParentTag parent, Func<Element, bool> predicate) => FindLastOrDefault(parent.Child?.Prev, predicate);

    public static Element? LastOrDefault(this IRoot root, Func<Element, bool> predicate) => FindLastOrDefault(root.Child()?.Prev, predicate);

    public static Attr? LastOrDefault(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr?)FindLastOrDefault(attributes.First?.Prev, el => predicate((Attr)el));

    private static Element? FindLastOrDefault(Element? last, Func<Element, bool> predicate)
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

    public static Element Single(this ParentTag parent)
    {
        var single = parent.Child ?? throw new InvalidOperationException("Sequence contains no elements.");
        if (single.Next != single)
            throw new InvalidOperationException("Sequence contains more than one element.");

        return single;
    }

    public static Element Single(this IRoot root)
    {
        var single = root.Child() ?? throw new InvalidOperationException("Sequence contains no elements.");
        if (single.Next != single)
            throw new InvalidOperationException("Sequence contains more than one element.");

        return single;
    }

    public static Attr Single(this in Attr.List attributes)
    {
        if (attributes.First is not Attr single)
            throw new InvalidOperationException("Sequence contains no elements.");
        if (single.Next != single)
            throw new InvalidOperationException("Sequence contains more than one element.");

        return single;
    }

    public static Element Single(this Document document, Func<Element, bool> predicate) => document.Root.Single(predicate);

    public static Element Single(this ParentTag parent, Func<Element, bool> predicate) => FindSingle(parent.Child, predicate);

    public static Element Single(this IRoot root, Func<Element, bool> predicate) => FindSingle(root.Child(), predicate);

    public static Attr Single(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr)FindSingle(attributes.First, el => predicate((Attr)el));

    private static Element FindSingle(Element? first, Func<Element, bool> predicate)
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

    public static Element? SingleOrDefault(this ParentTag parent) => FindSingleOrDefault(parent.Child);

    public static Element? SingleOrDefault(this IRoot root) => FindSingleOrDefault(root.Child());

    public static Attr? SingleOrDefault(this in Attr.List attributes) => (Attr?)FindSingleOrDefault(attributes.First);

    private static Element? FindSingleOrDefault(Element? first)
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

    public static Element? SingleOrDefault(this ParentTag parent, Func<Element, bool> predicate) => FindSingleOrDefault(parent.Child, predicate);

    public static Element? SingleOrDefault(this IRoot root, Func<Element, bool> predicate) => FindSingleOrDefault(root.Child(), predicate);

    public static Attr? SingleOrDefault(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr?)FindSingleOrDefault(attributes.First, el => predicate((Attr)el));

    private static Element? FindSingleOrDefault(Element? first, Func<Element, bool> predicate)
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

    public static TElement First<TElement>(this Document document) where TElement : Element =>
        (TElement)document.First(e => e is TElement);

    public static TElement First<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement)parent.First(e => e is TElement);

    public static TElement First<TElement>(this IRoot root) where TElement : Element =>
        (TElement)root.First(e => e is TElement);

    public static TElement Last<TElement>(this Document document) where TElement : Element =>
        (TElement)document.Last(e => e is TElement);

    public static TElement Last<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement)parent.Last(e => e is TElement);

    public static TElement Last<TElement>(this IRoot root) where TElement : Element =>
        (TElement)root.Last(e => e is TElement);

    public static TElement Single<TElement>(this Document document) where TElement : Element =>
        (TElement)document.Single(e => e is TElement);

    public static TElement Single<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement)parent.Single(e => e is TElement);

    public static TElement Single<TElement>(this IRoot root) where TElement : Element =>
        (TElement)root.Single(e => e is TElement);

    public static TElement? FirstOrDefault<TElement>(this Document document) where TElement : Element =>
        (TElement?)document.FirstOrDefault(e => e is TElement);

    public static TElement? FirstOrDefault<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement?)parent.FirstOrDefault(e => e is TElement);

    public static TElement? FirstOrDefault<TElement>(this IRoot root) where TElement : Element =>
        (TElement?)root.FirstOrDefault(e => e is TElement);

    public static TElement? LastOrDefault<TElement>(this Document document) where TElement : Element =>
        (TElement?)document.LastOrDefault(e => e is TElement);

    public static TElement? LastOrDefault<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement?)parent.LastOrDefault(e => e is TElement);

    public static TElement? LastOrDefault<TElement>(this IRoot root) where TElement : Element =>
        (TElement?)root.LastOrDefault(e => e is TElement);

    public static TElement? SingleOrDefault<TElement>(this Document document) where TElement : Element =>
        (TElement?)document.SingleOrDefault(e => e is TElement);

    public static TElement? SingleOrDefault<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement?)parent.SingleOrDefault(e => e is TElement);

    public static TElement? SingleOrDefault<TElement>(this IRoot root) where TElement : Element =>
        (TElement?)root.SingleOrDefault(e => e is TElement);

    public static TElement First<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)document.First(e => e is TElement element && predicate(element));

    public static TElement First<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)parent.First(e => e is TElement element && predicate(element));

    public static TElement First<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)root.First(e => e is TElement element && predicate(element));

    public static TElement Last<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)document.Last(e => e is TElement element && predicate(element));

    public static TElement Last<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)parent.Last(e => e is TElement element && predicate(element));

    public static TElement Last<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)root.Last(e => e is TElement element && predicate(element));

    public static TElement Single<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)document.Single(e => e is TElement element && predicate(element));

    public static TElement Single<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)parent.Single(e => e is TElement element && predicate(element));

    public static TElement Single<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)root.Single(e => e is TElement element && predicate(element));

    public static TElement? FirstOrDefault<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)document.FirstOrDefault(e => e is TElement element && predicate(element));

    public static TElement? FirstOrDefault<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)parent.FirstOrDefault(e => e is TElement element && predicate(element));

    public static TElement? FirstOrDefault<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)root.FirstOrDefault(e => e is TElement element && predicate(element));

    public static TElement? LastOrDefault<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)document.LastOrDefault(e => e is TElement element && predicate(element));

    public static TElement? LastOrDefault<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)parent.LastOrDefault(e => e is TElement element && predicate(element));

    public static TElement? LastOrDefault<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)root.LastOrDefault(e => e is TElement element && predicate(element));

    public static TElement? SingleOrDefault<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)document.SingleOrDefault(e => e is TElement element && predicate(element));

    public static TElement? SingleOrDefault<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)parent.SingleOrDefault(e => e is TElement element && predicate(element));

    public static TElement? SingleOrDefault<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)root.SingleOrDefault(e => e is TElement element && predicate(element));

    public static bool All(this Document document, Func<Element, bool> predicate) => document.Root.All(predicate);

    public static bool All(this ParentTag parent, Func<Element, bool> predicate) => TestAll(parent.Child, predicate);

    public static bool All(this IRoot root, Func<Element, bool> predicate) => TestAll(root.Child(), predicate);

    public static bool All(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        TestAll(attributes.First, el => predicate((Attr)el));

    private static bool TestAll(Element? first, Func<Element, bool> predicate)
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

    public static bool Any(this ParentTag parent, Func<Element, bool> predicate) => TestAny(parent.Child, predicate);

    public static bool Any(this IRoot root, Func<Element, bool> predicate) => TestAny(root.Child(), predicate);

    public static bool Any(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        TestAny(attributes.First, el => predicate((Attr)el));

    private static bool TestAny(Element? first, Func<Element, bool> predicate)
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

    public static bool Any(this ParentTag parent) => parent.Child is not null;

    public static bool Any(this IRoot root) => root.Child() is not null;

    public static bool Any(this in Attr.List attributes) => attributes.First is not null;

    public static int Count(this Document document, Func<Element, bool> predicate) => document.Root.Count(predicate);

    public static int Count(this ParentTag parent, Func<Element, bool> predicate) => GetCount(parent.Child, predicate);

    public static int Count(this IRoot root, Func<Element, bool> predicate) => GetCount(root.Child(), predicate);

    public static int Count(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        GetCount(attributes.First, el => predicate((Attr)el));

    private static int GetCount(Element? first, Func<Element, bool> predicate)
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

    public static int Count(this ParentTag parent) => GetCount(parent.Child);

    public static int Count(this IRoot root) => GetCount(root.Child());

    public static int Count(this in Attr.List attributes) => GetCount(attributes.First);

    private static int GetCount(Element? first)
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

    public static bool All<TElement>(this Document document) where TElement : Element =>
        document.All(e => e is TElement);

    public static bool All<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        document.All(e => e is TElement element && predicate(element));

    public static bool All<TElement>(this ParentTag parent) where TElement : Element =>
        parent.All(e => e is TElement);

    public static bool All<TElement>(this IRoot root) where TElement : Element =>
        root.All(e => e is TElement);

    public static bool All<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        parent.All(e => e is TElement element && predicate(element));

    public static bool All<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        root.All(e => e is TElement element && predicate(element));

    public static bool Any<TElement>(this Document document) where TElement : Element =>
        document.Any(e => e is TElement);

    public static bool Any<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        document.Any(e => e is TElement element && predicate(element));

    public static bool Any<TElement>(this ParentTag parent) where TElement : Element =>
        parent.Any(e => e is TElement);

    public static bool Any<TElement>(this IRoot root) where TElement : Element =>
        root.Any(e => e is TElement);

    public static bool Any<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        parent.Any(e => e is TElement element && predicate(element));

    public static bool Any<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        root.Any(e => e is TElement element && predicate(element));

    public static int Count<TElement>(this Document document) where TElement : Element =>
        document.Count(e => e is TElement);

    public static int Count<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        document.Count(e => e is TElement element && predicate(element));

    public static int Count<TElement>(this ParentTag parent) where TElement : Element =>
        parent.Count(e => e is TElement);

    public static int Count<TElement>(this IRoot root) where TElement : Element =>
        root.Count(e => e is TElement);

    public static int Count<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        parent.Count(e => e is TElement element && predicate(element));

    public static int Count<TElement>(this IRoot root, Func<TElement, bool> predicate) where TElement : Element =>
        root.Count(e => e is TElement element && predicate(element));
}