namespace Brackets;

public class AttributeReference : SyntaxReference
{
    public AttributeReference(string name, Document.MarkupReference markup)
        : base(markup)
    {
        this.Name = name;
    }

    public string Name { get; }

    public bool IsFlag { get; init; }
}