namespace Brackets
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

            public Attribute.Enumerator GetEnumerator() => new(this.attribute);

            IEnumerator<Attribute> IEnumerable<Attribute>.GetEnumerator() =>
                this.attribute is null ? ((IEnumerable<Attribute>)Array.Empty<Attribute>()).GetEnumerator() : GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                ((IEnumerable<Attribute>)this).GetEnumerator();
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

        internal override string ToDebugString()
        {
            return $"<{this.Name}>\u2026</{this.Name}>";
        }
    }
}
