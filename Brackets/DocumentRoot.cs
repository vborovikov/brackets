namespace Brackets
{
    using System;
    using System.Text;

    public interface IRoot : IEnumerable<Element>, ICloneable, IFormattable
    {
        int Length { get; }

        Element.Enumerator GetEnumerator();
        Element? Find(Predicate<Element> match);
        TElement? Find<TElement>(Func<TElement, bool> match) where TElement : Element;
        ParentTag.DescendantEnumerator<Element> FindAll(Predicate<Element> match);
        ParentTag.DescendantEnumerator<TElement> FindAll<TElement>(Func<TElement, bool> match) where TElement : Element;
        ParentTag.DescendantEnumerator<TElement> FindAll<TElement>() where TElement : Element;

        string ToString(string? format);
    }

    public interface IDocument : IRoot
    {
        bool IsWellFormed { get; }

        /// <summary>
        /// Indicates whether the document elements are serialized copies of the source data.
        /// </summary>
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
