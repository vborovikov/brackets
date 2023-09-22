namespace Brackets.Primitives
{
    using System;

    public readonly ref struct AttributeSpan
    {
        public AttributeSpan(ReadOnlySpan<char> span, int start, AttributeCategory category)
        {
            this.Span = span;
            this.Start = start;
            this.Category = category;
        }

        public ReadOnlySpan<char> Span { get; }
        public int Start { get; }
        public AttributeCategory Category { get; }

        public bool IsEmpty => this.Span.IsEmpty;

        public void Deconstruct(out ReadOnlySpan<char> span, out AttributeCategory category)
        {
            span = this.Span;
            category = this.Category;
        }

        public static implicit operator ReadOnlySpan<char>(AttributeSpan attr) => attr.Span;
    }
}
