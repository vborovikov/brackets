namespace Brackets
{
    using System;

    public sealed class Root : ParentTag
    {
        private readonly ReadOnlyMemory<char> markup;

        public Root(ReadOnlyMemory<char> markup)
            : base(RootReference.Default, -1)
        {
            this.markup = markup;
        }

        protected override ReadOnlySpan<char> Source => this.markup.Span;

        public override string ToString()
        {
            return String.Concat(this);
        }
    }
}
