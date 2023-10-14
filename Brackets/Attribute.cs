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
        private readonly int valueStart;
        private readonly int valueLength;

        public Attribute(AttributeReference reference, int start, int length)
            : this(reference, start, length, start, length)
        {
        }

        public Attribute(AttributeReference reference, int start, int length, int valueStart, int valueLength)
            : base(start)
        {
            this.reference = reference;
            this.length = length;
            this.valueStart = valueStart;
            this.valueLength = valueLength;
        }

        public sealed override int End => this.valueStart + this.valueLength;

        public string Name => this.reference.Name;

        public bool IsFlag => this.reference.IsFlag || !this.HasValue;

        public bool HasValue => this.Start < this.valueStart && this.valueLength > 0;

        public ReadOnlySpan<char> Value =>
            this.reference.Syntax.TrimValue(this.Source.Slice(this.valueStart, this.valueLength));

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
            return this.HasValue ? $"{this.reference.Name}={this.Value.ToString()}" : this.reference.Name;
        }

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

    sealed class StreamAttribute : Attribute
    {
        private readonly string value;

        public StreamAttribute(AttributeReference reference, string value)
            : base(reference, -1, 0, 0, value.Length)
        {
            this.value = value;
        }

        protected override ReadOnlySpan<char> Source => this.value;
    }
}
