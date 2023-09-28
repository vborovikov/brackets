namespace Brackets
{
    using System;

    sealed class DocumentRoot : ParentTag
    {
        private readonly ReadOnlyMemory<char> text;

        public DocumentRoot(RootReference rootReference, ReadOnlyMemory<char> text)
            : base(rootReference, -1, -1)
        {
            this.text = text;
        }

        protected override ReadOnlySpan<char> Source => this.text.Span;

        internal bool? IsWellFormed { get; set; }

        public override string? ToString()
        {
            return String.Concat(this);
        }
    }
}
