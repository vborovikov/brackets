namespace Brackets.Primitives
{
    using System;

    public readonly ref struct AttributeSpan
    {
        public AttributeSpan(ReadOnlySpan<char> span, int index, AttributeCategory category)
        {
            this.Span = span;
            this.Index = index;
            this.Category = category;
        }

        public ReadOnlySpan<char> Span { get; }
        public int Index { get; }
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
