namespace Brackets;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

public class Tag : Element
{
    private readonly TagRef reference;
    private readonly Attr.List attrList;
    private Attr? attribute;
    private int length;

    public Tag(TagRef reference, int offset, int length) : base(offset)
    {
        this.reference = reference;
        this.attrList = new Attr.List(this);
        this.length = length;
    }

    internal TagRef Reference => this.reference;

    internal Attr? FirstAttribute => this.attribute;

    public new ParentTag? Parent => base.Parent as ParentTag;

    public string Name => this.reference.Name;

    public override FlowLayout Layout => this.reference.Layout;

    public override ContentCategory Category => this.reference.Category;

    public ContentCategory PermittedContent => this.reference.PermittedContent;

    public bool HasAttributes => this.attribute is not null;

    public ref readonly Attr.List Attributes => ref this.attrList;

    public Attr.Enumerator EnumerateAttributes() => new(this.attribute);

    public sealed override int Length => this.length;

    public override string ToString() => this.reference.ToString(this);

    public new Tag Clone() => (Tag)CloneOverride();

    protected override Element CloneOverride()
    {
        var tag = new Tag(this.reference, this.Offset, this.Length);

        CloneAttributes(tag);

        return tag;
    }

    protected void CloneAttributes(Tag tag)
    {
        if (this.attribute is null)
            return;

        var attr = this.attribute;
        do
        {
            tag.AddAttribute(attr.Clone());
            attr = (Attr)attr.Next;
        } while (attr != this.attribute);
    }

    public void AddAttribute(Attr attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        this.attribute = (Attr?)Link(attribute, this, this.attribute);
    }

    public void RemoveAttribute(Attr attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        if (this.attribute is null)
            throw new InvalidOperationException("Cannot remove an attribute when the tag has no attributes.");

        this.attribute = (Attr?)Unlink(attribute, this, this.attribute);
    }

    public void ReplaceAttribute(Attr? oldAttribute, Attr newAttribute)
    {
        ArgumentNullException.ThrowIfNull(newAttribute);
        if (oldAttribute == newAttribute)
            throw new ArgumentException("Cannot replace an attribute with itself.");

        var next = this.attribute;
        if (oldAttribute is not null)
        {
            if (this.attribute is null)
                throw new InvalidOperationException("Cannot replace an attribute when the tag has no attributes.");

            next = (Attr)oldAttribute.Next;
            RemoveAttribute(oldAttribute);
        }

        if (this.attribute is null)
        {
            AddAttribute(newAttribute);
        }
        else
        {
            Link(newAttribute, this, next);
        }
    }

    public Attr? FindAttribute(Predicate<Attr> predicate)
    {
        if (this.attribute is Attr first)
        {
            var current = first;
            do
            {
                if (predicate(current))
                    return current;
                current = (Attr)current.Next;
            } while (current != first);
        }

        return null;
    }

    internal override string ToDebugString()
    {
        var debug = new StringBuilder();

        debug.Append('<').Append(this.Name).Append(' ');
        if (this.HasAttributes)
        {
            foreach (var attr in EnumerateAttributes())
            {
                debug.Append(attr.ToDebugString()).Append(' ');
            }
            debug.Length -= 1;
        }
        debug.Append("/>");

        return debug.ToString();
    }

    internal virtual void CloseAt(int end)
    {
        this.length = end - this.Offset;
    }
}

public sealed class Instruction : Tag
{
    public Instruction(TagRef reference, int offset, int length) : base(reference, offset, length)
    {
    }

    public new Instruction Clone() => (Instruction)CloneOverride();

    protected override Element CloneOverride()
    {
        var tag = new Instruction(this.Reference, this.Offset, this.Length);

        CloneAttributes(tag);

        return tag;
    }

    internal override string ToDebugString() => $"<?{this.Name}?>";
}

public sealed class Declaration : Tag
{
    public Declaration(TagRef reference, int offset, int length) : base(reference, offset, length)
    {
    }

    public new Declaration Clone() => (Declaration)CloneOverride();

    protected override Element CloneOverride()
    {
        var tag = new Declaration(this.Reference, this.Offset, this.Length);

        CloneAttributes(tag);

        return tag;
    }

