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
        }

        public string Name { get; }

        public bool IsParent { get; init; }

        public bool HasRawContent { get; init; }

        public ElementLevel Level { get; init; }

        public bool IsProcessingInstruction { get; init; }

        public virtual bool IsRoot => false;

        public virtual string ToString(Tag tag) => String.Empty;
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
