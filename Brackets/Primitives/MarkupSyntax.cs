namespace Brackets.Primitives;

//todo: make it an abstract class?
public readonly struct MarkupSyntax
{
    public required StringComparison Comparison { get; init; }
    public required char Opener { get; init; }
    public required char Closer { get; init; }
    public required char Terminator { get; init; }
    public required char EqSign { get; init; }
    public required char AltOpener { get; init; }
    public required string Separators { get; init; }
    public required string AttrSeparators { get; init; }
    public required string QuotationMarks { get; init; }
    public required string CommentOpener { get; init; }
    public required string CommentOpenerNB { get; init; }
    public required string CommentCloser { get; init; }
    public required string SectionOpener { get; init; }
    public required string SectionOpenerNB { get; init; }
    public required string SectionCloser { get; init; }
    public required char ContentOpener { get; init; }
}