    internal override string ToDebugString() => $"<!{this.Name}>";
}

public class ParentTag : Tag, IRoot, IEnumerable<Element>
{
    private Element? child;

    public ParentTag(TagRef reference, int offset, int length) : base(reference, offset, length)
    {
    }

    internal bool HasRawContent => this.Reference.ParsingMode == ParsingMode.RawContent;

    internal bool PreservesFormatting => this.Reference.ParsingMode == ParsingMode.Formatting || (this.Parent?.PreservesFormatting ?? false);

    /// <summary>
    /// Indicates whether this parent tag has children.
    /// </summary>
    public bool HasChildren => this.child is not null;

    /// <summary>
    /// Indicates whether this parent tag has a single child.
    /// </summary>
    public bool HasOneChild => this.child is not null && this.child == this.child.Next;

    public int ContentStart => this.child?.Start ?? -1;

    public int ContentEnd => this.child?.Prev?.End ?? -1;

    protected internal Element? Child => this.child;

    public new ParentTag Clone() => (ParentTag)CloneOverride();

    protected override Element CloneOverride()
    {
        var parentTag = new ParentTag(this.Reference, this.Offset, this.Length);

        CloneAttributes(parentTag);
        CloneElements(parentTag);

        return parentTag;
    }

    protected void CloneElements(ParentTag parentTag)
    {
        if (this.child is null)
            return;

        var element = this.child;
        do
        {
            parentTag.Add(element.Clone());
            element = element.Next;
        } while (element != this.child);
    }

    public void Add(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element is Attr)
            throw new InvalidOperationException("Cannot add an attribute to a parent tag.");
        if (element == this)
            throw new ArgumentException("Cannot add an element to itself.");

        if (this.HasRawContent && element is Content content && this.child is not null)
        {
            var childElement = this.child;
            do
            {
                childElement = childElement.Prev;
                if (childElement is Content childContent && childContent.TryConcat(content))
                {
                    return;
                }
            } while (childElement != this.child);
        }

