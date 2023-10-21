namespace Brackets.Html
{
    public class HrTagRef : TagRef
    {
        private static readonly string HorizontalLine = new('-', 80);

        public HrTagRef(ISyntaxReference syntax) : base("hr", syntax)
        {
            this.IsParent = false;
        }

        public override string ToString(Tag tag) => HorizontalLine;
    }
}
