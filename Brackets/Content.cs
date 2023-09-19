namespace Brackets
{
    using System;

    public class Content : Element
    {
        private int length;

        public Content(int index, int length) : base(index)
        {
            this.length = length;
        }

        public override string? ToString()
        {
            return this.Source.Slice(this.Index, this.length).ToString();
        }

        protected internal override string ToDebugString()
        {
            return String.Concat(this.Source[this.Index..].TrimStart()[..Math.Min(3, this.length)], "\u2026");
        }

        public bool TryAdd(Content content)
        {
            if (content.Index == (this.Index + this.length))
            {
                this.length += content.length;
                return true;
            }

            return false;
        }

        public bool Contains(ReadOnlySpan<char> text)
        {
            return this.Source.Slice(this.Index, this.length).Contains(text, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
