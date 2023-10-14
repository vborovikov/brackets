namespace Brackets
{
    using System;

    public abstract class CharacterData : Element
    {
        protected CharacterData(int start)
            : base(start) { }

        public virtual ReadOnlySpan<char> Data => this.Source[this.Start..this.End];

        public override string ToString()
        {
            return this.Data.ToString();
        }

        internal override string ToDebugString()
        {
            return String.Concat(this.Data.TrimStart()[..Math.Min(15, this.Length)], "\u2026");
        }

        public bool Contains(ReadOnlySpan<char> text)
        {
            return this.Data.Contains(text, StringComparison.CurrentCultureIgnoreCase);
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

    sealed class StreamContent : Content
    {
        private readonly string value;

        public StreamContent(string value, int offset)
            : base(offset, value.Length)
        {
            this.value = value;
        }

        protected override ReadOnlySpan<char> Source => this.value;
        public override ReadOnlySpan<char> Data => this.value;
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

        public override ReadOnlySpan<char> Data => this.Source.Slice(this.dataStart, this.dataLength);

        internal override string ToDebugString()
        {
            var name = this.Parent is ParentTag parent ? parent.Reference.Syntax.TrimName(base.Data) : ReadOnlySpan<char>.Empty;
            return $"<[{name}[{base.ToDebugString()}]]>";
        }
    }

    public sealed class Comment : CharacterData
    {
        public Comment(int start, int length) : base(start)
        {
            this.End = start + length;
        }

        public override int End { get; }

        public override ReadOnlySpan<char> Data =>
            this.Parent is ParentTag parent ? parent.Reference.Syntax.TrimData(base.Data) : base.Data;

        internal override string ToDebugString()
        {
            return $"<!--{base.ToDebugString()}-->";
        }
    }
}
