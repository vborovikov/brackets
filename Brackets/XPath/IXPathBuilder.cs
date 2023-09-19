namespace Brackets.XPath
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public interface IXPathBuilder<TNode>
    {
        // Should be called once per build
        void Begin();

        // Should be called after build for result tree post-processing
        TNode End(TNode result);

        TNode Text(string value);

        TNode Number(string value);

        TNode Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string? prefix, string? name);

        TNode Operator(XPathOperator op, TNode left, [AllowNull] TNode right);

        TNode Variable(string? prefix, string name);

        TNode Join(TNode left, TNode right);

        TNode Union(TNode left, TNode right);

        // http://www.w3.org/TR/xquery-semantics/#id-axis-steps
        // reverseStep is how parser comunicates to builder diference between "ansestor[1]" and "(ansestor)[1]"
        TNode Predicate(TNode node, TNode condition, bool reverseStep);

        TNode Function(string? prefix, string name, IEnumerable<TNode> args);
    }
}