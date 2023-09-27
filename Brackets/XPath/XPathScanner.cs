namespace Brackets.XPath;

using System.Diagnostics;
using Parsing;

// Extends XPathOperator enumeration
internal enum LexKind
{
    Unknown,        // Unknown lexeme
    Or,             // Operator 'or'
    And,            // Operator 'and'
    Eq,             // Operator '='
    Ne,             // Operator '!='
    Lt,             // Operator '<'
    Le,             // Operator '<='
    Gt,             // Operator '>'
    Ge,             // Operator '>='
    Plus,           // Operator '+'
    Minus,          // Operator '-'
    Multiply,       // Operator '*'
    Divide,         // Operator 'div'
    Modulo,         // Operator 'mod'
    UnaryMinus,     // Not used
    Union,          // Operator '|'
    LastOperator = Union,

    DotDot,         // '..'
    ColonColon,     // '::'
    SlashSlash,     // Operator '//'
    Number,         // Number (numeric literal)
    Axis,           // AxisName

    Name,           // NameTest, NodeType, FunctionName, AxisName, second part of VariableReference
    String,         // Literal (string literal)
    Eof,            // End of the expression

    FirstStringable = Name,
    LastNonChar = Eof,

    LParens = '(',
    RParens = ')',
    LBracket = '[',
    RBracket = ']',
    Dot = '.',
    At = '@',
    Comma = ',',

    Star = '*',      // NameTest
    Slash = '/',      // Operator '/'
    Dollar = '$',      // First part of VariableReference
    RBrace = '}',      // Used for AVTs
};

sealed class XPathScanner
{
    private readonly string xpathExpr;
    private int curIndex;
    private char curChar;
    private LexKind kind;
    private string? name;
    private string? prefix;
    private string? stringValue;
    private bool canBeFunction;
    private int lexStart;
    private int prevLexEnd;
    private LexKind prevKind;
    private XPathAxis axis;

    public XPathScanner(string xpathExpr) : this(xpathExpr, 0) { }

    public XPathScanner(string xpathExpr, int startFrom)
    {
        Debug.Assert(xpathExpr != null);
        this.xpathExpr = xpathExpr;
        this.kind = LexKind.Unknown;
        SetSourceIndex(startFrom);
        NextLex();
    }

    public string Source { get { return this.xpathExpr; } }
    public LexKind Kind { get { return this.kind; } }
    public int LexStart { get { return this.lexStart; } }
    public int LexSize { get { return this.curIndex - this.lexStart; } }
    public int PrevLexEnd { get { return this.prevLexEnd; } }

    private void SetSourceIndex(int index)
    {
        Debug.Assert(0 <= index && index <= this.xpathExpr.Length);
        this.curIndex = index - 1;
        NextChar();
    }

    private void NextChar()
    {
        Debug.Assert(-1 <= this.curIndex && this.curIndex < this.xpathExpr.Length);
        this.curIndex++;
        if (this.curIndex < this.xpathExpr.Length)
        {
            this.curChar = this.xpathExpr[this.curIndex];
        }
        else
        {
            Debug.Assert(this.curIndex == this.xpathExpr.Length);
            this.curChar = '\0';
        }
    }

    public string Name
    {
        get
        {
            Debug.Assert(this.kind == LexKind.Name);
            Debug.Assert(this.name != null);
            return this.name;
        }
    }

    public string Prefix
    {
        get
        {
            Debug.Assert(this.kind == LexKind.Name);
            Debug.Assert(this.prefix != null);
            return this.prefix;
        }
    }

    public string RawValue
    {
        get
        {
            if (this.kind == LexKind.Eof)
            {
                return LexKindToString(this.kind);
            }
            else
            {
                return this.xpathExpr.Substring(this.lexStart, this.curIndex - this.lexStart);
            }
        }
    }

    public string StringValue
    {
        get
        {
            Debug.Assert(this.kind == LexKind.String);
            Debug.Assert(this.stringValue != null);
            return this.stringValue;
        }
    }

    // Returns true if the character following an QName (possibly after intervening
    // ExprWhitespace) is '('. In this case the token must be recognized as a NodeType
    // or a FunctionName unless it is an OperatorName. This distinction cannot be done
    // without knowing the previous lexeme. For example, "or" in "... or (1 != 0)" may
    // be an OperatorName or a FunctionName.
    public bool CanBeFunction
    {
        get
        {
            Debug.Assert(this.kind == LexKind.Name);
            return this.canBeFunction;
        }
    }

    public XPathAxis Axis
    {
        get
        {
            Debug.Assert(this.kind == LexKind.Axis);
            Debug.Assert(this.axis != XPathAxis.Unknown);
            return this.axis;
        }
    }

    private void SkipSpace()
    {
        while (XmlCharType.IsWhiteSpace(this.curChar))
        {
            NextChar();
        }
    }

    private static bool IsAsciiDigit(char ch)
    {
        return unchecked((uint)(ch - '0')) <= 9;
    }

