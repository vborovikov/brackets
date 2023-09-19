namespace Brackets.XPath
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml.Linq;

    public class XPathTreeBuilder : IXPathBuilder<XElement>
    {
        public void Begin()
        {
        }

        public XElement End(XElement result)
        {
            return result;
        }

        public XElement Text(string value)
        {
            return new XElement("string", new XAttribute("value", value));
        }

        public XElement Number(string value)
        {
            return new XElement("number", new XAttribute("value", value));
        }

        public XElement Operator(XPathOperator op, XElement left, [AllowNull] XElement right)
        {
            if (op == XPathOperator.UnaryMinus)
            {
                return new XElement("negate", left);
            }
            return new XElement(op.ToString(), left, right);
        }

        public XElement Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string? prefix, string? name)
        {
            return new XElement(xpathAxis.ToString(),
                new XAttribute("nodeTyepe", nodeType.ToString()),
                new XAttribute("prefix", prefix ?? "(null)"),
                new XAttribute("name", name ?? "(null)")
            );
        }

        public XElement Join(XElement left, XElement right)
        {
            return new XElement("step", left, right);
        }

        public XElement Predicate(XElement node, XElement condition, bool reverseStep)
        {
            return new XElement("predicate", new XAttribute("reverse", reverseStep),
                node, condition
            );
        }

        public XElement Variable(string? prefix, string name)
        {
            return new XElement("variable",
                new XAttribute("prefix", prefix ?? "(null)"),
                new XAttribute("name", name ?? "(null)")
            );
        }

        public XElement Function(string? prefix, string name, IEnumerable<XElement> args)
        {
            XElement xe = new XElement("variable",
                new XAttribute("prefix", prefix ?? "(null)"),
                new XAttribute("name", name ?? "(null)")
            );
            foreach (XElement e in args)
            {
                xe.Add(e);
            }
            return xe;
        }

        public XElement Union(XElement left, XElement right)
        {
            return new XElement("union", left, right);
        }
    }
}