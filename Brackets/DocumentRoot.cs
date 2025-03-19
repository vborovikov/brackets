namespace Brackets
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents the root of a document structure, providing methods to interact with its elements.
    /// </summary>
    public interface IRoot : IEnumerable<Element>, ICloneable, IFormattable
    {
        /// <summary>
        /// Gets the total length of the underlying data represented by this root.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the elements in the root.
        /// </summary>
        /// <returns>An <see cref="Element.Enumerator"/> that can be used to iterate through the elements.</returns>
        new Element.Enumerator GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the child elements in the root.
        /// </summary>
        /// <returns>An <see cref="Element.Enumerator"/> that can be used to iterate through the child elements.</returns>
        Element.Enumerator Enumerate();

        /// <summary>
        /// Returns an enumerator that iterates through the child elements in the root that match the specified predicate.
        /// </summary>
        /// <param name="match">The predicate that defines the conditions of the child elements to include.</param>
        /// <returns>An <see cref="Element.Enumerator{TElement}"/> that can be used to iterate through the matching child elements.</returns>
        Element.Enumerator<Element> Enumerate(Predicate<Element> match);

        /// <summary>
        /// Returns an enumerator that iterates through the child elements of the specified type.
        /// </summary>
        /// <typeparam name="TElement">The type of child elements to include.</typeparam>
        /// <returns>An <see cref="Element.Enumerator{TElement}"/> that can be used to iterate through the matching child elements.</returns>
        Element.Enumerator<TElement> Enumerate<TElement>() where TElement : Element;

        /// <summary>
        /// Returns an enumerator that iterates through the child elements of the specified type that match the given predicate.
        /// </summary>
        /// <typeparam name="TElement">The type of child elements to include.</typeparam>
        /// <param name="match">The predicate that defines the conditions of the child elements to include.</param>
        /// <returns>An <see cref="Element.Enumerator{TElement}"/> that can be used to iterate through the matching child elements.</returns>
        Element.Enumerator<TElement> Enumerate<TElement>(Func<TElement, bool> match) where TElement : Element;

        /// <summary>
        /// Finds the first element that matches the specified predicate.
        /// </summary>
        /// <param name="match">The predicate that defines the conditions of the element to search for.</param>
        /// <returns>The first <see cref="Element"/> that matches the predicate, or <c>null</c> if no match is found.</returns>
        Element? Find(Predicate<Element> match);

        /// <summary>
        /// Finds the first element of the specified type that matches the given predicate.
        /// </summary>
        /// <typeparam name="TElement">The type of element to search for.</typeparam>
        /// <param name="match">The predicate that defines the conditions of the element to search for.</param>
        /// <returns>The first element of type <typeparamref name="TElement"/> that matches the predicate, or <c>null</c> if no match is found.</returns>
        TElement? Find<TElement>(Func<TElement, bool> match) where TElement : Element;

        /// <summary>
        /// Finds all elements that match the specified predicate.
        /// </summary>
        /// <param name="match">The predicate that defines the conditions of the elements to search for.</param>
        /// <returns>A <see cref="ParentTag.DescendantEnumerator{Element}"/> that can be used to iterate through the matching elements.</returns>
        ParentTag.DescendantEnumerator<Element> FindAll(Predicate<Element> match);

        /// <summary>
        /// Finds all elements of the specified type that match the given predicate.
        /// </summary>
        /// <typeparam name="TElement">The type of elements to search for.</typeparam>
        /// <param name="match">The predicate that defines the conditions of the elements to search for.</param>
        /// <returns>A <see cref="ParentTag.DescendantEnumerator{TElement}"/> that can be used to iterate through the matching elements.</returns>
        ParentTag.DescendantEnumerator<TElement> FindAll<TElement>(Func<TElement, bool> match) where TElement : Element;

        /// <summary>
        /// Finds all elements of the specified type.
        /// </summary>
        /// <typeparam name="TElement">The type of elements to search for.</typeparam>
        /// <returns>A <see cref="ParentTag.DescendantEnumerator{TElement}"/> that can be used to iterate through the matching elements.</returns>
        ParentTag.DescendantEnumerator<TElement> FindAll<TElement>() where TElement : Element;

        /// <summary>
        /// Converts the root and its elements to a formatted string.
        /// </summary>
        /// <param name="format">A format string.</param>
        /// <returns>A string representation of the root and its elements.</returns>
        string ToString(string? format);
    }

    /// <summary>
    /// Represents a document, which is a specialized type of root.
    /// </summary>
    public interface IDocument : IRoot
    {
        /// <summary>
        /// Gets a value indicating whether the document is well-formed.
        /// </summary>
        /// <remarks>
        /// A well-formed document adheres to the structural rules of its document type (e.g., XML).
        /// </remarks>
        bool IsWellFormed { get; }

        /// <summary>
        /// Indicates whether the document elements are serialized copies of the source data.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, the elements represent copies of the original data.
        /// If <c>false</c>, the elements are directly linked to the original source data.
        /// </remarks>
        bool IsSerialized { get; }
    }

    interface IParent : IRoot
    {
        Element? Child { get; }
    }

    abstract class DocumentRoot : ParentTag, IDocument
    {
        protected DocumentRoot(RootRef rootReference, int length)
            : base(rootReference, 0, length) { }

        public bool? IsWellFormed { get; set; }

        bool IDocument.IsWellFormed => this.IsWellFormed == true;

        public abstract bool IsSerialized { get; }

        public override string ToString()
        {
            return string.Concat(this);
        }

        public new DocumentRoot Clone() => (DocumentRoot)CloneOverride();

        protected override Element CloneOverride()
        {
            var root = new EmptyDocumentRoot((RootRef)this.Reference);

            CloneElements(root);

            return root;
        }

        internal static string ToText(IRoot root)
        {
            var text = new StringBuilder(root.Length);

            switch (root)
            {
                case DocumentRoot:
                    goto default;
                case ParentTag parent:
                    Append(text, parent);
                    break;
                default:
                    Append(text, root.GetEnumerator());
                    break;
            }

            return text.ToString();
        }

        private static void Append(StringBuilder text, ParentTag parent)
        {
            text.Append('<').Append(parent.Name);
            if (parent.HasAttributes)
            {
                text.Append(' ');
                Append(text, parent.EnumerateAttributes());
            }
            text.Append('>');

            Append(text, parent.GetEnumerator());

            text.Append("</").Append(parent.Name).Append('>');
        }

        private static void Append(StringBuilder text, Enumerator elements)
        {
            foreach (var element in elements)
            {
                Append(text, element);
            }
        }

        private static void Append(StringBuilder text, Element element)
        {
            switch (element)
            {
                case ParentTag parent:
                    Append(text, parent);
                    break;
                case Tag tag:
                    text.Append('<').Append(tag.Name);
                    if (tag.HasAttributes)
                    {
                        text.Append(' ');
                        Append(text, tag.EnumerateAttributes());
                    }
                    text.Append("/>");
                    break;
                case Comment comment:
                    text.Append("<!--").Append(comment.Data).Append("-->");
                    break;
                case Section section:
                    text.Append("<[CDATA[").Append(section.Data).Append("]]>");
                    break;
                case Content content:
                    text.Append(content.Data);
                    break;
                default:
                    text.Append(element.ToString());
                    break;
            }
        }

        private static void Append(StringBuilder text, Attr.Enumerator attributes)
        {
            foreach (var attribute in attributes)
            {
                if (text.Length > 0 && text[^1] != ' ')
                {
                    text.Append(' ');
                }
                text.Append(attribute.Name);
                if (attribute.HasValue)
                {
                    text.Append('=').Append('"').Append(attribute.Value).Append('"');
                }
            }
        }
    }

    sealed class TextDocumentRoot : DocumentRoot
    {
        private readonly ReadOnlyMemory<char> text;

        public TextDocumentRoot(ReadOnlyMemory<char> text, RootRef rootReference)
            : base(rootReference, text.Length)
        {
            this.text = text;
        }

        protected override ReadOnlySpan<char> Source => this.text.Span;

        public override bool IsSerialized => false;
    }

    sealed class EmptyDocumentRoot : DocumentRoot
    {
        public EmptyDocumentRoot(RootRef rootReference)
            : base(rootReference, 0) { }

        protected override ReadOnlySpan<char> Source => ReadOnlySpan<char>.Empty;

        public override bool IsSerialized => true;
    }
}
