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

    public partial class Document : IDocument, IEnumerable<Element>, ICloneable
    {
        private static readonly LRUCache<string, PathQuery> pathQueryCache = new(16);
        private readonly DocumentRoot root;

        internal Document(DocumentRoot root)
        {
            this.root = root;
        }

        public static readonly HtmlParser Html = HtmlParser.CreateConcurrent();

        public static readonly XmlParser Xml = XmlParser.CreateConcurrent();

        public bool IsComplete => this.root.IsWellFormed.HasValue;

        public bool IsWellFormed => this.root.IsWellFormed == true;

        public bool IsSerialized => this.root.IsSerialized;

        public int Length => this.root.Length;

        internal DocumentRoot Root => this.root;

        public override string? ToString()
        {
            return this.root.ToString();
        }

        public Document Clone()
        {
            return new Document(this.root.Clone());
        }

        object ICloneable.Clone() => Clone();

        public Element? Find(Predicate<Element> match)
        {
            return this.root.Find(match);
        }

        public TElement? Find<TElement>(Func<TElement, bool> match)
            where TElement : Element
        {
            return this.root.Find(match);
        }

        public IEnumerable<Element> FindAll(Predicate<Element> match)
        {
            return this.root.FindAll(match);
        }

        public IEnumerable<TElement> FindAll<TElement>(Func<TElement, bool> match)
            where TElement : Element
        {
            return this.root.FindAll(match);
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