namespace Brackets
{
    using System;

    public sealed class Root : ParentTag
    {
        private readonly ReadOnlyMemory<char> text;

        public Root(RootReference rootReference, ReadOnlyMemory<char> text)
            : base(rootReference, -1, -1)
        {
            this.text = text;
        }

        protected override ReadOnlySpan<char> Source => this.text.Span;

        public override string? ToString()
        {
            return String.Concat(this);
        }
    }
}
