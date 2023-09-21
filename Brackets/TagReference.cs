namespace Brackets
{
    using System;

    public class TagReference : SyntaxReference
    {
        public TagReference(string name, Document.MarkupReference markup) : base(markup)
        {
            this.Name = name;
            this.IsParent = true;
            this.Level = ElementLevel.Block;
        }

        public string Name { get; }

        public bool IsParent { get; init; }

        public bool HasRawContent { get; init; }

        public ElementLevel Level { get; init; }

        public virtual bool IsRoot => false;

        public virtual string ToString(Tag tag) => String.Empty;
    }

    public sealed class RootReference : TagReference
    {
        internal RootReference(Document.MarkupReference markup)
            : base(String.Empty, markup)
        {
        }

        public override bool IsRoot => true;
    }
}
