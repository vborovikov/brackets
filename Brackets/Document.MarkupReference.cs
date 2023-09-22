namespace Brackets
{
    using System;
    using System.Collections.Generic;
    using Collections;
    using Primitives;

    public sealed partial class Document
    {
        public abstract class MarkupReference
        {
            private readonly MarkupSyntax syntax;
            private readonly StringDir<TagReference> tagReferences;
            private readonly StringDir<AttributeReference> attributeReferences;
            private readonly RootReference rootReference;

            protected MarkupReference(in MarkupSyntax syntax)
            {
                this.syntax = syntax;
                this.tagReferences = new(this.syntax.Comparison);
                this.attributeReferences = new(this.syntax.Comparison);
                this.rootReference = new RootReference(this);
            }

            internal ref readonly MarkupSyntax Syntax => ref this.syntax;

            public Document Parse(string text)
            {
                var root = Parse(text.AsMemory());
                return new Document(root);
            }

            protected void AddReference(TagReference reference)
            {
                this.tagReferences.Add(reference.Name, reference);
            }

            protected void AddReference(AttributeReference reference)
            {
                this.attributeReferences.Add(reference.Name, reference);
            }

            private Root Parse(ReadOnlyMemory<char> text)
            {
                var span = text.Span;
                var tree = new Stack<ParentTag>();
                tree.Push(new Root(this.rootReference, text));

                foreach (var tag in Tags.Parse(span, this.syntax))
                {
                    // skip comments
                    if (tag.Category == TagCategory.Comment)
                        continue;

                    var parent = tree.Peek();

                    if (parent.HasRawContent)
                    {
                        if (tag.Category == TagCategory.Closing && parent.IsClosedBy(tag))
                        {
                            ParseClosingTag(tag, parent, tree);
                        }
                        else
                        {
                            ParseContent(tag, parent);
                        }
                    }
                    else
                    {
                        // skip empty content
                        if (tag.IsEmpty)
                            continue;

                        switch (tag.Category)
                        {
                            case TagCategory.Content:
                                ParseContent(tag, parent);
                                break;

                            case TagCategory.Opening:
                            case TagCategory.Unpaired:
                                ParseOpeningTag(tag, parent, tree);
                                break;

                            case TagCategory.Closing:
                                ParseClosingTag(tag, parent, tree);
                                break;
                        }
                    }
                }

                // close unclosed tags
                while (tree.Count > 1)
                {
                    var unclosedTag = tree.Pop();
                    if (!unclosedTag.IsClosed)
                    {
                        var parentTag = tree.Peek();
                        if (parentTag is Root)
                            continue;

                        parentTag.Graft(unclosedTag);
                    }
                }

                return (Root)tree.Pop();
            }

            private void ParseContent(TagSpan tagSpan, ParentTag parent)
            {
                parent.Add(CreateContent(tagSpan));
            }

            private void ParseOpeningTag(TagSpan tagSpan, ParentTag parent, Stack<ParentTag> tree)
            {
                var tag = CreateTag(tagSpan);
                parent.Add(tag);

                if (tag is ParentTag pairedTag)
                    tree.Push(pairedTag);
            }

            private void ParseClosingTag(TagSpan tagSpan, ParentTag parent, Stack<ParentTag> tree)
            {
                // close the current tag
                // also special handling for a misplaced closing tag
                foreach (var unclosedTag in tree)
                {
                    if (unclosedTag.IsClosedBy(tagSpan))
                    {
                        while (tree.Count > 1)
                        {
                            var tag = tree.Pop();
                            if (tag == unclosedTag)
                            {
                                if (tag != parent)
                                {
                                    var parentTag = tree.Peek();
                                    parentTag.Graft(tag);
                                }

                                tag.CloseAt(tagSpan.End);
                                return;
                            }
                            else
                            {
                                var parentTag = tree.Peek();
                                parentTag.Graft(tag);
                            }
                        }
                    }
                }

                // there was no opening tag for this closing tag, turn it into content
                parent.Add(CreateContent(tagSpan));
            }

            private static Content CreateContent(TagSpan tagSpan)
            {
                return new Content(tagSpan.Start, tagSpan.Span.Length);
            }

            private Tag CreateTag(TagSpan tagSpan)
            {
                var reference = CreateOrFindTagReference(tagSpan);
                var tag = tagSpan.Category != TagCategory.Unpaired && reference.IsParent ?
                    new ParentTag(reference, tagSpan.Start, tagSpan.Span.Length) :
                    new Tag(reference, tagSpan.Start, tagSpan.Span.Length);

                ParseAttributes(tag, tagSpan);

                return tag;
            }

            private void ParseAttributes(Tag tag, TagSpan tagSpan)
            {
                var attrName = default(AttributeSpan);
                foreach (var attr in Tags.ParseAttributes(tagSpan, this.syntax))
                {
                    if (attr.Category == AttributeCategory.Name)
                    {
                        attrName = attr;
                    }
                    else
                    {
                        var attribute = attr.Category == AttributeCategory.Flag ?
                            CreateAttribute(attr) : CreateAttribute(attrName, attr);
                        tag.Attributes.Add(attribute);
                    }
                }
            }

            private Attribute CreateAttribute(AttributeSpan name, AttributeSpan value = default)
            {
                var reference = CreateOrFindAttributeReference(name);
                return value.IsEmpty ?
                    new Attribute(reference, name.Start, name.Span.Length) :
                    new Attribute(reference, name.Start, name.Span.Length, value.Start, value.Span.Length);
            }

            private TagReference CreateOrFindTagReference(TagSpan tagSpan)
            {
                if (!this.tagReferences.TryGetValue(tagSpan.Name, out var reference))
                {
                    reference = new TagReference(tagSpan.Name.ToString(), this);
                    AddReference(reference);
                }

                return reference;
            }

            private AttributeReference CreateOrFindAttributeReference(AttributeSpan attr)
            {
                if (!this.attributeReferences.TryGetValue(attr, out var reference))
                {
                    reference = new AttributeReference(attr.Span.ToString(), this);
                    AddReference(reference);
                }

                return reference;
            }
        }
    }
}