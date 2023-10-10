namespace ParseXPath
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Brackets.XPath;
    using Termly;

    public class XPathStringBuilder : IXPathBuilder<string>
    {
        private static readonly Dictionary<XPathOperator, string> opStrings = new Dictionary<XPathOperator, string>
        {
            { XPathOperator.Unknown    , " Unknown " },
            { XPathOperator.Or         , " or "      },
            { XPathOperator.And        , " and "     },
            { XPathOperator.Eq         , "="         },
            { XPathOperator.Ne         , "!="        },
            { XPathOperator.Lt         , "<"         },
            { XPathOperator.Le         , "<="        },
            { XPathOperator.Gt         , ">"         },
            { XPathOperator.Ge         , ">="        },
            { XPathOperator.Plus       , "+"         },
            { XPathOperator.Minus      , "-"         },
            { XPathOperator.Multiply   , "*"         },
            { XPathOperator.Divide     , " div "     },
            { XPathOperator.Modulo     , " mod "     },
            { XPathOperator.UnaryMinus , "-"         },
        };

        private static readonly Dictionary<XPathAxis, string> axisStrings = new Dictionary<XPathAxis, string>
        {
            { XPathAxis.Ancestor         , "ancestor::"          },
            { XPathAxis.AncestorOrSelf   , "ancestor-or-self::"  },
            { XPathAxis.Attribute        , "attribute::"         },
            { XPathAxis.Child            , "child::"             },
            { XPathAxis.Descendant       , "descendant::"        },
            { XPathAxis.DescendantOrSelf , "descendant-or-self::"},
            { XPathAxis.Following        , "following::"         },
            { XPathAxis.FollowingSibling , "following-sibling::" },
            { XPathAxis.Namespace        , "namespace::"         },
            { XPathAxis.Parent           , "parent::"            },
            { XPathAxis.Preceding        , "preceding::"         },
            { XPathAxis.PrecedingSibling , "preceding-sibling::" },
            { XPathAxis.Self             , "self::"              },
        };

        public void Begin()
        {
        }

        public string End(string result)
        {
            return result;
        }

        public string Text(string value)
        {
            return ("'" + value + "'").InColor(XPathColors.Argument);
        }

        public string Number(string value)
        {
            return value.InColor(XPathColors.Argument);
        }

        public string Operator(XPathOperator op, string left, string? right)
        {
            if (op == XPathOperator.UnaryMinus)
            {
                return "-" + left;
            }
            return left + opStrings[op].InColor(XPathColors.Parameter) + right;
        }

        public string Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string? prefix, string? name)
        {
            var axisTest = axisStrings[xpathAxis];
            string nodeTest;
            switch (nodeType)
            {
                case XPathNodeType.ProcessingInstruction:
                    Debug.Assert(prefix == "");
                    nodeTest = "processing-instruction(".InColor(XPathColors.CallNumber) + name + ")".InColor(XPathColors.CallNumber);
                    break;

                case XPathNodeType.Text:
                    Debug.Assert(prefix == null && name == null);
                    nodeTest = "text()".InColor(XPathColors.CallNumber);
                    break;

                case XPathNodeType.Comment:
                    Debug.Assert(prefix == null && name == null);
                    nodeTest = "comment()".InColor(XPathColors.CallNumber);
                    break;

                case XPathNodeType.Root:
                    axisTest = "root::";
                    goto case XPathNodeType.All;
                case XPathNodeType.All:
                    nodeTest = "node()".InColor(XPathColors.CallNumber);
                    break;

                case XPathNodeType.Attribute:
                case XPathNodeType.Element:
                case XPathNodeType.Namespace:
                    nodeTest = QNameOrWildcard(prefix, name).InColor(XPathColors.Argument);
                    break;

                default:
                    throw new ArgumentException("unexpected XPathNodeType", "XPathNodeType");
            }
            return axisTest.InColor(XPathColors.Operation) + nodeTest;
        }

        public string Join(string left, string right)
        {
            return left + '/'.InColor(XPathColors.Parameter) + right;
        }

        public string Predicate(string node, string condition, bool reverseStep)
        {
            if (!reverseStep)
            {
                // In this method we don't know how axis was represented in original XPath and the only
                // difference between ancestor::*[2] and (ancestor::*)[2] is the reverseStep parameter.
                // to not store the axis from previous builder events we simply wrap node in the () here.
                node = '('.InColor(XPathColors.Parameter) + node + ')'.InColor(XPathColors.Parameter);
            }
            return node + '['.InColor(XPathColors.Parameter) + condition + ']'.InColor(XPathColors.Parameter);
        }

        public string Variable(string? prefix, string name)
        {
            return '$' + QName(prefix, name);
        }

        public string Function(string? prefix, string name, IEnumerable<string> args)
        {
            string result = QName(prefix, name) + '(' + string.Join(',', args) + ')';
            return result;
        }

        private static string QName(string? prefix, string? localName)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException("prefix");
            }
            if (localName == null)
            {
                throw new ArgumentNullException("localName");
            }
            return prefix == "" ? localName.InColor(XPathColors.Parameter) : prefix.InColor(XPathColors.Parameter) + ':' + localName.InColor(XPathColors.Parameter);
        }

        private static string QNameOrWildcard(string? prefix, string? localName)
        {
            if (prefix == null)
            {
                Debug.Assert(localName == null);
                return "*";
            }
            if (localName == null)
            {
                Debug.Assert(prefix != "");
                return prefix + ":*";
            }
            return prefix == "" ? localName : prefix + ':' + localName;
        }

        public string Union(string left, string right)
        {
            return left + '|'.InColor(XPathColors.Parameter) + right;
        }
    }
}