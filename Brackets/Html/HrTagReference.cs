namespace Brackets.Html
{
    public class HrTagReference : TagReference
    {
        private static readonly string HorizontalLine = new('-', 80);

        public HrTagReference(ISyntaxReference syntax) : base("hr", syntax)
        {
            this.IsParent = false;
        }

        public override string ToString(Tag tag) => HorizontalLine;
    }
}
