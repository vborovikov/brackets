namespace Brackets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Primitives;

    public class Attribute : Element
    {
        private readonly AttributeReference reference;
        private readonly int length;
        private readonly int valueIndex;
        private readonly int valueLength;

        public Attribute(AttributeReference reference, int index, int length)
            : this(reference, index, length, index, length)
        {
        }

        public Attribute(AttributeReference reference, int index, int length, int valueIndex, int valueLength)
            : base(index)
        {
            this.reference = reference;
            this.length = length;
            this.valueIndex = valueIndex;
            this.valueLength = valueLength;
        }

        public string Name => this.reference.Name;

        public bool IsFlag => this.reference.IsFlag || !this.HasValue;

        public bool HasValue => this.Index < this.valueIndex;

        public ReadOnlySpan<char> Value => this.Source.Slice(this.valueIndex, this.valueLength);

        public override string ToText()
        {
            if (!this.HasValue)
                return String.Empty;

            var valueSpan = this.Value;
            if (valueSpan[0] == valueSpan[^1] && Tags.QuotationMarks.Contains(valueSpan[0]))
                valueSpan = valueSpan[1..^1];

            return valueSpan.ToString();
        }

        public override bool TryGetValue<T>(out T value)
        {
            if (!this.HasValue)
            {
                value = default;
                return false;
            }

            return TryGetValue(ToText(), out value);
        }

        public override string ToString()
        {
            return this.Source[this.Index..(this.valueIndex + this.valueLength)].ToString();
        }

        public override string ToDebugString()
        {
            return this.HasValue ? $"{this.reference.Name}={this.Value.ToString()}" : this.reference.Name;
        }
    }

    public interface IAttributeCollection : IEnumerable<Attribute>
    {
        int Count { get; }

        void Add(Attribute attribute);
        void Remove(Attribute attribute);

        string ToString();
    }
}
