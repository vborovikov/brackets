namespace ParseXPath
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Brackets.XPath;
    using Termly;

    public class XPathLogBuilder : IXPathBuilder<string>
    {
        private readonly StringBuilder log;
        private int callNumber;

        public XPathLogBuilder()
        {
            this.log = new StringBuilder();
        }

        public void Begin()
        {
            this.callNumber = 0;
            this.log.Clear();
        }

        public string End(string result)
        {
            // pad single digit and double digit numbers
            if (this.callNumber >= 10)
            {
                PadCallNumbers(0, 9, new string(' ', this.callNumber >= 100 ? 2 : 1));
                if (this.callNumber >= 100)
                {
                    PadCallNumbers(10, 99, " ");
                }
            }
            // remove last new line
            this.log.Length -= Environment.NewLine.Length;

            return this.log.ToString();
        }

        public string Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string? prefix, string? name) =>
            Log($"{nameof(Axis).InColor(XPathColors.Operation)}({xpathAxis.InColor(XPathColors.Parameter)}, {nodeType.InColor(XPathColors.Parameter)}, \"{prefix.InColor(XPathColors.Argument)}\", \"{name.InColor(XPathColors.Argument)}\")");

        public string Function(string? prefix, string name, IEnumerable<string> args) =>
            Log($"{nameof(Function).InColor(XPathColors.Operation)}(\"{prefix.InColor(XPathColors.Argument)}\", \"{name.InColor(XPathColors.Argument)}\", [{string.Join(", ", args.Select(s => $"{{{s}}}"))}])");

        public string Join(string left, string right) =>
            Log($"{nameof(Join).InColor(XPathColors.Operation)}({{{left}}}, {{{right}}})");

        public string Number(string value) =>
            Log($"{nameof(Number).InColor(XPathColors.Operation)}(\"{value.InColor(XPathColors.Argument)}\")");

        public string Operator(XPathOperator op, string left, string? right) =>
            Log($"{nameof(Operator).InColor(XPathColors.Operation)}({op.InColor(XPathColors.Parameter)}, {{{left}}}, {{{right}}})");

        public string Predicate(string node, string condition, bool reverseStep) =>
            Log($"{nameof(Predicate).InColor(XPathColors.Operation)}({{{node}}}, {{{condition}}}, {reverseStep.InColor(XPathColors.Argument)})");

        public string Text(string value) =>
            Log($"{nameof(Text).InColor(XPathColors.Operation)}(\"{value.InColor(XPathColors.Argument)}\")");

        public string Union(string left, string right) =>
            Log($"{nameof(Union).InColor(XPathColors.Operation)}({{{left}}}, {{{right}}})");

        public string Variable(string? prefix, string name) =>
            Log($"{nameof(Variable).InColor(XPathColors.Operation)}(\"{prefix.InColor(XPathColors.Argument)}\", \"{name.InColor(XPathColors.Argument)}\")");

        private string Log(string expr)
        {
            ++this.callNumber;
            var callNumberStr = this.callNumber.ToString().InColor(XPathColors.CallNumber);
            var call = callNumberStr + ". " + expr;
            this.log.AppendLine(call);
            return callNumberStr!;
        }

        private void PadCallNumbers(int minNumber, int maxNumber, string padding)
        {
            if (minNumber <= 1 && (this.log[0] == '0' || this.log[0] == '1'))
            {
                this.log.Insert(0, padding);
            }

            for (var i = minNumber; i <= maxNumber; ++i)
            {
                this.log.Replace(Environment.NewLine + i + ".", Environment.NewLine + padding + i + ".");
            }
        }
    }
}
