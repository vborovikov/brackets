namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Tag : Element
    {
        protected readonly TagReference reference;
        private Attribute? attribute;
        private int end;

        public Tag(TagReference reference, int start, int length) : base(start)
        {
            this.reference = reference;
            this.end = start + length;
        }

        public string Name => this.reference.Name;

        public bool HasRawContent => this.reference.HasRawContent;

        public override ElementLevel Level => this.reference.Level;

        public bool HasAttributes => this.attribute is not null;

        public Attribute.Enumerator EnumerateAttributes() => new(this.attribute);

        public sealed override int End => this.end;

        public override string ToString() => this.reference.ToString(this);

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

        internal override string ToDebugString()
        {
            return $"<{this.Name}/>";
        }

        internal void CloseAt(int end)
        {
            this.end = end;
        }
    }

    public class ParentTag : Tag, IEnumerable<Element>
    {
        private Element? child;

        public ParentTag(TagReference reference, int start, int length) : base(reference, start, length)
        {
        }

        public int ContentStart => this.child?.Start ?? -1;

        public int ContentEnd => this.child?.Prev?.End ?? -1;

        protected internal Element? Child => this.child;

        public void Add(Element element)
        {
            if (this.HasRawContent && element is Content content && this.child is not null)
            {
                var childElement = this.child;
                do
                {
                    childElement = childElement.Prev;
                    if (childElement is Content childContent && childContent.TryAdd(content))
                    {
                        return;
                    }
                } while (childElement != this.child);
            }

            this.child = Link(element, this, this.child);
        }

        public Enumerator GetEnumerator() => new(this.child);

        IEnumerator<Element> IEnumerable<Element>.GetEnumerator() =>
            this.child is null ? ((IEnumerable<Element>)Array.Empty<Element>()).GetEnumerator() : GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable<Element>)this).GetEnumerator();

        public void Remove(Element element)
        {
            if (this.child is not null)
            {
                this.child = Unlink(element, this, this.child);
            }
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

        internal override string ToDebugString()
        {
            return $"<{this.Name}>\u2026</{this.Name}>";
        }
    }
}
