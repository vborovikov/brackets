namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class Attribute : Element
    {
        private readonly AttributeReference reference;
        private readonly int length;

        public Attribute(AttributeReference reference, int start, int length)
            : base(start)
        {
            this.reference = reference;
            this.length = length;
        }

        public sealed override int End => this.Start + this.length;

        public string Name => this.reference.Name;

        public bool IsFlag => this.reference.IsFlag || !this.HasValue;

        public virtual bool HasValue => false;

        public virtual ReadOnlySpan<char> Value => this.Name;

        public override string ToString()
        {
            if (!this.HasValue)
                return String.Empty;

            return this.Value.ToString();
        }

        public override bool TryGetValue<T>([MaybeNullWhen(false)] out T value)
        {
            if (!this.HasValue)
            {
                value = default;
                return false;
            }

            return TryGetValue(ToString(), out value);
        }

        internal override string ToDebugString()
        {
            return this.HasValue ? $"{this.Name}={this.Value}" : this.Name;
        }

        protected ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> value) => this.reference.Syntax.TrimValue(value);

        public new struct Enumerator : IEnumerable<Attribute>, IEnumerator<Attribute>
        {
            private readonly Attribute? first;
            private Attribute? sibling;
            private Attribute? current;

            internal Enumerator(Attribute? node)
            {
                this.first = node;
                this.sibling = node;
            }

            public readonly Attribute Current => this.current!;
            public readonly Enumerator GetEnumerator() => this;
            readonly object IEnumerator.Current => this.current!;

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.sibling is null)
                    return false;

                this.current = this.sibling;
                this.sibling = (Attribute)this.sibling.Next;
                if (this.sibling == this.first)
                {
                    this.sibling = null;
                }

                return true;
            }

            public void Reset()
            {
                this.sibling = this.first;
                this.current = null;
            }

            readonly IEnumerator<Attribute> IEnumerable<Attribute>.GetEnumerator() => GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    public class ValueAttribute : Attribute
    {
        private readonly int valueStart;
        private readonly int valueLength;

        public ValueAttribute(AttributeReference reference, int start, int length, int valueStart, int valueLength)
            : base(reference, start, length)
        {
            this.valueStart = valueStart;
            this.valueLength = valueLength;
        }

        public override bool HasValue => true;

        public override ReadOnlySpan<char> Value => TrimValue(this.Source.Slice(this.valueStart, this.valueLength));
    }

    sealed class StreamAttribute : Attribute
    {
        private readonly string value;

        public StreamAttribute(AttributeReference reference, ReadOnlySpan<char> value, int offset, int length)
            : base(reference, offset, length)
        {
            this.value = TrimValue(value).ToString();
        }

        public override bool HasValue => !string.IsNullOrEmpty(this.value);

        public override ReadOnlySpan<char> Value => this.value;

        protected override ReadOnlySpan<char> Source => this.value;
    }
}
