namespace Brackets
{
    using System;

    public class TagReference : SyntaxAware
    {
        public TagReference(string name, ISyntaxReference syntax) : base(syntax)
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
        internal RootReference(ISyntaxReference syntax)
            : base(String.Empty, syntax)
        {
        }

        public override bool IsRoot => true;
    }
}
