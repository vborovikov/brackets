namespace Brackets
{
    using System;

    public class TagRef : SyntaxAware
    {
        public TagRef(string name, ISyntaxReference syntax) : base(syntax)
        {
            this.Name = name;
            this.IsParent = true;
            this.Level = ElementLevel.Block;
            this.Category = ContentCategory.Flow;
        }

        public string Name { get; }

        public bool IsParent { get; init; }

        public bool HasRawContent { get; init; }

        public ElementLevel Level { get; init; }

        public ContentCategory Category { get; init; }

        public ContentCategory PermittedContent { get; init; }

        public bool IsProcessingInstruction { get; init; }

        public virtual bool IsRoot => false;

        public virtual string ToString(Tag tag) => string.Empty;
    }

    public sealed class RootRef : TagRef
    {
        internal RootRef(ISyntaxReference syntax)
            : base(String.Empty, syntax)
        {
        }

        public override bool IsRoot => true;
    }
}
