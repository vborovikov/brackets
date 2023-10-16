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
            var data = this.Data.TrimStart();
            if (data.IsEmpty)
                return string.Empty;

            return string.Concat(data[..Math.Min(Math.Min(15, data.Length), this.Length)], "\u2026");
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

    sealed class StringContent : Content
    {
        private string value;

        public StringContent(string value, int offset)
            : base(offset, value.Length)
        {
            this.value = value;
        }

        protected override ReadOnlySpan<char> Source => this.value;
        public override ReadOnlySpan<char> Data => this.value;

        internal override bool TryAdd(Content content)
        {
            if (content is StringContent streamContent && base.TryAdd(streamContent))
            {
                this.value += streamContent.value;
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

        public override ReadOnlySpan<char> Data => this.Source.Slice(this.dataStart, this.dataLength);

        internal override string ToDebugString()
        {
            var name = this.Parent is ParentTag parent ? parent.Reference.Syntax.TrimName(base.Data) : ReadOnlySpan<char>.Empty;
            return $"<[{name}[{base.ToDebugString()}]]>";
        }
    }

    sealed class StreamSection : Section
    {
        private readonly string name;
        private readonly string data;

        public StreamSection(string name, int start, int length, string data, int dataStart)
            : base(start, length, dataStart, data.Length)
        {
            this.name = name;
            this.data = data;
        }

        public override ReadOnlySpan<char> Data => this.data;

        internal override string ToDebugString()
        {
            var data = this.data.AsSpan().TrimStart();
            return $"<[{this.name}[{string.Concat(data[..Math.Min(15, data.Length)], "\u2026")}]]>";
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
            this.Source.IsEmpty ? ReadOnlySpan<char>.Empty :
            this.Parent is ParentTag parent ? parent.Reference.Syntax.TrimData(base.Data) :
            base.Data;

        internal override string ToDebugString()
        {
            return $"<!--{base.ToDebugString()}-->";
        }
    }
}
