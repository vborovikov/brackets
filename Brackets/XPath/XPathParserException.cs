namespace Brackets.XPath
{
    using System;
    using System.Text;

    [Serializable]
    public class XPathParserException : Exception
    {
        private enum TrimType
        {
            Left,
            Right,
            Middle,
        }

        public string? queryString;
        public int startChar;
        public int endChar;

        public XPathParserException(string queryString, int startChar, int endChar,
            string res, params string?[]? args) : this(CreateMessage(res, args))
        {
            this.queryString = queryString;
            this.startChar = startChar;
            this.endChar = endChar;
        }

        public XPathParserException(string message) : base(message)
        {
        }

        public XPathParserException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public XPathParserException() : base()
        {
        }

        public override string ToString()
        {
            string result = this.GetType().FullName!;
            string info = FormatDetailedMessage();
            if (!string.IsNullOrEmpty(info))
            {
                result += ": " + info;
            }
            if (this.StackTrace != null)
            {
                result += Environment.NewLine + this.StackTrace;
            }
            return result;
        }

        private static string CreateMessage(string res, params string?[]? args)
        {
            if (args == null)
            {
                return res;
            }

            return string.Format(res, args);
        }

        private static void AppendTrimmed(StringBuilder sb, string value, int startIndex, int count, TrimType trimType)
        {
            const int TrimSize = 32;
            const string TrimMarker = "...";

            if (count <= TrimSize)
            {
                sb.Append(value, startIndex, count);
            }
            else
            {
                switch (trimType)
                {
                    case TrimType.Left:
                        sb.Append(TrimMarker);
                        sb.Append(value, startIndex + count - TrimSize, TrimSize);
                        break;
                    case TrimType.Right:
                        sb.Append(value, startIndex, TrimSize);
                        sb.Append(TrimMarker);
                        break;
                    case TrimType.Middle:
                        sb.Append(value, startIndex, TrimSize / 2);
                        sb.Append(TrimMarker);
                        sb.Append(value, startIndex + count - TrimSize / 2, TrimSize / 2);
                        break;
                }
            }
        }

        private string? MarkOutError()
        {
            if (this.queryString == null || this.queryString.AsSpan().Trim(' ').IsEmpty)
            {
                return null;
            }

            int len = this.endChar - this.startChar;
            StringBuilder sb = new StringBuilder();

            AppendTrimmed(sb, this.queryString, 0, this.startChar, TrimType.Left);
            if (len > 0)
            {
                sb.Append(" -->");
                AppendTrimmed(sb, this.queryString, this.startChar, len, TrimType.Middle);
            }

            sb.Append("<-- ");
            AppendTrimmed(sb, this.queryString, this.endChar, this.queryString.Length - this.endChar, TrimType.Right);

            return sb.ToString();
        }

        private string FormatDetailedMessage()
        {
            string message = this.Message;
            string? error = MarkOutError();

            if (error != null && error.Length > 0)
            {
                if (message.Length > 0)
                {
                    message += Environment.NewLine;
                }
                message += error;
            }
            return message;
        }
    }
}