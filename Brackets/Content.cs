namespace Brackets;

using System;

/// <summary>
/// Loosely specifies the groups of elements that share common characteristics.
/// </summary>
[Flags]
public enum ContentCategory
{
    /// <summary>
    /// The content category is not specified.
    /// </summary>
    None        = 0 << 0,
    /// <summary>
    /// Modifies the presentation or the behavior of the rest of a document.
    /// </summary>
    Metadata    = 1 << 0,
    /// <summary>
    /// Encompasses most elements that can go inside a document.
    /// </summary>
    Flow        = 1 << 1,
    /// <summary>
    /// Encompasses elements that are used to group content, such as sections, articles, navigation, etc.
    /// </summary>
    Sectioning  = 1 << 2,
    /// <summary>
    /// Encompasses elements that are used to mark up headings, such as h1, h2, h3, etc.
    /// </summary>
    Heading     = 1 << 3,
    /// <summary>
    /// Refers to the text and the markup within a document.
    /// </summary>
    Phrasing    = 1 << 4,
    /// <summary>
    /// Imports another resource or inserts content from another markup language or namespace into a document.
    /// </summary>
    Embedded    = 1 << 5,
    /// <summary>
    /// Includes elements that are specifically designed for user interaction.
    /// </summary>
    Interactive = 1 << 6,
    /// <summary>
    /// Includes elements that have a form owner, exposed by a form attribute.
    /// </summary>
    Form        = 1 << 7,
    /// <summary>
    /// Serves to support scripts, either by containing or specifying script code directly, or by specifying data that will be used by scripts.
    /// </summary>
    Script      = 1 << 8,
}

public abstract class CharacterData : Element
{
    protected CharacterData(int start)
        : base(start) { }

    public new ParentTag? Parent => base.Parent as ParentTag;

    public virtual ReadOnlySpan<char> Data => this.Source[this.Start..this.End];

    public override string ToString()
    {
        return this.Data.ToString();
    }

    internal override string ToDebugString()
    {
        var data = this.Data.TrimStart();
        if (data.IsEmpty)
            return string.Empty;

        return string.Concat(data[..Math.Min(Math.Min(15, data.Length), this.Length)], "\u2026");
    }

    public bool Contains(ReadOnlySpan<char> text)
    {
        return this.Data.Contains(text, StringComparison.CurrentCultureIgnoreCase);
    }
}

public class Content : CharacterData
{
    private int end;

    public Content(int start, int length) : base(start)
    {
        this.end = start + length;
    }

    public sealed override int End => this.end;

    public override Element Clone() => new StringContent(this.Data.ToString(), this.Offset);

    internal virtual bool TryConcat(Content content)
    {
        if (content.Start == this.End)
        {
            this.end = content.end;
            return true;
        }

        return false;
    }
}

sealed class StringContent : Content
{
    private string value;

    public StringContent(string value, int offset)
        : base(offset, value.Length)
    {
        this.value = value;
    }

    protected override ReadOnlySpan<char> Source => this.value;
    public override ReadOnlySpan<char> Data => this.value;

    public override string ToString() => this.value;

    public override Element Clone() => new StringContent(this.value, this.Offset);

    internal override bool TryConcat(Content content)
    {
        if (content is StringContent streamContent && base.TryConcat(streamContent))
        {
            this.value += streamContent.value;
            return true;
        }

        return false;
    }
}

public class Section : CharacterData
{
    private readonly int dataStart;
    private readonly int dataLength;

    public Section(int start, int length, int dataStart, int dataLength)
        : base(start)
    {
        this.End = start + length;
        this.dataStart = dataStart;
        this.dataLength = dataLength;
    }

    public override int End { get; }

    public virtual ReadOnlySpan<char> Name => this.Parent is ParentTag parent ? parent.Reference.Syntax.TrimName(base.Data) : ReadOnlySpan<char>.Empty;
    public override ReadOnlySpan<char> Data => this.Source.Slice(this.dataStart, this.dataLength);

    protected int DataStart => this.dataStart;

    public override Element Clone() => new StringSection(this.Name.ToString(), 
        this.Offset, this.Length, this.Data.ToString(), this.dataStart);

    internal override string ToDebugString()
    {
        return $"<[{this.Name}[{base.ToDebugString()}]]>";
    }
}

sealed class StringSection : Section
{
    private readonly string name;
    private readonly string data;

    public StringSection(string name, int start, int length, string data, int dataStart)
        : base(start, length, dataStart, data.Length)
    {
        this.name = name;
        this.data = data;
    }

    public override ReadOnlySpan<char> Name => this.name;
    public override ReadOnlySpan<char> Data => this.data;

    public override string ToString() => this.data;

    public override Element Clone() => new StringSection(this.name,
        this.Offset, this.Length, this.data, this.DataStart);

    internal override string ToDebugString()
    {
        var data = this.data.AsSpan().TrimStart();
        return $"<[{this.name}[{string.Concat(data[..Math.Min(15, data.Length)], "\u2026")}]]>";
    }
}

public class Comment : CharacterData
{
    public Comment(int start, int length) : base(start)
    {
        this.End = start + length;
    }

    public override int End { get; }

    public override ReadOnlySpan<char> Data =>
        this.Source.IsEmpty ? ReadOnlySpan<char>.Empty :
        this.Parent is ParentTag parent ? parent.Reference.Syntax.TrimData(base.Data) :
        base.Data;

    public override Element Clone() => new StringComment(this.Data.ToString(), this.Offset, this.Length);

    internal override string ToDebugString()
    {
        return $"<!--{base.ToDebugString()}-->";
    }
}

sealed class StringComment : Comment
{
    private readonly string data;

    public StringComment(string data, int start, int length) : base(start, length)
    {
        this.data = data;
    }

    public override ReadOnlySpan<char> Data => this.data;

    public override string ToString() => this.data;

    public override Element Clone() => new StringComment(this.data, this.Offset, this.Length);
}
