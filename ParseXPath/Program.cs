namespace ParseXPath;

using Brackets.XPath;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: ParseXPath <XPath expression>");
            return 1;
        }

        var pathParser = new XPathParser<string>();
        var pathExpr = pathParser.Parse(new XPathScanner(args[0]), new XPathStringBuilder(), LexKind.Eof);
        var pathEval = pathParser.Parse(new XPathScanner(args[0]), new XPathLogBuilder(), LexKind.Eof);

        Console.Out.WriteLine(pathExpr);
        Console.Out.WriteLine();
        Console.Out.WriteLine(pathEval);

        return 0;
    }
}
