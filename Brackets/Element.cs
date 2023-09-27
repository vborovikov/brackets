namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public enum ElementLevel
    {
        Inline,
        Block,
    }

    [DebuggerDisplay("{" + nameof(ToDebugString) + "(),nq}")]
    public abstract class Element
    {
        private Tag? parent;
        private Element prev;
        private Element next;

        protected Element(int start)
        {
            this.prev = this.next = this;
            this.Start = start;
        }

        public virtual ElementLevel Level => ElementLevel.Inline;

        public int Start { get; }

        public abstract int End { get; }

        public int Offset => this.Start;

        public int Length => this.End - this.Start;

        public int NestingLevel
        {
            get
            {
                var level = 0;

                for (Element? ancestor = this.parent; ancestor is not null; ancestor = ancestor.parent)
                {
                    ++level;
                }

                return level;
            }
        }

        protected virtual ReadOnlySpan<char> Source => this.parent is null ? ReadOnlySpan<char>.Empty : this.parent.Source;

        public static string ToString(IEnumerable<Element> elements)
        {
            var text = new StringBuilder();
            var lineBreakPos = 0;

            var currentParent = default(Element);
            foreach (var element in elements)
            {
                if (element.parent != currentParent)
                {
                    currentParent = element.parent;
                    BreakLine();
                }

                if (element.Level == ElementLevel.Block)
                {
                    BreakLine();
                }
                text.Append(element.ToString());
                if (element.Level == ElementLevel.Block)
                {
                    BreakLine();
                }
            }

            return text.ToString();

            void BreakLine()
            {
                if (text.Length > lineBreakPos)
                {
                    text.AppendLine();
                    lineBreakPos = text.Length;
                }
            }
        }

        public virtual bool TryGetValue<T>([MaybeNullWhen(false)] out T value) => TryGetValue(ToString(), out value);

        protected static bool TryGetValue<T>(object? original, [MaybeNullWhen(false)] out T value)
        {
            if (original is null)
            {
                value = default;
                return false;
            }
            if (original is T variable)
            {
                value = variable;
                return true;
            }

            try
            {
                //Handling Nullable types i.e, int?, double?, bool? .. etc
                if (Nullable.GetUnderlyingType(typeof(T)) is not null)
                {
                    var obj = TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(original);
                    if (obj is T convertedValue)
                    {
                        value = convertedValue;
                        return true;
                    }
                }
                else
                {
                    value = (T)Convert.ChangeType(original, typeof(T));
                    return true;
                }
            }
            catch
            {
            }

            value = default;
            return false;
        }

        internal abstract string ToDebugString();

        protected static Element Link(Element element, Tag parent, Element? sibling)
        {
            if (element.parent is not null)
                throw new InvalidOperationException();
            element.parent = parent;

            if (sibling is null)
            {
                sibling = element;
                sibling.prev = sibling.next = sibling;
            }
            else
            {
                var lastChild = sibling.prev;
                element.prev = lastChild;
                lastChild.next = element;
                element.next = sibling;
                sibling.prev = element;
            }

            return sibling;
        }

        protected static Element? Unlink(Element element, Tag parent, Element sibling)
        {
            if (element.parent is null || element.parent != parent)
                throw new InvalidOperationException();

            if (sibling is null)
                throw new InvalidOperationException();

            var child = sibling;
            if (element.next == element)
            {
                child = null;
            }
            else
            {
                var elementPrev = element.prev;
                var elementNext = element.next;

                if (sibling == element)
                {
                    child = elementNext;
                }

                elementNext.prev = elementPrev;
                elementPrev.next = elementNext;
            }

            element.prev = element.next = element;
            element.parent = null;

            return child;
        }

        protected static Element? Prune(Element element, Element sibling)
        {
            if (element == sibling)
            {
                element.parent = null;
                return null;
            }

            var lastElement = sibling.prev;
            var lastSibling = element.prev;

            lastSibling.next = sibling;
            sibling.prev = lastSibling;

            lastElement.next = element;
            element.prev = lastElement;

            return sibling;
        }

        protected static Element Graft(Element child, Tag parent, Element? sibling)
        {
            // connect
            if (sibling is null)
            {
                sibling = child;
            }
            else
            {
                var lastChild = sibling.prev;
                var lastAdopted = child.prev;
                lastChild.next = child;
                lastAdopted.next = sibling;
                sibling.prev = lastAdopted;
            }

            // adopt
            var el = child;
            do
            {
                el.parent = parent;
                el = el.next;
            } while (el != sibling);

            return sibling;
        }

        public Tag? Parent => this.parent;

        protected internal Element Next => this.next;

        protected internal Element Prev => this.prev;

        public struct Enumerator : IEnumerator<Element>
        {
            private readonly Element? first;
            private Element? node;
            private Element? current;

            internal Enumerator(Element? node)
            {
                this.first = node;
                this.node = node;
            }

            public readonly Element Current => this.current!;
            readonly object IEnumerator.Current => this.current!;

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.node is null)
                    return false;

                this.current = this.node;
                this.node = this.node.next;
                if (this.node == this.first)
                {
                    this.node = null;
                }

                return true;
            }

            public void Reset()
            {
                this.node = this.first;
                this.current = null;
            }
        }
    }

    public static class ElementExtensions
    {
        public static bool Contains(this Element element, ReadOnlySpan<char> text)
        {
            if (element is Attribute attribute)
                return attribute.HasValue && attribute.Value.Contains(text, StringComparison.CurrentCultureIgnoreCase);

            if (element is Content content)
                return content.Contains(text);

            if (element is IEnumerable<Element> parent)
            {
                foreach (var child in parent)
                {
                    if (child is Content childContent && childContent.Contains(text))
                        return true;
                }
            }

            return false;
        }
    }
}