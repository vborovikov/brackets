namespace Brackets.Html
{
    using System;

    sealed class BrTagRef : TagRef
    {
        public BrTagRef(ISyntaxReference syntax) : base("br", syntax)
        {
            this.IsParent = false;
            this.Layout = FlowLayout.Inline;
            this.Category = ContentCategory.Flow | ContentCategory.Phrasing;
        }

        public override string ToString(Tag tag) => Environment.NewLine;
    }
}
