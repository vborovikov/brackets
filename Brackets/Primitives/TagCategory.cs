namespace Brackets.Primitives
{
    public enum TagCategory
    {
        Content,  // abc...
        Opening,  // <abc ...>
        Closing,  // </abc>
        Unpaired, // <abc ... />
        Comment, // <!--...-->
        Section, // <![ABC[...]]>
    }
}
