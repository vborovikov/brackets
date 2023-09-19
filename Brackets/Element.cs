namespace Brackets
{
    using System;
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

        protected Element(int index)
        {
            this.prev = this.next = this;
            this.Index = index;
        }

        public virtual ElementLevel Level => ElementLevel.Inline;

        public int Index { get; }

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

        protected internal abstract string ToDebugString();

        protected static Element? Link(Element element, Tag parent, Element? sibling)
        {
            if (element.parent is not null)
                throw new InvalidOperationException();
            element.parent = parent;

            if (sibling is null)
            {
                sibling = element;
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

        protected static Element? Unlink(Element element, Tag parent, Element? sibling)
        {
            if (element.parent is null || element.parent != parent)
                throw new InvalidOperationException();

            if (sibling is null)
                throw new InvalidOperationException();

            if (element.next == element)
            {
                sibling = null;
            }
            else
            {
                var elementPrev = element.prev;
                var elementNext = element.next;

                if (sibling == element)
                {
                    sibling = elementNext;
                }

                elementNext.prev = elementPrev;
                elementPrev.next = elementNext;
            }

            element.prev = element.next = element;
            element.parent = null;

            return sibling;
        }

        protected internal Tag? Parent => this.parent;

        protected internal Element Next => this.next;

        protected internal Element Prev => this.prev;
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