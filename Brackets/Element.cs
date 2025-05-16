namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public enum FlowLayout
    {
        Inline,
        Block,
    }

    [DebuggerDisplay("{" + nameof(ToDebugString) + "(),nq}")]
    public abstract class Element : ICloneable //todo: ISpanFormattable
    {
        private Tag? parent;
        private Element prev;
        private Element next;

        protected Element(int offset)
        {
            this.prev = this.next = this;
            this.Offset = offset;
        }

        public virtual FlowLayout Layout => FlowLayout.Inline;

        public virtual ContentCategory Category => ContentCategory.Flow;

        public int Offset { get; }

        public abstract int Length { get; }

        internal int Start => this.Offset;

        internal int End => this.Offset + this.Length;

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

        public override string ToString() => string.Empty;

        /// <summary>
        /// Determines whether the current element is a descendant of the specified tag.
        /// </summary>
        /// <param name="tag">The tag to check for ancestry.</param>
        /// <returns><c>true</c> if the current element is a descendant of the specified tag; otherwise, <c>false</c>.</returns>
        public bool IsDescendantOf(Tag tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            if (tag == this) return false;

            for (var ancestor = this.parent; ancestor is not null; ancestor = ancestor.parent)
            {
                if (ancestor == tag)
                    return true;
            }

            return false;
        }

        public Element Clone() => CloneOverride();

        protected abstract Element CloneOverride();

        object ICloneable.Clone() => Clone();

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

                if (element.Layout == FlowLayout.Block)
                {
                    BreakLine();
                }
                text.Append(element.ToString());
                if (element.Layout == FlowLayout.Block)
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
                throw new InvalidOperationException("Element already has a parent.");
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
                throw new InvalidOperationException("Element does not have the expected parent.");

            if (sibling is null)
                throw new InvalidOperationException("Element does not have a sibling.");

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

        /// <summary>
        /// Enumerates the elements of a <see cref="ParentTag">parent tag</see>.
        /// </summary>
        public struct Enumerator : IEnumerable<Element>, IEnumerator<Element>
        {
            private readonly Element? last;
            private Element? current;

            internal Enumerator(Element? first)
            {
                this.last = first?.prev;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public readonly Element Current => this.current!;

            /// <summary>
            /// Returns this instance as an enumerator.
            /// </summary>
            public readonly Enumerator GetEnumerator() => this;

            /// <inheritdoc/>
            [MemberNotNullWhen(true, nameof(current))]
            public bool MoveNext()
            {
                if (this.current == this.last)
                    return false;

                if (this.current is null || this.current.parent is null || this.current.parent != this.last?.parent)
                {
                    // the enumeration has been reset or the element has been removed,
                    // in both cases we have to start from the beginning
                    this.current = this.last?.next;
                }
                else
                {
                    // move to the next element
                    this.current = this.current.next;
                }

                return this.current is not null;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                this.current = null;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }
            readonly object IEnumerator.Current => this.current!;
            readonly IEnumerator<Element> IEnumerable<Element>.GetEnumerator() => GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Enumerates the elements of a <see cref="ParentTag">parent tag</see> that match the given predicate.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements to enumerate.</typeparam>
        public struct Enumerator<TElement> : IEnumerable<TElement>, IEnumerator<TElement>
            where TElement : Element
        {
            private readonly Element? last;
            private readonly Func<TElement, bool>? match;
            private Element? current;

            internal Enumerator(Element? first, Func<TElement, bool>? match = default)
            {
                this.last = first?.prev;
                this.match = match;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public readonly TElement Current => (TElement)this.current!;

            /// <summary>
            /// Returns this instance as an enumerator.
            /// </summary>
            public readonly Enumerator<TElement> GetEnumerator() => this;

            /// <inheritdoc/>
            [MemberNotNullWhen(true, nameof(current))]
            public bool MoveNext()
            {
                do
                {
                    if (this.current == this.last)
                        return false;

                    if (this.current is null || this.current.parent is null || this.current.parent != this.last?.parent)
                    {
                        // the enumeration has been reset or the element has been removed,
                        // in both cases we have to start from the beginning
                        this.current = this.last?.next;
                    }
                    else
                    {
                        // move to the next element
                        this.current = this.current.next;
                    }
                } while (this.current is not TElement element || !Match(element));

                return this.current is not null;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                this.current = null;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }
            readonly object IEnumerator.Current => this.current!;
            readonly IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private readonly bool Match(TElement element) => this.match?.Invoke(element) ?? true;
        }
    }
}