        this.child = Link(element, this, this.child);
    }

    public void Remove(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element is Attr)
            throw new InvalidOperationException("Cannot remove an attribute from a parent tag.");
        if (element == this)
            throw new ArgumentException("Cannot remove an element from itself.");
        if (this.child is null)
            throw new InvalidOperationException("Cannot remove an element when the parent has no children.");

        this.child = Unlink(element, this, this.child);
    }

    public void Replace(Element? oldElement, Element newElement)
    {
        ArgumentNullException.ThrowIfNull(newElement);
        if (newElement is Attr || oldElement is Attr)
            throw new InvalidOperationException("Cannot replace an attribute.");
        if (oldElement == newElement)
            throw new ArgumentException("Cannot replace an element with itself.");
        if (oldElement == this)
            throw new ArgumentException("Cannot replace the parent element.");
        if (newElement == this)
            throw new ArgumentException("Cannot replace an element with the parent element.");

        var next = this.child;
        var isFirst = false;
        if (oldElement is not null)
        {
            if (this.child is null)
                throw new InvalidOperationException("Cannot replace an element when the parent has no children.");

            next = oldElement.Next;
            isFirst = oldElement == this.child;
            Remove(oldElement);
        }

        if (this.child is null)
        {
            Add(newElement);
        }
        else
        {
            Link(newElement, this, next);
            if (isFirst)
            {
                this.child = newElement;
            }
        }
    }

    public Enumerator GetEnumerator() => new(this.child);

    IEnumerator<Element> IEnumerable<Element>.GetEnumerator() =>
        this.child is null ? ((IEnumerable<Element>)Array.Empty<Element>()).GetEnumerator() : GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable<Element>)this).GetEnumerator();

    public TElement? Find<TElement>(Func<TElement, bool> match)
        where TElement : Element
    {
        var elements = new DescendantEnumerator<TElement>(this, match);
        if (elements.MoveNext())
            return elements.Current;

        return null;
    }

    public Element? Find(Predicate<Element> match) => Find<Element>(el => match(el));

    public DescendantEnumerator<Element> FindAll(Predicate<Element> match) => new(this, el => match(el));

    public DescendantEnumerator<TElement> FindAll<TElement>(Func<TElement, bool> match) where TElement : Element => new(this, match);

    public DescendantEnumerator<TElement> FindAll<TElement>() where TElement : Element => new(this, default);

    public override string ToString()
    {
        if (this.child is null)
            return string.Empty;

        return ToString(this);
    }

    public string ToString(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return ToString();

        // any format different from the default is compact markup

        //todo: implement these
        // X, x - XML, XHTML
        // H, h - HTML

        return DocumentRoot.ToText(this);
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString(format);

    internal override void CloseAt(int end)
    {
        if (this.child is not null &&
            this.child.Next != this.child.Prev &&
            this.child.Prev is Content content &&
            content.Data.IsWhiteSpace())
        {
            Remove(content);
        }

        base.CloseAt(end);
    }

    internal void Graft(ParentTag other, int unexpectedEndPos = -1)
    {
        var element = other.Prune();
        if (element is not null)
        {
            this.child = Graft(element, this, this.child);

            var newEnd = unexpectedEndPos > 0 ? unexpectedEndPos :
                this.child is not null ? this.child.Prev.End : -1;
            if (newEnd > 0)
            {
                CloseAt(newEnd);
            }
        }
    }

    private Element? Prune()
    {
        if (this.child is null)
            return null;

        var element = this.child;
        var hasContentBeforeTags = false;
        do
        {
            if (element is Tag tag)
            {
                if (!hasContentBeforeTags)
                {
                    // the first non-comment element element is a tag, we don't prune it
                    //todo: or maybe we want to find the first content element amoung children and prune the element from there
                    break;
                }

                this.child = Prune(tag, this.child);
                if (this.child is not null)
                {
                    CloseAt(this.child.Prev.End);
                }

                return tag;
            }
            hasContentBeforeTags = hasContentBeforeTags || element is not Comment; // and not a Tag
            element = element.Next;
        }
        while (element != this.child);

        return null;
    }

    internal override string ToDebugString()
    {
        var debug = new StringBuilder();

        debug.Append('<').Append(this.Name).Append(' ');
        if (this.HasAttributes)
        {
            foreach (var attr in EnumerateAttributes())
            {
                debug.Append(attr.ToDebugString()).Append(' ');
            }
            debug.Length -= 1;
        }
        debug.Append('>');
        if (this.HasChildren)
        {
            debug.Append('\u2026');
        }

        return debug.ToString();
    }

    public struct DescendantEnumerator<TElement> : IEnumerable<TElement>, IEnumerator<TElement>
        where TElement : Element
    {
        private readonly ParentTag root;
        private readonly Func<TElement, bool>? match;
        private ParentTag? parent;
        private Element? child;
        private TElement? current;

        internal DescendantEnumerator(ParentTag root, Func<TElement, bool>? match = default)
        {
            this.root = root;
            this.match = match;
            this.parent = root;
            this.child = root.child;
        }

        public readonly TElement Current => this.current!;
        public readonly DescendantEnumerator<TElement> GetEnumerator() => this;

        [MemberNotNullWhen(true, nameof(current))]
        public bool MoveNext()
        {
            while (this.child is not null)
            {
                // visit descendants
                while (this.child is ParentTag { child: not null } childAsParent)
                {
                    this.parent = childAsParent;
                    this.child = childAsParent.child;

                    if (childAsParent is TElement matchedChildAsParent && Match(matchedChildAsParent))
                    {
                        this.current = matchedChildAsParent;
                        return true;
                    }
                }

                var matchedChild = this.child as TElement;

                // visit siblings and ancestors
                for (this.child = this.child.Next;
                     this.parent != this.root && this.child == this.parent?.child;
                     this.child = this.child.Next)
                {
                    this.child = this.parent;
                    this.parent = (ParentTag?)this.child.Parent;
                }

                // check if traversal completed
                if (this.parent == this.root && this.child == this.root.child)
                {
                    this.child = null;
                }

                if (matchedChild is not null && Match(matchedChild))
                {
                    this.current = matchedChild;
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            this.parent = this.root;
            this.child = this.root.child;
            this.current = null;
        }

        public readonly void Dispose() { }
        readonly object IEnumerator.Current => this.Current;
        readonly IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private readonly bool Match(TElement element) => this.match?.Invoke(element) ?? true;
    }
}