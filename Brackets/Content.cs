namespace Brackets
{
    using System;

    public abstract class CharacterData : Element
    {
        protected CharacterData(int start)
            : base(start) { }

        public override string ToString()
        {
            return this.Source[this.Start..this.End].ToString();
        }

        internal override string ToDebugString()
        {
            return String.Concat(this.Source[this.Start..].TrimStart()[..Math.Min(15, this.Length)], "\u2026");
        }

        public bool Contains(ReadOnlySpan<char> text)
        {
            return this.Source[this.Start..this.End].Contains(text, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public class Content : CharacterData
    {
        private int end;

        public Content(int start, int length) : base(start)
        {
            this.end = start + length;
        }

        public sealed override int End => this.end;

        internal virtual bool TryAdd(Content content)
        {
            if (content.Start == this.End)
            {
                this.end = content.end;
                return true;
            }

            return false;
        }
    }

    public class Section : CharacterData
    {
        private readonly int dataStart;
        private readonly int dataLength;

        public Section(int start, int length, int dataStart, int dataLength)
            : base(start)
        {
            this.End = start + length;
            this.dataStart = dataStart;
            this.dataLength = dataLength;
        }

        public override int End { get; }

        public ReadOnlySpan<char> Data => this.Source.Slice(this.dataStart, this.dataLength);

        public override string ToString()
        {
            return this.Data.ToString();
        }
    }

    public sealed class Comment : CharacterData
    {
        public Comment(int start, int length) : base(start)
        {
            this.End = start + length;
        }

        public override int End { get; }
    }
}
