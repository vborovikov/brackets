namespace Brackets;

public abstract class SyntaxAware
{
    protected SyntaxAware(IMarkup markup)
    {
        this.Syntax = (ISyntaxReference)markup;
    }

    internal ISyntaxReference Syntax { get; }
}
