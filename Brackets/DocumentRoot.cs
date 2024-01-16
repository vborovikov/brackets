namespace Brackets
{
    using System;

    abstract class DocumentRoot : ParentTag
    {
        protected DocumentRoot(RootRef rootReference, int length)
            : base(rootReference, 0, length) { }

        internal bool? IsWellFormed { get; set; }

        public override string ToString()
        {
            return string.Concat(this);
        }

        public override Element Clone()
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
    }

    sealed class EmptyDocumentRoot : DocumentRoot
    {
        public EmptyDocumentRoot(RootRef rootReference)
            : base(rootReference, 0) { }

        protected override ReadOnlySpan<char> Source => ReadOnlySpan<char>.Empty;
    }
}
