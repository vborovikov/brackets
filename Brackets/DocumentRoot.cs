namespace Brackets
{
    using System;

    public interface IRoot : IEnumerable<Element>, ICloneable
    {
        int Length { get; }

        Element.Enumerator GetEnumerator();
        Element? Find(Predicate<Element> match);
        TElement? Find<TElement>(Func<TElement, bool> match) where TElement : Element;
        ParentTag.DescendantEnumerator<Element> FindAll(Predicate<Element> match);
        ParentTag.DescendantEnumerator<TElement> FindAll<TElement>(Func<TElement, bool> match) where TElement : Element;
        ParentTag.DescendantEnumerator<TElement> FindAll<TElement>() where TElement : Element;
    }

    public interface IDocument : IRoot
    {
        bool IsWellFormed { get; }

        /// <summary>
        /// Indicates whether the document elements are serialized copies of the source data.
        /// </summary>
        bool IsSerialized { get; }
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
