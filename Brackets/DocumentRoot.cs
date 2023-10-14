namespace Brackets
{
    using System;

    abstract class DocumentRoot : ParentTag
    {
        protected DocumentRoot(RootReference rootReference, int length)
            : base(rootReference, 0, length) { }

        internal bool? IsWellFormed { get; set; }

        public override string ToString()
        {
            return string.Concat(this);
        }
    }

    sealed class TextDocumentRoot : DocumentRoot
    {
        private readonly ReadOnlyMemory<char> text;

        public TextDocumentRoot(ReadOnlyMemory<char> text, RootReference rootReference)
            : base(rootReference, text.Length)
        {
            this.text = text;
        }

        protected override ReadOnlySpan<char> Source => this.text.Span;
    }

    sealed class EmptyDocumentRoot : DocumentRoot
    {
        public EmptyDocumentRoot(RootReference rootReference)
            : base(rootReference, 0) { }

        protected override ReadOnlySpan<char> Source => ReadOnlySpan<char>.Empty;
    }
}
