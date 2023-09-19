namespace Brackets.XPath;

using System.Diagnostics;

sealed class XPathParser<TNode>
{
    private XPathScanner? scanner;
    private IXPathBuilder<TNode>? builder;
    private readonly Stack<int> posInfo = new();

    // Six possible causes of exceptions in the builder:
    // 1. Undefined prefix in a node test.
    // 2. Undefined prefix in a variable reference, or unknown variable.
    // 3. Undefined prefix in a function call, or unknown function, or wrong number/types of arguments.
    // 4. Argument of Union operator is not a node-set.
    // 5. First argument of Predicate is not a node-set.
    // 6. Argument of Axis is not a node-set.

    public TNode Parse(XPathScanner scanner, IXPathBuilder<TNode> builder, LexKind endLex)
    {
        Debug.Assert(this.scanner == null && this.builder == null);
        Debug.Assert(scanner != null && builder != null);

        TNode? result = default;
        this.scanner = scanner;
        this.builder = builder;
        this.posInfo.Clear();

        try
        {
            this.builder.Begin();
            result = ParseExpr();
            this.scanner.CheckToken(endLex);
        }
        catch (XPathParserException e)
        {
            if (e.queryString == null)
            {
                e.queryString = this.scanner.Source;
                PopPosInfo(out e.startChar, out e.endChar);
            }
            throw;
        }
        finally
        {
            result = this.builder.End(result!);
#if DEBUG
            this.builder = null;
            this.scanner = null;
#endif
        }
        Debug.Assert(this.posInfo.Count == 0, "PushPosInfo() and PopPosInfo() calls have been unbalanced");
        return result!;
    }

    #region Location paths and node tests
    /**************************************************************************************************/
    /*  Location paths and node tests                                                                 */
    /**************************************************************************************************/

    internal static bool IsStep(LexKind lexKind)
    {
        return (
            lexKind == LexKind.Dot ||
            lexKind == LexKind.DotDot ||
            lexKind == LexKind.At ||
            lexKind == LexKind.Axis ||
            lexKind == LexKind.Star ||
            lexKind == LexKind.Name   // NodeTest is also Name
        );
    }

    /*
    *   LocationPath ::= RelativeLocationPath | '/' RelativeLocationPath? | '//' RelativeLocationPath
    */
    private TNode ParseLocationPath()
    {
        if (this.scanner!.Kind == LexKind.Slash)
        {
            this.scanner.NextLex();
            var opnd = this.builder!.Axis(XPathAxis.Self, XPathNodeType.Root, null, null);

            if (IsStep(this.scanner.Kind))
            {
                opnd = this.builder.Join(opnd, ParseRelativeLocationPath());
            }
            return opnd;
        }
        else if (this.scanner.Kind == LexKind.SlashSlash)
        {
            this.scanner.NextLex();
            return this.builder!.Join(
                this.builder.Axis(XPathAxis.Self, XPathNodeType.Root, null, null),
                this.builder.Join(
                    this.builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                    ParseRelativeLocationPath()
                )
            );
        }
        else
        {
            return ParseRelativeLocationPath();
        }
    }

    /*
    *   RelativeLocationPath ::= Step (('/' | '//') Step)*
    */
    private TNode ParseRelativeLocationPath()
    {
        var opnd = ParseStep();
        if (this.scanner!.Kind == LexKind.Slash)
        {
            this.scanner.NextLex();
            opnd = this.builder!.Join(opnd, ParseRelativeLocationPath());
        }
        else if (this.scanner.Kind == LexKind.SlashSlash)
        {
            this.scanner.NextLex();
            opnd = this.builder!.Join(opnd,
                this.builder.Join(
                    this.builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                    ParseRelativeLocationPath()
                )
            );
        }
        return opnd;
    }