    public void NextLex()
    {
        this.prevLexEnd = this.curIndex;
        this.prevKind = this.kind;
        SkipSpace();
        this.lexStart = this.curIndex;

        switch (this.curChar)
        {
            case '\0':
                this.kind = LexKind.Eof;
                return;
            case '(':
            case ')':
            case '[':
            case ']':
            case '@':
            case ',':
            case '$':
            case '}':
                this.kind = (LexKind)this.curChar;
                NextChar();
                break;
            case '.':
                NextChar();
                if (this.curChar == '.')
                {
                    this.kind = LexKind.DotDot;
                    NextChar();
                }
                else if (IsAsciiDigit(this.curChar))
                {
                    SetSourceIndex(this.lexStart);
                    goto case '0';
                }
                else
                {
                    this.kind = LexKind.Dot;
                }
                break;
            case ':':
                NextChar();
                if (this.curChar == ':')
                {
                    this.kind = LexKind.ColonColon;
                    NextChar();
                }
                else
                {
                    this.kind = LexKind.Unknown;
                }
                break;
            case '*':
                this.kind = LexKind.Star;
                NextChar();
                CheckOperator(true);
                break;
            case '/':
                NextChar();
                if (this.curChar == '/')
                {
                    this.kind = LexKind.SlashSlash;
                    NextChar();
                }
                else
                {
                    this.kind = LexKind.Slash;
                }
                break;
            case '|':
                this.kind = LexKind.Union;
                NextChar();
                break;
            case '+':
                this.kind = LexKind.Plus;
                NextChar();
                break;
            case '-':
                this.kind = LexKind.Minus;
                NextChar();
                break;
            case '=':
                this.kind = LexKind.Eq;
                NextChar();
                break;
            case '!':
                NextChar();
                if (this.curChar == '=')
                {
                    this.kind = LexKind.Ne;
                    NextChar();
                }
                else
                {
                    this.kind = LexKind.Unknown;
                }
                break;
            case '<':
                NextChar();
                if (this.curChar == '=')
                {
                    this.kind = LexKind.Le;
                    NextChar();
                }
                else
                {
                    this.kind = LexKind.Lt;
                }
                break;
            case '>':
                NextChar();
                if (this.curChar == '=')
                {
                    this.kind = LexKind.Ge;
                    NextChar();
                }
                else
                {
                    this.kind = LexKind.Gt;
                }
                break;
            case '"':
            case '\'':
                this.kind = LexKind.String;
                ScanString();
                break;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                this.kind = LexKind.Number;
                ScanNumber();
                break;
            default:
                if (XmlCharType.IsStartNCNameSingleChar(this.curChar))
                {
                    this.kind = LexKind.Name;
                    this.name = ScanNCName();
                    this.prefix = string.Empty;
                    this.canBeFunction = false;
                    this.axis = XPathAxis.Unknown;
                    var colonColon = false;
                    var saveSourceIndex = this.curIndex;

                    // "foo:bar" or "foo:*" -- one lexeme (no spaces allowed)
                    // "foo::" or "foo ::"  -- two lexemes, reported as one (AxisName)
                    // "foo:?" or "foo :?"  -- lexeme "foo" reported
                    if (this.curChar == ':')
                    {
                        NextChar();
                        if (this.curChar == ':')
                        {   // "foo::" -> OperatorName, AxisName
                            NextChar();
                            colonColon = true;
                            SetSourceIndex(saveSourceIndex);
                        }
                        else
                        {                // "foo:bar", "foo:*" or "foo:?"
                            if (this.curChar == '*')
                            {
                                NextChar();
                                this.prefix = this.name;
                                this.name = "*";
                            }
                            else if (XmlCharType.IsStartNCNameSingleChar(this.curChar))
                            {
                                this.prefix = this.name;
                                this.name = ScanNCName();
                                // Look ahead for '(' to determine whether QName can be a FunctionName
                                saveSourceIndex = this.curIndex;
                                SkipSpace();
                                this.canBeFunction = (this.curChar == '(');
                                SetSourceIndex(saveSourceIndex);
                            }
                            else
                            {            // "foo:?" -> OperatorName, NameTest
                                // Return "foo" and leave ":" to be reported later as an unknown lexeme
                                SetSourceIndex(saveSourceIndex);
                            }
                        }
                    }
                    else
                    {
                        SkipSpace();
                        if (this.curChar == ':')
                        {   // "foo ::" or "foo :?"
                            NextChar();
                            if (this.curChar == ':')
                            {
                                NextChar();
                                colonColon = true;
                            }
                            SetSourceIndex(saveSourceIndex);
                        }
                        else
                        {
                            this.canBeFunction = (this.curChar == '(');
                        }
                    }
                    if (!CheckOperator(false) && colonColon)
                    {
                        this.axis = CheckAxis();
                    }
                }
                else
                {
                    this.kind = LexKind.Unknown;
                    NextChar();
                }
                break;
        }
    }

