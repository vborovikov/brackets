namespace Brackets.Html
{
    sealed class HrTagRef : TagRef
    {
        public HrTagRef(ISyntaxReference syntax) : base("hr", syntax)
        {
            this.IsParent = false;
        }

        public override string ToString(Tag tag) => Environment.NewLine;
    }
}
