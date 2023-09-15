namespace Brackets.XPath
{
    /// <summary>
    /// Defines the XPath node types that can be returned from the <see cref="IXPathBuilder{TNode}"/>.
    /// </summary>
    public enum XPathNodeType
    {
        /// <summary>
        /// The root node of the document or node tree.
        /// </summary>
        Root,
        /// <summary>
        /// An element, such as <c>&lt;element&gt;</c>.
        /// </summary>
        Element,
        /// <summary>
        /// An attribute, such as <c>id='123'</c>.
        /// </summary>
        Attribute,
        /// <summary>
        /// A namespace, such as <c>xmlns="namespace"</c>.
        /// </summary>
        Namespace,
        /// <summary>
        /// The text content of a node. 
        /// Equivalent to the Document Object Model (DOM) Text and CDATA node types. Contains at least one character.
        /// </summary>
        Text,
        //SignificantWhitespace,
        //Whitespace,
        /// <summary>
        /// A processing instruction, such as <c>&lt;?pi test?&gt;</c>.
        /// </summary>
        ProcessingInstruction,
        /// <summary>
        /// A comment, such as <c>&lt;!-- my comment --&gt;</c>.
        /// </summary>
        Comment,
        /// <summary>
        /// Any of the <see cref="XPathNodeType"/> node types.
        /// </summary>
        All,
    }
}
