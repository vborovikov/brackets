namespace Brackets.XPath
{
    /// <summary>
    /// Represents a relationship to the context (current) node, 
    /// and is used to locate nodes relative to that node on the tree.
    /// </summary>
    public enum XPathAxis
    {
        /// <summary>
        /// Selects the current node.
        /// </summary>
        Self,
        /// <summary>
        /// Selects the parent of the current node.
        /// </summary>
        Parent,
        /// <summary>
        /// Selects all children of the current node.
        /// </summary>
        Child,
        /// <summary>
        /// Selects all ancestors (parent, grandparent, etc.) of the current node.
        /// </summary>
        Ancestor,
        /// <summary>
        /// Selects all ancestors (parent, grandparent, etc.) of the current node and the current node itself.
        /// </summary>
        AncestorOrSelf,
        /// <summary>
        /// Selects all descendants (children, grandchildren, etc.) of the current node.
        /// </summary>
        Descendant,
        /// <summary>
        /// Selects all descendants (children, grandchildren, etc.) of the current node and the current node itself.
        /// </summary>
        DescendantOrSelf,
        /// <summary>
        /// Selects everything in the document after the closing tag of the current node.
        /// </summary>
        Following,
        /// <summary>
        /// Selects all siblings after the current node.
        /// </summary>
        FollowingSibling,
        /// <summary>
        /// Selects all nodes that appear before the current node in the document, except ancestors, attribute nodes and namespace nodes.
        /// </summary>
        Preceding,
        /// <summary>
        /// Selects all siblings before the current node.
        /// </summary>
        PrecedingSibling,
        /// <summary>
        /// Selects all attributes of the current node.
        /// </summary>
        Attribute,
        /// <summary>
        /// Selects all namespace nodes of the current node.
        /// </summary>
        Namespace,
    }
}