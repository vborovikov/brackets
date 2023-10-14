namespace Brackets.Parsing;

using System;

static class Lexer
{
    public static ElementTokenEnumerator<TMarkupLexer> TokenizeElements<TMarkupLexer>(in ReadOnlySpan<char> text, in TMarkupLexer syntax, int globalOffset = 0)
        where TMarkupLexer : struct, IMarkupLexer => new(text, syntax, globalOffset);

    public static AttributeTokenEnumerator<TMarkupLexer> TokenizeAttributes<TMarkupLexer>(in Token token, in TMarkupLexer syntax)
        where TMarkupLexer : struct, IMarkupLexer => new(token, syntax);

    public ref struct ElementTokenEnumerator<TMarkupLexer>
        where TMarkupLexer : struct, IMarkupLexer
    {
        private readonly ReadOnlySpan<char> text;
        private readonly ref readonly TMarkupLexer lexer;
        private readonly int globalOffset;
        private int offset;
        private Token current;

        public ElementTokenEnumerator(in ReadOnlySpan<char> text, in TMarkupLexer lexer, int globalOffset)
        {
            this.text = text;
            this.lexer = ref lexer;
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
        private readonly Token token;
        private int offset;
        private Token current;

        public AttributeTokenEnumerator(in Token token, in TMarkupLexer lexer)
        {
            this.lexer = ref lexer;
            this.token = token;
        }

        public readonly Token Current => this.current;

        public readonly AttributeTokenEnumerator<TMarkupLexer> GetEnumerator() => this;

        public void Reset()
        {
            this.offset = 0;
        }

        public bool MoveNext()
        {
            if (this.offset >= this.token.Data.Length)
                return false;

            this.current = this.lexer.GetAttributeToken(this.token.Data[this.offset..], this.token.DataOffset + this.offset);
            if (this.current.Span.Length == 0)
            {
                this.offset = this.token.Data.Length;
                return false;
            }

            this.offset = this.current.Offset + this.current.Span.Length - this.token.DataOffset;
            return true;
        }
    }
}
