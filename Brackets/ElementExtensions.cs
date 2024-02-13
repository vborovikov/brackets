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

    public static Attr First(this in Attr.List attributes)
    {
        if (attributes.First is null)
            throw new InvalidOperationException("Sequence contains no elements.");

        return attributes.First;
    }

    public static Element First(this Document document, Func<Element, bool> predicate) => document.Root.First(predicate);

    public static Element First(this ParentTag root, Func<Element, bool> predicate) => First(root.Child, predicate);

    public static Attr First(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr)First(attributes.First, el => predicate((Attr)el));

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

    public static Attr Last(this in Attr.List attributes)
    {
        if (attributes.First is null)
            throw new InvalidOperationException("Sequence contains no elements.");

        return (Attr)attributes.First.Prev;
    }

    public static Element Last(this Document document, Func<Element, bool> predicate) => document.Root.Last(predicate);

    public static Element Last(this ParentTag root, Func<Element, bool> predicate) => Last(root.Child?.Prev, predicate);

    public static Attr Last(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr)Last(attributes.First?.Prev, el => predicate((Attr)el));

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

    public static Attr? FirstOrDefault(this in Attr.List attributes) => attributes.First;

    public static Element? FirstOrDefault(this Document document, Func<Element, bool> predicate) => document.Root.FirstOrDefault(predicate);

    public static Element? FirstOrDefault(this ParentTag root, Func<Element, bool> predicate) =>
        FirstOrDefault(root.Child, predicate);

    public static Attr? FirstOrDefault(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr?)FirstOrDefault(attributes.First, el => predicate((Attr)el));

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

    public static Attr? LastOrDefault(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr?)LastOrDefault(attributes.First?.Prev, el => predicate((Attr)el));

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

    public static Attr Single(this in Attr.List attributes)
    {
        if (attributes.First is not Attr single)
            throw new InvalidOperationException("Sequence contains no elements.");
        if (single.Next != single)
            throw new InvalidOperationException("Sequence contains more than one element.");

        return single;
    }

    public static Element Single(this Document document, Func<Element, bool> predicate) => document.Root.Single(predicate);

    public static Element Single(this ParentTag root, Func<Element, bool> predicate) => Single(root.Child, predicate);

    public static Attr Single(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr)Single(attributes.First, el => predicate((Attr)el));

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

    public static Attr? SingleOrDefault(this in Attr.List attributes) => (Attr?)SingleOrDefault(attributes.First);

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

    public static Attr? SingleOrDefault(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        (Attr?)SingleOrDefault(attributes.First, el => predicate((Attr)el));

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

    public static TElement First<TElement>(this Document document) where TElement : Element =>
        (TElement)document.First(e => e is TElement);
    public static TElement First<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement)parent.First(e => e is TElement);
    public static TElement Last<TElement>(this Document document) where TElement : Element =>
        (TElement)document.Last(e => e is TElement);
    public static TElement Last<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement)parent.Last(e => e is TElement);
    public static TElement Single<TElement>(this Document document) where TElement : Element =>
        (TElement)document.Single(e => e is TElement);
    public static TElement Single<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement)parent.Single(e => e is TElement);

    public static TElement? FirstOrDefault<TElement>(this Document document) where TElement : Element =>
        (TElement?)document.FirstOrDefault(e => e is TElement);
    public static TElement? FirstOrDefault<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement?)parent.FirstOrDefault(e => e is TElement);
    public static TElement? LastOrDefault<TElement>(this Document document) where TElement : Element =>
        (TElement?)document.LastOrDefault(e => e is TElement);
    public static TElement? LastOrDefault<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement?)parent.LastOrDefault(e => e is TElement);
    public static TElement? SingleOrDefault<TElement>(this Document document) where TElement : Element =>
        (TElement?)document.SingleOrDefault(e => e is TElement);
    public static TElement? SingleOrDefault<TElement>(this ParentTag parent) where TElement : Element =>
        (TElement?)parent.SingleOrDefault(e => e is TElement);

    public static TElement First<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)document.First(e => e is TElement element && predicate(element));
    public static TElement First<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)parent.First(e => e is TElement element && predicate(element));
    public static TElement Last<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)document.Last(e => e is TElement element && predicate(element));
    public static TElement Last<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)parent.Last(e => e is TElement element && predicate(element));
    public static TElement Single<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)document.Single(e => e is TElement element && predicate(element));
    public static TElement Single<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement)parent.Single(e => e is TElement element && predicate(element));

    public static TElement? FirstOrDefault<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)document.FirstOrDefault(e => e is TElement element && predicate(element));
    public static TElement? FirstOrDefault<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)parent.FirstOrDefault(e => e is TElement element && predicate(element));
    public static TElement? LastOrDefault<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)document.LastOrDefault(e => e is TElement element && predicate(element));
    public static TElement? LastOrDefault<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)parent.LastOrDefault(e => e is TElement element && predicate(element));
    public static TElement? SingleOrDefault<TElement>(this Document document, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)document.SingleOrDefault(e => e is TElement element && predicate(element));
    public static TElement? SingleOrDefault<TElement>(this ParentTag parent, Func<TElement, bool> predicate) where TElement : Element =>
        (TElement?)parent.SingleOrDefault(e => e is TElement element && predicate(element));

    public static bool All(this Document document, Func<Element, bool> predicate) => document.Root.All(predicate);

    public static bool All(this ParentTag root, Func<Element, bool> predicate) => All(root.Child, predicate);

    public static bool All(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        All(attributes.First, el => predicate((Attr)el));

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

    public static bool Any(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        Any(attributes.First, el => predicate((Attr)el));

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

    public static bool Any(this in Attr.List attributes) => attributes.First is not null;

    public static int Count(this Document document, Func<Element, bool> predicate) => document.Root.Count(predicate);

    public static int Count(this ParentTag root, Func<Element, bool> predicate) => Count(root.Child, predicate);

    public static int Count(this in Attr.List attributes, Func<Attr, bool> predicate) =>
        Count(attributes.First, el => predicate((Attr)el));

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

    public static int Count(this in Attr.List attributes) => Count(attributes.First);

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