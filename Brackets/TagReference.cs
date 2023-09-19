namespace Brackets
{
    using System;

    public class TagReference
    {
        public TagReference(string name)
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
        public static readonly RootReference Default = new();

        private RootReference()
            : base(String.Empty)
        {
        }

        public override bool IsRoot => true;
    }
}
