namespace Brackets.Primitives
{
    using System;

    public readonly ref struct TagSpan
    {
        public TagSpan(ReadOnlySpan<char> span, int index, TagCategory category)
        {
            this.Span = span;
            this.Index = index;
            this.Category = category;
            this.Name = ReadOnlySpan<char>.Empty;
        }

        public ReadOnlySpan<char> Span { get; }
        public int Index { get; }
        public TagCategory Category { get; }
        public ReadOnlySpan<char> Name { get; init; }

        public bool IsEmpty => this.Category == TagCategory.Content && (this.Span.IsEmpty || this.Span.IsWhiteSpace());

        public void Deconstruct(out ReadOnlySpan<char> span, out TagCategory category)
        {
            span = this.Span;
            category = this.Category;
        }

        public static implicit operator ReadOnlySpan<char>(TagSpan tag) => tag.Span;
    }

}