    /*
    *   Step ::= '.' | '..' | (AxisName '::' | '@')? NodeTest Predicate*
    */
    private TNode ParseStep()
    {
        TNode opnd;
        if (LexKind.Dot == this.scanner!.Kind)
        {                  // '.'
            this.scanner.NextLex();
            opnd = this.builder!.Axis(XPathAxis.Self, XPathNodeType.All, null, null);
            if (LexKind.LBracket == this.scanner.Kind)
            {
                throw this.scanner.CreateException("Abbreviated step '.' cannot be followed by a predicate. Use the full form 'self::node()[predicate]' instead.");
            }
        }
        else if (LexKind.DotDot == this.scanner.Kind)
        {        // '..'
            this.scanner.NextLex();
            opnd = this.builder!.Axis(XPathAxis.Parent, XPathNodeType.All, null, null);
            if (LexKind.LBracket == this.scanner.Kind)
            {
                throw this.scanner.CreateException("Abbreviated step '..' cannot be followed by a predicate. Use the full form 'parent::node()[predicate]' instead.");
            }
        }
        else
        {                                            // (AxisName '::' | '@')? NodeTest Predicate*
            XPathAxis axis;
            switch (this.scanner.Kind)
            {
                case LexKind.Axis:                              // AxisName '::'
                    axis = this.scanner.Axis;
                    this.scanner.NextLex();
                    this.scanner.NextLex();
                    break;
                case LexKind.At:                                // '@'
                    axis = XPathAxis.Attribute;
                    this.scanner.NextLex();
                    break;
                case LexKind.Name:
                case LexKind.Star:
                    // NodeTest must start with Name or '*'
                    axis = XPathAxis.Child;
                    break;
                default:
                    throw this.scanner.CreateException("Unexpected token '{0}' in the expression.", this.scanner.RawValue);
            }

            opnd = ParseNodeTest(axis);

            while (LexKind.LBracket == this.scanner.Kind)
            {
                opnd = this.builder!.Predicate(opnd, ParsePredicate(), IsReverseAxis(axis));
            }
        }
        return opnd;
    }

    private static bool IsReverseAxis(XPathAxis axis)
    {
        return (
            axis == XPathAxis.Ancestor || axis == XPathAxis.Preceding ||
            axis == XPathAxis.AncestorOrSelf || axis == XPathAxis.PrecedingSibling
        );
    }

    /*
    *   NodeTest ::= NameTest | ('comment' | 'text' | 'node') '(' ')' | 'processing-instruction' '('  Literal? ')'
    *   NameTest ::= '*' | NCName ':' '*' | QName
    */
    private TNode ParseNodeTest(XPathAxis axis)
    {
        XPathNodeType nodeType;
        string? nodePrefix, nodeName;

        var startChar = this.scanner!.LexStart;
        InternalParseNodeTest(this.scanner, axis, out nodeType, out nodePrefix, out nodeName);
        PushPosInfo(startChar, this.scanner.PrevLexEnd);
        var result = this.builder!.Axis(axis, nodeType, nodePrefix, nodeName);
        PopPosInfo();
        return result;
    }

    private static bool IsNodeType(XPathScanner scanner)
    {
        return scanner.Prefix.Length == 0 && (
            scanner.Name == "node" ||
            scanner.Name == "text" ||
            scanner.Name == "processing-instruction" ||
            scanner.Name == "comment"
        );
    }

    private static XPathNodeType PrincipalNodeType(XPathAxis axis)
    {
        return (
            axis == XPathAxis.Attribute ? XPathNodeType.Attribute :
            axis == XPathAxis.Namespace ? XPathNodeType.Namespace :
            /*else*/                      XPathNodeType.Element
        );
    }

    internal static void InternalParseNodeTest(XPathScanner scanner, XPathAxis axis, out XPathNodeType nodeType, out string? nodePrefix, out string? nodeName)
    {
        switch (scanner.Kind)
        {
            case LexKind.Name:
                if (scanner.CanBeFunction && IsNodeType(scanner))
                {
                    nodePrefix = null;
                    nodeName = null;
                    switch (scanner.Name)
                    {
                        case "comment": nodeType = XPathNodeType.Comment; break;
                        case "text": nodeType = XPathNodeType.Text; break;
                        case "node": nodeType = XPathNodeType.All; break;
                        default:
                            Debug.Assert(scanner.Name == "processing-instruction");
                            nodeType = XPathNodeType.ProcessingInstruction;
                            break;
                    }

                    scanner.NextLex();
                    scanner.PassToken(LexKind.LParens);

                    if (nodeType == XPathNodeType.ProcessingInstruction)
                    {
                        if (scanner.Kind != LexKind.RParens)
                        {  // 'processing-instruction' '(' Literal ')'
                            scanner.CheckToken(LexKind.String);
                            // It is not needed to set nodePrefix here, but for our current implementation
                            // comparing whole QNames is faster than comparing just local names
                            nodePrefix = string.Empty;
                            nodeName = scanner.StringValue;
                            scanner.NextLex();
                        }
                    }

                    scanner.PassToken(LexKind.RParens);
                }
                else
                {
                    nodePrefix = scanner.Prefix;
                    nodeName = scanner.Name;
                    nodeType = PrincipalNodeType(axis);
                    scanner.NextLex();
                    if (nodeName == "*")
                    {
                        nodeName = null;
                    }
                }
                break;
            case LexKind.Star:
                nodePrefix = null;
                nodeName = null;
                nodeType = PrincipalNodeType(axis);
                scanner.NextLex();
                break;
            default:
                throw scanner.CreateException("Expected a node test, found '{0}'.", scanner.RawValue);
        }
    }

