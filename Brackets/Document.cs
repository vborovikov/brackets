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

    public partial class Document : IEnumerable<Element>
    {
        private static readonly LRUCache<string, PathQuery> pathQueryCache = new(10);
        private readonly DocumentRoot root;

        private Document(DocumentRoot root)
        {
            this.root = root;
        }

        public static readonly HtmlReference Html = new();

        public static readonly XmlReference Xml = new();

        public bool IsComplete => this.root.IsWellFormed.HasValue;

        public bool IsWellFormed => this.root.IsWellFormed == true;

        public int Length => this.root.Length;

#if DEBUG
        internal DocumentRoot Root => this.root;
#endif

        public override string? ToString()
        {
            return this.root.ToString();
        }

        public Element? Find(Predicate<Element> match)
        {
            return this.root.Find(match);
        }

        public TElement? Find<TElement>(Func<TElement, bool> match)
            where TElement : Element
        {
            return this.root.Find(el => el is TElement element && match(element)) as TElement;
        }

        public IEnumerable<Element> FindAll(Predicate<Element> match)
        {
            return this.root.FindAll(match);
        }

        public IEnumerable<TElement> FindAll<TElement>(Func<TElement, bool> match)
            where TElement : Element
        {
            return this.root.FindAll(el => el is TElement element && match(element)).Cast<TElement>();
        }

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