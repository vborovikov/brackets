namespace Brackets;

public class AttrRef : SyntaxAware
{
    public AttrRef(string name, ISyntaxReference syntax)
        : base(syntax)
    {
        this.Name = name;
    }

    public string Name { get; }

    public bool IsFlag { get; init; }
}