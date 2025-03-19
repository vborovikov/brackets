namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Collections;
    using Html;
    using Xml;
    using XPath;

    public partial class Document : IDocument, IParent
    {
        private static readonly LRUCache<string, PathQuery> pathQueryCache = new(16);
        private readonly DocumentRoot root;

        internal Document(DocumentRoot root)
        {
            this.root = root;
        }

        public static readonly HtmlParser Html = HtmlParser.CreateConcurrent();

        public static readonly XmlParser Xml = XmlParser.CreateConcurrent();

        public static readonly XhtmlParser Xhtml = XhtmlParser.CreateConcurrent();

        public bool IsComplete => this.root.IsWellFormed.HasValue;

        public bool IsWellFormed => this.root.IsWellFormed == true;

        public bool IsSerialized => this.root.IsSerialized;

        /// <inheritdoc/>
        public int Length => this.root.Length;

        internal DocumentRoot Root => this.root;

        Element? IParent.Child => this.root.Child;

        public override string ToString() => this.root.ToString();

        public string ToString(string? format) => this.root.ToString(format);

        public string ToString(string? format, IFormatProvider? formatProvider) => this.root.ToString(format, formatProvider);

        public Document Clone() => new(this.root.Clone());

        object ICloneable.Clone() => Clone();

        /// <inheritdoc/>
        public Element.Enumerator Enumerate() =>
            this.root.Enumerate();

        /// <inheritdoc/>
        public Element.Enumerator<Element> Enumerate(Predicate<Element> match) =>
            this.root.Enumerate(match);

        /// <inheritdoc/>
        public Element.Enumerator<TElement> Enumerate<TElement>() where TElement : Element =>
            this.root.Enumerate<TElement>();

        /// <inheritdoc/>
        public Element.Enumerator<TElement> Enumerate<TElement>(Func<TElement, bool> match) where TElement : Element =>
            this.root.Enumerate(match);

        /// <inheritdoc/>
        public Element? Find(Predicate<Element> match) =>
            this.root.Find(match);

        /// <inheritdoc/>
        public TElement? Find<TElement>(Func<TElement, bool> match) where TElement : Element =>
            this.root.Find(match);

        /// <inheritdoc/>
        public ParentTag.DescendantEnumerator<Element> FindAll(Predicate<Element> match) =>
            this.root.FindAll(match);

        /// <inheritdoc/>
        public ParentTag.DescendantEnumerator<TElement> FindAll<TElement>(Func<TElement, bool> match) where TElement : Element =>
            this.root.FindAll(match);

        /// <inheritdoc/>
        public ParentTag.DescendantEnumerator<TElement> FindAll<TElement>() where TElement : Element =>
            this.root.FindAll<TElement>();

        public IEnumerable<Element> Find(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var pathQuery = pathQueryCache.GetOrAdd(path, path =>
            {
                var pathParser = new XPathParser<PathQuery>();
                var pathScanner = new XPathScanner(path);
                return pathParser.Parse(pathScanner, PathQueryBuilder.Instance, LexKind.Eof);
            });

            return pathQuery.Run(this);
        }

        public Element.Enumerator GetEnumerator() => this.root.GetEnumerator();

        IEnumerator<Element> IEnumerable<Element>.GetEnumerator() =>
            ((IEnumerable<Element>)this.root).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable<Element>)this.root).GetEnumerator();
    }

    public static class DocumentExtensions
    {
        public static bool TryFindString(this Document document, string path, [MaybeNullWhen(false)] out string str)
        {
            str = default;
            if (String.IsNullOrWhiteSpace(path))
                return false;

            var elements = document.Find(path);
            return elements.FirstOrDefault()?.TryGetValue(out str) == true;
        }

        public static string FindString(this Document document, string path)
        {
            return document.TryFindString(path, out var str) ? str : String.Empty;
        }

        public static string[] FindStrings(this Document document, string path)
        {
            var elements = document.Find(path);
            return elements
                .Select(el => el.TryGetValue<string>(out var str) ? str : null)
                .Where(str => str is not null)
                .ToArray()!;
        }
    }
}