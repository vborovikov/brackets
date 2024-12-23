namespace Brackets;

public class AttrRef : SyntaxAware
{
    public AttrRef(string name, IMarkup markup)
        : base(markup)
    {
        this.Name = name;
    }

    public string Name { get; }

    public bool IsFlag { get; init; }
}