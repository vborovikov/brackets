namespace Brackets.Html
{
    public class HrTagReference : TagReference
    {
        private static readonly string HorizontalLine = new string('-', 80);

        public HrTagReference() : base("hr")
        {
            this.IsParent = false;
        }

        public override string ToText(Tag tag) => HorizontalLine;
    }
}
