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

        public override string ToString()
        {
            return this.Source[this.Start..this.End].ToString();
        }

        internal override string ToDebugString()
        {
            return String.Concat(this.Source[this.Start..].TrimStart()[..Math.Min(15, this.Length)], "\u2026");
        }

        internal virtual bool TryAdd(Content content)
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

    public class Section : Content
    {
        private readonly int dataStart;
        private readonly int dataLength;

        public Section(int start, int length, int dataStart, int dataLength)
            : base(start, length)
        {
            this.dataStart = dataStart;
            this.dataLength = dataLength;
        }

        public ReadOnlySpan<char> Data => this.Source.Slice(this.dataStart, this.dataLength);

        public override string? ToString()
        {
            return this.Data.ToString();
        }

        internal override bool TryAdd(Content content) => false;
    }
}
