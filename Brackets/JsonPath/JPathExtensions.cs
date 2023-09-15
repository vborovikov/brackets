namespace Brackets.JPath
{
    using System.Collections.Generic;
    using System.Text.Json;

    public static class JPathExtensions
    {
        public static JsonElement? SelectToken(this JsonElement document, string path, bool errorWhenNoMatch = false)
        {
            var p = new JPathParser(path);

            JsonElement? token = null;
            foreach (var t in p.Evaluate(document, document, errorWhenNoMatch))
            {
                if (token != null)
                {
                    throw new JsonException("Path returned multiple tokens.");
                }

                token = t;
            }

            return token;
        }

        public static IEnumerable<JsonElement> SelectTokens(this JsonElement document, string path, bool errorWhenNoMatch = false)
        {
            var p = new JPathParser(path);
            return p.Evaluate(document, document, errorWhenNoMatch);
        }

        public static JsonElement? SelectToken(this JsonDocument document, string path, bool errorWhenNoMatch = false) =>
            document.RootElement.SelectToken(path, errorWhenNoMatch);

        public static IEnumerable<JsonElement> SelectTokens(this JsonDocument document, string path, bool errorWhenNoMatch = false) =>
            document.RootElement.SelectTokens(path, errorWhenNoMatch);
    }
}
