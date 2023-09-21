namespace Brackets.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public readonly struct MarkupSyntax
    {
        public MarkupSyntax()
        {
        }

        public char Opener { get; init; } = '<';
        public char Closer { get; init; } = '>';
        public char Slash { get; init; } = '/';
        public char EqSign { get; init; } = '=';
        public char AltOpener { get; init; } = '!';
        public string Separators { get; init; } = " \r\n\t\xA0";
        public string AttrSeparators { get; init; } = "= \r\n\t\xA0";
        public string QuotationMarks { get; init; } = "'\"";
        public string CommentOpener { get; init; } = "<!--";
        public string CommentCloser { get; init; } = "-->";
        public string SectionOpener { get; init; } = "<![";
        public string SectionCloser { get; init; } = "]]>";
    }
}
