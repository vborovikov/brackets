namespace Brackets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class Attr : Element
    {
        private readonly AttrRef reference;
        private readonly int length;

        public Attr(AttrRef reference, int start, int length)
            : base(start)
        {
            this.reference = reference;
            this.length = length;
        }

        public sealed override int End => this.Start + this.length;

        internal AttrRef Reference => this.reference;

        public string Name => this.reference.Name;

        public bool IsFlag => this.reference.IsFlag || !this.HasValue;

        [MemberNotNullWhen(true, nameof(Value))]
        public virtual bool HasValue => false;

        public virtual ReadOnlySpan<char> Value => ReadOnlySpan<char>.Empty;

        public override string ToString() => string.Empty;

        public override Element Clone() => new StringAttr(this.reference, null, this.Offset, this.length);

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

        public new struct Enumerator : IEnumerable<Attr>, IEnumerator<Attr>
        {
            private readonly Attr? last;
            private Attr? sibling;
            private Attr? current;

            internal Enumerator(Attr? first)
            {
                //todo: use last here?
                this.last = (Attr?)first?.Prev;
                this.sibling = first;
            }

            public readonly Attr Current => this.current!;
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
                this.sibling = this.sibling == this.last ? null : (Attr)this.sibling.Next;

                return true;
            }

            public void Reset()
            {
                this.sibling = (Attr?)this.last?.Next;
                this.current = null;
            }

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
        private readonly int valueStart;
        private readonly int valueLength;

        public ValueAttr(AttrRef reference, int start, int length, int valueStart, int valueLength)
            : base(reference, start, length)
        {
            this.valueStart = valueStart;
            this.valueLength = valueLength;
        }

        public override bool HasValue => true;

        public override ReadOnlySpan<char> Value => TrimValue(this.Source.Slice(this.valueStart, this.valueLength));

        public override string ToString() => this.Value.ToString();

        public override Element Clone() =>
            new StringAttr(this.Reference, this.Source.Slice(this.valueStart, this.valueLength), this.Offset, this.Length);
    }

    sealed class StringAttr : Attr
    {
        private readonly string? value;

        public StringAttr(AttrRef reference, ReadOnlySpan<char> value, int offset, int length)
            : base(reference, offset, length)
        {
            this.value = TrimValue(value).ToString();
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

        public override Element Clone() =>
            new StringAttr(this.Reference, this.value, this.Offset, this.Length);
    }
}
