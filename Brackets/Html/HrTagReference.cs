namespace Brackets.Html
{
    public class HrTagReference : TagReference
    {
        private static readonly string HorizontalLine = new('-', 80);

        public HrTagReference(Document.MarkupReference markup) : base("hr", markup)
        {
            this.IsParent = false;
        }

        public override string ToString(Tag tag) => HorizontalLine;
    }
}