    /*
    *   Predicate ::= '[' Expr ']'
    */
    private TNode ParsePredicate()
    {
        this.scanner!.PassToken(LexKind.LBracket);
        var opnd = ParseExpr();
        this.scanner.PassToken(LexKind.RBracket);
        return opnd;
    }
    #endregion

    #region Expressions
    /**************************************************************************************************/
    /*  Expressions                                                                                   */
    /**************************************************************************************************/

    /*
    *   Expr   ::= OrExpr
    *   OrExpr ::= AndExpr ('or' AndExpr)*
    *   AndExpr ::= EqualityExpr ('and' EqualityExpr)*
    *   EqualityExpr ::= RelationalExpr (('=' | '!=') RelationalExpr)*
    *   RelationalExpr ::= AdditiveExpr (('<' | '>' | '<=' | '>=') AdditiveExpr)*
    *   AdditiveExpr ::= MultiplicativeExpr (('+' | '-') MultiplicativeExpr)*
    *   MultiplicativeExpr ::= UnaryExpr (('*' | 'div' | 'mod') UnaryExpr)*
    *   UnaryExpr ::= ('-')* UnionExpr
    */
    private TNode ParseExpr()
    {
        return ParseSubExpr(callerPrec: 0);
    }

    private TNode ParseSubExpr(int callerPrec)
    {
        XPathOperator op;
        TNode opnd;

        ReadOnlySpan<byte> xpathOperatorPrecedence = new byte[]
        {
            /*Unknown    */ 0,
            /*Or         */ 1,
            /*And        */ 2,
            /*Eq         */ 3,
            /*Ne         */ 3,
            /*Lt         */ 4,
            /*Le         */ 4,
            /*Gt         */ 4,
            /*Ge         */ 4,
            /*Plus       */ 5,
            /*Minus      */ 5,
            /*Multiply   */ 6,
            /*Divide     */ 6,
            /*Modulo     */ 6,
            /*UnaryMinus */ 7,
            /*Union      */ 8,  // Not used
        };

        // Check for unary operators
        if (this.scanner!.Kind == LexKind.Minus)
        {
            op = XPathOperator.UnaryMinus;
            var opPrec = xpathOperatorPrecedence[(int)op];
            this.scanner.NextLex();
            opnd = this.builder!.Operator(op, ParseSubExpr(opPrec), default);
        }
        else
        {
            opnd = ParseUnionExpr();
        }

        // Process binary operators
        while (true)
        {
            op = (this.scanner.Kind <= LexKind.LastOperator) ? (XPathOperator)this.scanner.Kind : XPathOperator.Unknown;
            var opPrec = xpathOperatorPrecedence[(int)op];
            if (opPrec <= callerPrec)
            {
                break;
            }

            // Operator's precedence is greater than the one of our caller, so process it here
            this.scanner.NextLex();
            opnd = this.builder!.Operator(op, opnd, ParseSubExpr(callerPrec: opPrec));
        }
        return opnd;
    }

    /*
    *   UnionExpr ::= PathExpr ('|' PathExpr)*
    */
    private TNode ParseUnionExpr()
    {
        var startChar = this.scanner!.LexStart;
        var opnd1 = ParsePathExpr();

        if (this.scanner.Kind == LexKind.Union)
        {
            while (this.scanner.Kind == LexKind.Union)
            {
                this.scanner.NextLex();
                startChar = this.scanner.LexStart;
                var opnd2 = ParsePathExpr();
                PushPosInfo(startChar, this.scanner.PrevLexEnd);
                opnd1 = this.builder!.Union(opnd1, opnd2);
                PopPosInfo();
            }
        }
        return opnd1;
    }

    /*
    *   PathExpr ::= LocationPath | FilterExpr (('/' | '//') RelativeLocationPath )?
    */
    private TNode ParsePathExpr()
    {
        // Here we distinguish FilterExpr from LocationPath - the former starts with PrimaryExpr
        if (IsPrimaryExpr())
        {
            var startChar = this.scanner!.LexStart;
            var opnd = ParseFilterExpr();
            var endChar = this.scanner.PrevLexEnd;

            if (this.scanner.Kind == LexKind.Slash)
            {
                this.scanner.NextLex();
                PushPosInfo(startChar, endChar);
                opnd = this.builder!.Join(opnd, ParseRelativeLocationPath());
                PopPosInfo();
            }
            else if (this.scanner.Kind == LexKind.SlashSlash)
            {
                this.scanner.NextLex();
                PushPosInfo(startChar, endChar);
                opnd = this.builder!.Join(opnd,
                    this.builder.Join(
                        this.builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                        ParseRelativeLocationPath()
                    )
                );
                PopPosInfo();
            }
            return opnd;
        }
        else
        {
            return ParseLocationPath();
        }
    }

