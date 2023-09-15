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

        public override string ToText(Tag tag) => Environment.NewLine;
    }
}
