namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Primitives;

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

        public bool HasValue => this.Start < this.valueStart;

        public ReadOnlySpan<char> Value => this.Source.Slice(this.valueStart, this.valueLength);

        public override string? ToString()
        {
            if (!this.HasValue)
                return String.Empty;

            var valueSpan = this.Value;
            if (valueSpan[0] == valueSpan[^1] && this.reference.Syntax.QuotationMarks.Contains(valueSpan[0]))
                valueSpan = valueSpan[1..^1];

            return valueSpan.ToString();
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

        protected internal override string ToDebugString()
        {
            return this.HasValue ? $"{this.reference.Name}={this.Value.ToString()}" : this.reference.Name;
        }

        public new struct Enumerator : IEnumerator<Attribute>
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
        }
    }

    public interface IAttributeCollection : IEnumerable<Attribute>
    {
        int Count { get; }

        void Add(Attribute attribute);
        void Remove(Attribute attribute);

        new Attribute.Enumerator GetEnumerator();
    }
}
