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

        public Tag(TagReference reference, int index, int end) : base(index)
        {
            this.reference = reference;
            this.end = end;
            this.Attributes = new AttributeCollection(this);
        }

        public string Name => this.reference.Name;

        public bool HasRawContent => this.reference.HasRawContent;

        public override ElementLevel Level => this.reference.Level;

        public IAttributeCollection Attributes { get; }

        public int End => this.end;

        public override string? ToString() => this.reference.ToString(this);

        protected internal override string ToDebugString()
        {
            return $"<{this.Name}/>";
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

        internal void CloseAt(int index)
        {
            this.end = index;
        }
    }

    public class ParentTag : Tag, IEnumerable<Element>
    {
        private Element? child;

        public ParentTag(TagReference reference, int index, int length) : base(reference, index, length)
        {
        }

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

        public IEnumerator<Element> GetEnumerator()
        {
            if (this.child is null)
                yield break;

            var element = this.child;
            do
            {
                yield return element;
                element = element.Next;
            } while (element != this.child);
        }

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

        internal Element? Abandon()
        {
            if (this.child is null)
                return null;

            return Exclude(this.child);
        }

        internal void Adopt(Element child)
        {
            this.child = Include(child, this, this.child);
        }

        protected internal override string ToDebugString()
        {
            return $"<{this.Name}>\u2026</{this.Name}>";
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
