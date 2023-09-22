namespace Brackets.Primitives
{
    using System;

    public readonly ref struct TagSpan
    {
        public TagSpan(ReadOnlySpan<char> span, int start, TagCategory category)
        {
            this.Span = span;
            this.Start = start;
            this.Category = category;
            this.Name = ReadOnlySpan<char>.Empty;
        }

        public ReadOnlySpan<char> Span { get; }
        public int Start { get; }
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
