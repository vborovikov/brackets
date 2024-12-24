namespace Brackets.Html
{
    sealed class HrTagRef : TagRef
    {
        public HrTagRef(IMarkup markup) : base("hr", markup)
        {
            this.IsParent = false;
        }

        public override string ToString(Tag tag) => Environment.NewLine;
    }
}
