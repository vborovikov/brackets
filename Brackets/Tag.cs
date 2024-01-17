namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public interface ITagAttributes
    {
    }

    public class Tag : Element, ITagAttributes
    {
        private readonly TagRef reference;
        private Attribute? attribute;
        private int end;

        public Tag(TagRef reference, int start, int length) : base(start)
        {
            this.reference = reference;
            this.end = start + length;
        }

        internal TagRef Reference => this.reference;

        internal Attribute? FirstAttribute => this.attribute;

        public string Name => this.reference.Name;

        public bool HasRawContent => this.reference.HasRawContent;

        public override ElementLevel Level => this.reference.Level;

        public bool HasAttributes => this.attribute is not null;

        public ITagAttributes Attributes => this;

        public Attribute.Enumerator EnumerateAttributes() => new(this.attribute);

        public sealed override int End => this.end;

        public override string ToString() => this.reference.ToString(this);

        public override Element Clone()
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
                tag.Add((Attribute)attr.Clone());
                attr = (Attribute)attr.Next;
            } while (attr != this.attribute);
        }

        public void Add(Attribute attribute)
        {
            this.attribute = (Attribute?)Link(attribute, this, this.attribute);
        }

        public void Remove(Attribute attribute)
        {
            if (this.attribute is not null)
            {
                this.attribute = (Attribute?)Unlink(attribute, this, this.attribute);
            }
        }

        internal override string ToDebugString() => $"<{this.Name}/>";

        internal void CloseAt(int end)
        {
            this.end = end;
        }
    }

    public sealed class Instruction : Tag
    {
        public Instruction(TagRef reference, int start, int length) : base(reference, start, length)
        {
        }

        public override Element Clone()
        {
            var tag = new Instruction(this.Reference, this.Offset, this.Length);

            CloneAttributes(tag);

            return tag;
        }

        internal override string ToDebugString() => $"<?{this.Name}?>";
    }

    public sealed class Declaration : Tag
    {
        public Declaration(TagRef reference, int start, int length) : base(reference, start, length)
        {
        }

        public override Element Clone()
        {
            var tag = new Declaration(this.Reference, this.Offset, this.Length);

            CloneAttributes(tag);

            return tag;
        }

        internal override string ToDebugString() => $"<!{this.Name}>";
    }

    public class ParentTag : Tag, IEnumerable<Element>
    {
        private Element? child;

        public ParentTag(TagRef reference, int start, int length) : base(reference, start, length)
        {
        }

        public int ContentStart => this.child?.Start ?? -1;

        public int ContentEnd => this.child?.Prev?.End ?? -1;

        protected internal Element? Child => this.child;

        public override Element Clone()
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
            if (element == this)
                throw new ArgumentException("Cannot remove an element from itself.");
            if (this.child is null)
                throw new InvalidOperationException("Cannot remove an element when the parent has no children.");

            this.child = Unlink(element, this, this.child);
        }

        public void Replace(Element oldElement, Element newElement)
        {
            ArgumentNullException.ThrowIfNull(oldElement);
            ArgumentNullException.ThrowIfNull(newElement);
            if (oldElement == newElement)
                throw new ArgumentException("Cannot replace an element with itself.");
            if (oldElement == this)
                throw new ArgumentException("Cannot replace the parent element.");
            if (newElement == this)
                throw new ArgumentException("Cannot replace an element with the parent element.");
            if (this.child is null)
                throw new InvalidOperationException("Cannot replace an element when the parent has no children.");

            var next = oldElement.Next;
            Remove(oldElement);

            if (this.child is null)
            {
                Add(newElement);
            }
            else
            {
                Link(newElement, this, next);
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
            return Find(el => el is TElement element && match(element)) as TElement;
        }

        public Element? Find(Predicate<Element> match)
        {
            foreach (var element in FindAll(match))
                return element;

            return null;
        }

        public IEnumerable<Element> FindAll(Predicate<Element> match)
        {
            if (this.child is null)
                yield break;

            var elementParent = this;
            var element = this.child;
            do
            {
                // go down
                while (element is ParentTag elementAsParent)
                {
                    if (elementAsParent.child is null)
                        break;

                    if (match(elementAsParent))
                        yield return elementAsParent;

                    elementParent = elementAsParent;
                    element = elementAsParent.child;
                }

                if (match(element))
                    yield return element;

                // go next or up
                for (element = element.Next;
                     elementParent != this && element == elementParent?.child;
                     element = element.Next)
                {
                    element = elementParent;
                    elementParent = (ParentTag?)element.Parent;
                }
            } while (elementParent != this || element != this.child);
        }

        public override string ToString()
        {
            if (this.child is null)
                return String.Empty;

            return ToString(this);
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
                        // a first non-comment element element is a tag, we don't prune it
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

        internal override string ToDebugString() => $"<{this.Name}>\u2026</{this.Name}>";
    }
}
