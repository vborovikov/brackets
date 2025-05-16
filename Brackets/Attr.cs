namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class Attr : Element
    {
        private readonly AttrRef reference;

        public Attr(AttrRef reference, int offset, int length)
            : base(offset)
        {
            this.reference = reference;
            this.Length = length;
        }

        public sealed override int Length { get; }

        internal AttrRef Reference => this.reference;

        public string Name => this.reference.Name;

        public bool IsFlag => this.reference.IsFlag || !this.HasValue;

        [MemberNotNullWhen(true, nameof(Value))]
        public virtual bool HasValue => false;

        public virtual ReadOnlySpan<char> Value => ReadOnlySpan<char>.Empty;

        public override string ToString() => string.Empty;

        public new Attr Clone() => (Attr)CloneOverride();

        protected override Element CloneOverride() => new StringAttr(this.reference, null, this.Offset, this.Length);

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
            return this.HasValue ? $"{this.Name}=\"{this.Value}\"" : this.Name;
        }

        /// <summary>
        /// Strips the attribute value from the quotation marks and then removes any leading or trailing whitespace.
        /// </summary>
        /// <param name="value">The raw attribute value provided by the parser.</param>
        /// <returns>The trimmed attribute value.</returns>
        protected ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> value) => this.reference.Syntax.TrimValue(value).Trim();

        /// <summary>
        /// Enumerates the attributes of a <see cref="Tag">tag</see>.
        /// </summary>
        public new struct Enumerator : IEnumerable<Attr>, IEnumerator<Attr>
        {
            private readonly Attr? last;
            private Attr? current;

            internal Enumerator(Attr? first)
            {
                this.last = (Attr?)first?.Prev;
            }

            /// <summary>
            /// Gets the current attribute.
            /// </summary>
            public readonly Attr Current => this.current!;

            /// <summary>
            /// Returns this instance as an enumerator.
            /// </summary>
            public readonly Enumerator GetEnumerator() => this;

            /// <inheritdoc/>
            [MemberNotNullWhen(true, nameof(current))]
            public bool MoveNext()
            {
                if (this.current == this.last)
                    return false;

                if (this.current is null || this.current.Parent is null || this.current.Parent != this.last?.Parent)
                {
                    // the enumeration has been reset or the attribute has been removed,
                    // in both cases we have to start from the beginning
                    this.current = (Attr?)this.last?.Next;
                }
                else
                {
                    // move to the next attribute
                    this.current = (Attr?)this.current.Next;
                }

                return this.current is not null;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                this.current = null;
            }

            /// <inheritdoc/>
            public readonly void Dispose() { }
            readonly object IEnumerator.Current => this.current!;
            readonly IEnumerator<Attr> IEnumerable<Attr>.GetEnumerator() => GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public readonly struct List : IEnumerable<Attr>
        {
            private readonly Tag tag;

            internal List(Tag tag)
            {
                this.tag = tag;
            }

            internal Attr? First => this.tag.FirstAttribute;

            public ReadOnlySpan<char> this[ReadOnlySpan<char> name]
            {
                get => Get(name);
                set => Set(name, value);
            }

            public Enumerator GetEnumerator() => new(this.tag.FirstAttribute);
            IEnumerator<Attr> IEnumerable<Attr>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public ReadOnlySpan<char> Get(ReadOnlySpan<char> name)
            {
                return Find(name) is Attr attr ? attr.Value : ReadOnlySpan<char>.Empty;
            }

            public void Set(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
            {
                var newAttr = this.tag.Reference.Syntax.CreateAttribute(name, value);
                this.tag.ReplaceAttribute(Find(name), newAttr);
            }

            public bool Has(ReadOnlySpan<char> name) => Find(name) is not null;

            public bool Has(ReadOnlySpan<char> name, ReadOnlySpan<char> value, StringComparison valueComparison = StringComparison.OrdinalIgnoreCase)
            {
                var attr = Find(name);
                return attr is not null && attr.Value.Contains(value, valueComparison);
            }

            private Attr? Find(ReadOnlySpan<char> name)
            {
                if (this.tag.FirstAttribute is Attr first)
                {
                    var compareMethod = this.tag.Reference.Syntax.Comparison;
                    var current = first;
                    do
                    {
                        if (name.Equals(current.Name, compareMethod))
                            return current;
                        current = (Attr)current.Next;
                    } while (current != first);
                }

                return null;
            }
        }
    }

    sealed class ValueAttr : Attr
    {
        private readonly int valueOffset;
        private readonly int valueLength;

        public ValueAttr(AttrRef reference, int offset, int length, int valueOffset, int valueLength)
            : base(reference, offset, length)
        {
            this.valueOffset = valueOffset;
            this.valueLength = valueLength;
        }

        public override bool HasValue => true;

        public override ReadOnlySpan<char> Value => TrimValue(this.Source.Slice(this.valueOffset, this.valueLength));

        public override string ToString() => this.Value.ToString();

        protected override Element CloneOverride() =>
            new StringAttr(this.Reference, this.Source.Slice(this.valueOffset, this.valueLength), this.Offset, this.Length);
    }

    sealed class StringAttr : Attr
    {
        private readonly string? value;

        public StringAttr(AttrRef reference, ReadOnlySpan<char> value, int offset, int length)
            : base(reference, offset, length)
        {
            this.value = value.IsEmpty ? null : TrimValue(value).ToString();
        }

        public StringAttr(AttrRef reference, string? value, int offset, int length)
            : base(reference, offset, length)
        {
            this.value = value;
        }

        public override bool HasValue => this.value is not null;

        public override ReadOnlySpan<char> Value => this.value;

        protected override ReadOnlySpan<char> Source => this.value;

        public override string ToString() => this.value ?? string.Empty;

        protected override Element CloneOverride() =>
            new StringAttr(this.Reference, this.value, this.Offset, this.Length);
    }
}
