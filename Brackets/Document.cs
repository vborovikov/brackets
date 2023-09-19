namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Html;
    using XPath;

    public partial class Document : IEnumerable<Element>
    {
        private readonly Root root;

        private Document(Root root)
        {
            this.root = root;
        }

        public static readonly HtmlReference Html = new HtmlReference();

#if DEBUG
        internal Root Root => this.root;
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

            var pathParser = new XPathParser<PathQuery>();
            var pathQuery = pathParser.Parse(path, PathQueryBuilder.Instance);

            return pathQuery.Run(this);
        }

        public IEnumerator<Element> GetEnumerator() => this.root.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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