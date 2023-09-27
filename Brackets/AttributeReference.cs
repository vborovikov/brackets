namespace Brackets;

public class AttributeReference : SyntaxAware
{
    public AttributeReference(string name, ISyntaxReference syntax)
        : base(syntax)
    {
        this.Name = name;
    }

    public string Name { get; }

    public bool IsFlag { get; init; }
}