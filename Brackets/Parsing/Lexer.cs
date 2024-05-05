namespace Brackets.Parsing;

using System;

static class Lexer
{
    public static ElementTokenEnumerator<TMarkupLexer> TokenizeElements<TMarkupLexer>(in ReadOnlySpan<char> text, in TMarkupLexer syntax, int globalOffset = 0)
        where TMarkupLexer : struct, IMarkupLexer => new(syntax, text, globalOffset);

    public static AttributeTokenEnumerator<TMarkupLexer> TokenizeAttributes<TMarkupLexer>(in Token token, in TMarkupLexer syntax)
        where TMarkupLexer : struct, IMarkupLexer => new(syntax, token);

    public ref struct ElementTokenEnumerator<TMarkupLexer>
        where TMarkupLexer : struct, IMarkupLexer
    {
        private readonly ref readonly TMarkupLexer lexer;
        private readonly ReadOnlySpan<char> text;
        private readonly int globalOffset;
        private int offset;
        private Token current;

        public ElementTokenEnumerator(in TMarkupLexer lexer, in ReadOnlySpan<char> text, int globalOffset)
        {
            this.lexer = ref lexer;
            this.text = text;
            this.globalOffset = globalOffset;
        }

        public readonly Token Current => this.current;

        public readonly ElementTokenEnumerator<TMarkupLexer> GetEnumerator() => this;

        public void Reset()
        {
            this.offset = 0;
        }

        public bool MoveNext()
        {
            if (this.offset >= this.text.Length)
                return false;

            this.current = this.lexer.GetElementToken(this.text[this.offset..], this.globalOffset + this.offset);
            if (this.current.Span.Length == 0)
            {
                this.offset = this.text.Length;
                return false;
            }

            this.offset = this.current.Offset + this.current.Span.Length - this.globalOffset;
            return true;
        }
    }

    public ref struct AttributeTokenEnumerator<TMarkupLexer>
        where TMarkupLexer : struct, IMarkupLexer
    {
        private readonly ref readonly TMarkupLexer lexer;
        private readonly ReadOnlySpan<char> data;
        private readonly int dataOffset;
        private int offset;
        private Token current;

        public AttributeTokenEnumerator(in TMarkupLexer lexer, in Token token)
        {
            this.lexer = ref lexer;
            this.data = token.Data;
            this.dataOffset = token.DataOffset;
        }

        public readonly Token Current => this.current;

        public readonly AttributeTokenEnumerator<TMarkupLexer> GetEnumerator() => this;

        public void Reset()
        {
            this.offset = 0;
        }

        public bool MoveNext()
        {
            if (this.offset >= this.data.Length)
                return false;

            this.current = this.lexer.GetAttributeToken(this.data[this.offset..], this.dataOffset + this.offset);
            if (this.current.Span.Length == 0)
            {
                this.offset = this.data.Length;
                return false;
            }

            this.offset = this.current.Offset + this.current.Span.Length - this.dataOffset;
            return true;
        }
    }
}
