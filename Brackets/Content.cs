namespace Brackets
{
    using System;

    public class Content : Element
    {
        private int end;

        public Content(int start, int length) : base(start)
        {
            this.end = start + length;
        }

        public sealed override int End => this.end;

        public override string? ToString()
        {
            return this.Source[this.Start..this.End].ToString();
        }

        protected internal override string ToDebugString()
        {
            return String.Concat(this.Source[this.Start..].TrimStart()[..Math.Min(3, this.Length)], "\u2026");
        }

        internal bool TryAdd(Content content)
        {
            if (content.Start == this.End)
            {
                this.end = content.end;
                return true;
            }

            return false;
        }

        public bool Contains(ReadOnlySpan<char> text)
        {
            return this.Source[this.Start..this.End].Contains(text, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