    private bool CheckOperator(bool star)
    {
        LexKind opKind;

        if (star)
        {
            opKind = LexKind.Multiply;
        }
        else
        {
            Debug.Assert(this.prefix != null);
            Debug.Assert(this.name != null);
            if (this.prefix.Length != 0 || this.name.Length > 3)
                return false;

            switch (this.name)
            {
                case "or": opKind = LexKind.Or; break;
                case "and": opKind = LexKind.And; break;
                case "div": opKind = LexKind.Divide; break;
                case "mod": opKind = LexKind.Modulo; break;
                default: return false;
            }
        }

        // If there is a preceding token and the preceding token is not one of '@', '::', '(', '[', ',' or an Operator,
        // then a '*' must be recognized as a MultiplyOperator and an NCName must be recognized as an OperatorName.
        if (this.prevKind <= LexKind.LastOperator)
            return false;

        switch (this.prevKind)
        {
            case LexKind.Slash:
            case LexKind.SlashSlash:
            case LexKind.At:
            case LexKind.ColonColon:
            case LexKind.LParens:
            case LexKind.LBracket:
            case LexKind.Comma:
            case LexKind.Dollar:
                return false;
        }

        this.kind = opKind;
        return true;
    }

    private XPathAxis CheckAxis()
    {
        this.kind = LexKind.Axis;
        switch (this.name)
        {
            case "ancestor": return XPathAxis.Ancestor;
            case "ancestor-or-self": return XPathAxis.AncestorOrSelf;
            case "attribute": return XPathAxis.Attribute;
            case "child": return XPathAxis.Child;
            case "descendant": return XPathAxis.Descendant;
            case "descendant-or-self": return XPathAxis.DescendantOrSelf;
            case "following": return XPathAxis.Following;
            case "following-sibling": return XPathAxis.FollowingSibling;
            case "namespace": return XPathAxis.Namespace;
            case "parent": return XPathAxis.Parent;
            case "preceding": return XPathAxis.Preceding;
            case "preceding-sibling": return XPathAxis.PrecedingSibling;
            case "self": return XPathAxis.Self;
            default: this.kind = LexKind.Name; return XPathAxis.Unknown;
        }
    }

    private void ScanNumber()
    {
        Debug.Assert(IsAsciiDigit(this.curChar) || this.curChar == '.');
        while (IsAsciiDigit(this.curChar))
        {
            NextChar();
        }
        if (this.curChar == '.')
        {
            NextChar();
            while (IsAsciiDigit(this.curChar))
            {
                NextChar();
            }
        }
        if ((this.curChar & (~0x20)) == 'E')
        {
            NextChar();
            if (this.curChar == '+' || this.curChar == '-')
            {
                NextChar();
            }
            while (IsAsciiDigit(this.curChar))
            {
                NextChar();
            }
            throw CreateException("Scientific notation is not allowed.");
        }
    }

    private void ScanString()
    {
        var startIdx = this.curIndex + 1;
        var endIdx = this.xpathExpr.IndexOf(this.curChar, startIdx);

        if (endIdx < 0)
        {
            SetSourceIndex(this.xpathExpr.Length);
            throw CreateException("String literal was not closed.");
        }

        this.stringValue = this.xpathExpr.Substring(startIdx, endIdx - startIdx);
        SetSourceIndex(endIdx + 1);
    }

    private string ScanNCName()
    {
        Debug.Assert(XmlCharType.IsStartNCNameSingleChar(this.curChar));
        var start = this.curIndex;
        while (true)
        {
            if (XmlCharType.IsNCNameSingleChar(this.curChar))
            {
                NextChar();
            }
            else
            {
                break;
            }
        }
        return this.xpathExpr.Substring(start, this.curIndex - start);
    }

    public void PassToken(LexKind t)
    {
        CheckToken(t);
        NextLex();
    }

    public void CheckToken(LexKind t)
    {
        Debug.Assert(LexKind.FirstStringable <= t);
        if (this.kind != t)
        {
            if (t == LexKind.Eof)
            {
                throw CreateException("Expected end of the expression, found '{0}'.", this.RawValue);
            }
            else
            {
                throw CreateException("Expected token '{0}', found '{1}'.", LexKindToString(t), this.RawValue);
            }
        }
    }

    // May be called for the following tokens: Name, String, Eof, Comma, LParens, RParens, LBracket, RBracket, RBrace
    private static string LexKindToString(LexKind t)
    {
        Debug.Assert(LexKind.FirstStringable <= t);

        if (LexKind.LastNonChar < t)
        {
            Debug.Assert("()[].@,*/$}".Contains((char)t));
            return char.ToString((char)t);
        }

        switch (t)
        {
            case LexKind.Name: return "<name>";
            case LexKind.String: return "<string literal>";
            case LexKind.Eof: return "<eof>";
            default:
                Debug.Fail($"Unexpected LexKind: {t}");
                return string.Empty;
        }
    }

    public XPathParserException CreateException(string resId, params string[] args)
    {
        return new XPathParserException(this.xpathExpr, this.lexStart, this.curIndex, resId, args);
    }
}
