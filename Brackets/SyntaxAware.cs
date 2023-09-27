namespace Brackets;

public abstract class SyntaxAware
{
    protected SyntaxAware(ISyntaxReference syntax)
    {
        this.Syntax = syntax;
    }

    internal ISyntaxReference Syntax { get; }
}
