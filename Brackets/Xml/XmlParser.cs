﻿namespace Brackets.Xml;

using Collections;

public class XmlParser : MarkupParser<XmlLexer>
{
    public XmlParser() : this(
        new StringSet<TagRef>(XmlLexer.Comparison),
        new StringSet<AttrRef>(XmlLexer.Comparison))
    { }

    protected XmlParser(IStringSet<TagRef> tagReferences, IStringSet<AttrRef> attributeReferences)
        : base(MarkupLanguage.Xml, tagReferences, attributeReferences)
    {
        // void elements
        AddReference(new TagRef("xml", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagRef("doctype", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
        AddReference(new TagRef("xml-stylesheet", this) { IsParent = false, Level = ElementLevel.Inline, IsProcessingInstruction = true });
    }

    internal static XmlParser CreateConcurrent() =>
        new(new ConcurrentStringSet<TagRef>(XmlLexer.Comparison), 
            new ConcurrentStringSet<AttrRef>(XmlLexer.Comparison));
}
