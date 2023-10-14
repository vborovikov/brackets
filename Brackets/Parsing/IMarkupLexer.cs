namespace Brackets.Parsing;

public enum TokenCategory
{
    Discarded,   // tokens discarded as content
    Content,     // abc...
    OpeningTag,  // <abc ...>
    ClosingTag,  // </abc>
    UnpairedTag, // <abc ... />
    Comment,     // <!--...-->
    Section,     // <![ABC[...]]>
    Attribute,   // abc="123"
}

public readonly ref struct Token
{
    public Token(TokenCategory category, ReadOnlySpan<char> span, int offset)
    {
        this.Category = category;
        this.Span = span;
        this.Offset = offset;
    }

    public Token(TokenCategory category, ReadOnlySpan<char> span, int offset,
        ReadOnlySpan<char> name, int nameOffset, ReadOnlySpan<char> data, int dataOffset)
        : this(category, span, offset)
    {
        this.Name = name;
        this.NameOffset = nameOffset;
        this.Data = data;
        this.DataOffset = dataOffset;
    }

    public TokenCategory Category { get; }
    public ReadOnlySpan<char> Span { get; }
    public ReadOnlySpan<char> Name { get; }
    public ReadOnlySpan<char> Data { get; }
    public int Offset { get; }
    public int NameOffset { get; }
    public int DataOffset { get; }
    public bool IsEmpty => this.Category == TokenCategory.Content && (this.Span.IsEmpty || this.Span.IsWhiteSpace());

    internal int Start => this.Offset;
    internal int End => this.Offset + this.Span.Length;
    internal int Length => this.Span.Length;

    public static implicit operator ReadOnlySpan<char>(in Token token) => token.Span;
}

/// <summary>
/// Represents a markup syntax.
/// </summary>
public interface IMarkupLexer
{
    /// <summary>
    /// Gets the string comparison for the syntax.
    /// </summary>
    StringComparison Comparison { get; }

    char Opener { get; }

    char Closer { get; }

    Token GetElementToken(ReadOnlySpan<char> text, int globalOffset);

    Token GetAttributeToken(ReadOnlySpan<char> text, int globalOffset);

    /// <summary>
    /// Determines if a tag with the specified name is closed by the specified tag span.
    /// </summary>
    /// <param name="tagSpan">The closing tag span.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <returns><c>true</c> if the tag is closed by the specified tag span; otherwise, <c>false</c>.</returns>
    bool ClosesTag(ReadOnlySpan<char> tagSpan, ReadOnlySpan<char> tagName);

    /// <summary>
    /// Determines if the specified tag is closed.
    /// </summary>
    /// <param name="tagSpan">The tag span.</param>
    /// <returns><c>true</c> if the tag is closed; otherwise, <c>false</c>.</returns>
    bool TagIsClosed(ReadOnlySpan<char> tagSpan);

    ReadOnlySpan<char> TrimName(ReadOnlySpan<char> tag);

    /// <summary>
    /// Trims the value from the specified attribute span.
    /// </summary>
    /// <param name="value">The attribute value span.</param>
    /// <returns>The trimmed value.</returns>
    ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> value);

    /// <summary>
    /// Trims the data from the specified section span.
    /// </summary>
    /// <param name="section">The section span.</param>
    /// <returns>The trimmed data.</returns>
    ReadOnlySpan<char> TrimData(ReadOnlySpan<char> section);
}