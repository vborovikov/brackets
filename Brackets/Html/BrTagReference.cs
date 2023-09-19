namespace Brackets.Html
{
    using System;

    class BrTagReference : TagReference
    {
        public BrTagReference() : base("br")
        {
            this.IsParent = false;
            this.Level = ElementLevel.Inline;
        }

        public override string ToString(Tag tag) => Environment.NewLine;
    }
}
