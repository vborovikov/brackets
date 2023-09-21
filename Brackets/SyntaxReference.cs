namespace Brackets;

using Primitives;

public abstract class SyntaxReference
{
    private readonly Document.MarkupReference markup;

    protected SyntaxReference(Document.MarkupReference markup)
    {
        this.markup = markup;
    }

    internal ref readonly MarkupSyntax Syntax => ref this.markup.Syntax;
}