    /*
    *   FilterExpr ::= PrimaryExpr Predicate*
    */
    private TNode ParseFilterExpr()
    {
        var startChar = this.scanner!.LexStart;
        var opnd = ParsePrimaryExpr();
        var endChar = this.scanner.PrevLexEnd;

        while (this.scanner.Kind == LexKind.LBracket)
        {
            PushPosInfo(startChar, endChar);
            opnd = this.builder!.Predicate(opnd, ParsePredicate(), reverseStep: false);
            PopPosInfo();
        }
        return opnd;
    }

    private bool IsPrimaryExpr()
    {
        return (
            this.scanner!.Kind == LexKind.String ||
            this.scanner.Kind == LexKind.Number ||
            this.scanner.Kind == LexKind.Dollar ||
            this.scanner.Kind == LexKind.LParens ||
            (this.scanner.Kind == LexKind.Name && this.scanner.CanBeFunction && !IsNodeType(this.scanner))
        );
    }

    /*
    *   PrimaryExpr ::= Literal | Number | VariableReference | '(' Expr ')' | FunctionCall
    */
    private TNode ParsePrimaryExpr()
    {
        Debug.Assert(IsPrimaryExpr());
        TNode opnd;
        switch (this.scanner!.Kind)
        {
            case LexKind.String:
                opnd = this.builder!.Text(this.scanner.StringValue);
                this.scanner.NextLex();
                break;
            case LexKind.Number:
                opnd = this.builder!.Number(this.scanner.RawValue);
                this.scanner.NextLex();
                break;
            case LexKind.Dollar:
                var startChar = this.scanner.LexStart;
                this.scanner.NextLex();
                this.scanner.CheckToken(LexKind.Name);
                PushPosInfo(startChar, this.scanner.LexStart + this.scanner.LexSize);
                opnd = this.builder!.Variable(this.scanner.Prefix, this.scanner.Name);
                PopPosInfo();
                this.scanner.NextLex();
                break;
            case LexKind.LParens:
                this.scanner.NextLex();
                opnd = ParseExpr();
                this.scanner.PassToken(LexKind.RParens);
                break;
            default:
                Debug.Assert(
                    this.scanner.Kind == LexKind.Name && this.scanner.CanBeFunction && !IsNodeType(this.scanner),
                    "IsPrimaryExpr() returned true, but the lexeme is not recognized"
                );
                opnd = ParseFunctionCall();
                break;
        }
        return opnd;
    }

    /*
    *   FunctionCall ::= FunctionName '(' (Expr (',' Expr)* )? ')'
    */
    private TNode ParseFunctionCall()
    {
        var argList = new List<TNode>();
        var name = this.scanner!.Name;
        var prefix = this.scanner.Prefix;
        var startChar = this.scanner.LexStart;

        this.scanner.PassToken(LexKind.Name);
        this.scanner.PassToken(LexKind.LParens);

        if (this.scanner.Kind != LexKind.RParens)
        {
            while (true)
            {
                argList.Add(ParseExpr());
                if (this.scanner.Kind != LexKind.Comma)
                {
                    this.scanner.CheckToken(LexKind.RParens);
                    break;
                }
                this.scanner.NextLex();  // move off the ','
            }
        }

        this.scanner.NextLex();          // move off the ')'
        PushPosInfo(startChar, this.scanner.PrevLexEnd);
        var result = this.builder!.Function(prefix, name, argList);
        PopPosInfo();
        return result;
    }
    #endregion

    /**************************************************************************************************/
    /*  Helper methods                                                                                */
    /**************************************************************************************************/

    private void PushPosInfo(int startChar, int endChar)
    {
        this.posInfo.Push(startChar);
        this.posInfo.Push(endChar);
    }

    private void PopPosInfo()
    {
        this.posInfo.Pop();
        this.posInfo.Pop();
    }

    private void PopPosInfo(out int startChar, out int endChar)
    {
        endChar = this.posInfo.Pop();
        startChar = this.posInfo.Pop();
    }
}
