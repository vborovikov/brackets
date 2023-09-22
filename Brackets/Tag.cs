﻿namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Tag : Element
    {
        private class AttributeCollection : IAttributeCollection
        {
            private readonly Tag owner;
            private Attribute? attribute;
            private int attributeCount;

            public AttributeCollection(Tag owner)
            {
                this.owner = owner;
            }

            public int Count => this.attributeCount;

            public override string? ToString()
            {
                return String.Join(' ', this);
            }

            public void Add(Attribute attribute)
            {
                this.attribute = (Attribute?)Link(attribute, this.owner, this.attribute);
                ++this.attributeCount;
            }

            public void Remove(Attribute attribute)
            {
                if (this.attribute is not null)
                {
                    --this.attributeCount;
                    this.attribute = (Attribute?)Unlink(attribute, this.owner, this.attribute);
                }
            }

            public IEnumerator<Attribute> GetEnumerator()
            {
                if (this.attribute is null)
                    yield break;

                var attr = this.attribute;
                do
                {
                    yield return attr;
                    attr = (Attribute)attr.Next;
                } while (attr != this.attribute);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        protected readonly TagReference reference;
        private int end;

        public Tag(TagReference reference, int start, int length) : base(start)
        {
            this.reference = reference;
            this.end = start + length;
            this.Attributes = new AttributeCollection(this);
        }

        public string Name => this.reference.Name;

        public bool HasRawContent => this.reference.HasRawContent;

        public override ElementLevel Level => this.reference.Level;

        public IAttributeCollection Attributes { get; }

        public sealed override int End => this.end;

        public override string? ToString() => this.reference.ToString(this);

        protected internal override string ToDebugString()
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

        internal bool IsClosed
        {
            get
            {
                var span = this.Source;
                if (this.child is null)
                {
                    span = span[this.Start..this.End]
                        .TrimEnd(this.reference.Syntax.Closer)
                        .TrimEnd(this.reference.Syntax.Separators);
                    return span.EndsWith(this.Name, this.reference.Syntax.Comparison);
                }

                span = span[this.child.Prev.End..]
                    .TrimStart(this.reference.Syntax.Separators)
                    .TrimStart(this.reference.Syntax.Opener)
                    .TrimStart(this.reference.Syntax.Terminator);
                return span.StartsWith(this.Name, this.reference.Syntax.Comparison);
            }
        }

        internal bool IsClosedBy(ReadOnlySpan<char> other)
        {
            if (this.reference.IsRoot)
            {
                // nothing is equal to the root tag
                return false;
            }

            if (other.IsEmpty)
                return false;

            var nameIdx = other.IndexOf(this.reference.Name, this.reference.Syntax.Comparison);
            return
                nameIdx == 0 ||
                (nameIdx == 2 && other[0] == this.reference.Syntax.Opener && other[1] == this.reference.Syntax.Terminator);
        }

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

        public override string? ToString()
        {
            if (this.child is null)
                return String.Empty;

            return ToString(this);
        }

        internal void Graft(ParentTag other)
        {
            var element = other.Prune();
            if (element is not null)
            {
                Graft(element);
            }
        }

        private Element? Prune()
        {
            if (this.child is null)
                return null;

            var tag = this.child;
            do
            {
                if (tag is Tag)
                {
                    this.child = Prune(tag, this.child);
                    return tag;
                }
                tag = tag.Next;
            }
            while (tag != this.child);

            return null;
        }

        private void Graft(Element child)
        {
            this.child = Graft(child, this, this.child);

            if (this.child is not null)
            {
                //todo: find the closing tag end position
                CloseAt(this.child.Prev.End);
            }
        }

        protected internal override string ToDebugString()
        {
            return $"<{this.Name}>\u2026</{this.Name}>";
        }
    }
}
