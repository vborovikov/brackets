namespace Brackets.Parsing;

using System.Runtime.CompilerServices;

static class XmlCharType
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhiteSpace(char ch) => ch <= ' ' && (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNCNameSingleChar(char ch) => IsStartNCNameSingleChar(ch) ||
        ((uint)(ch - '0') <= 9u) || ch == '-' || ch == '.' || ch == 0xB7 ||
        (ch is >= '\u0300' and <= '\u036F') || (ch is >= '\u203F' and <= '\u2040');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStartNCNameSingleChar(char ch) => ((uint)((ch | 0x20) - 'a') <= 'z' - 'a') || ch == ':' || ch == '_' ||
        (ch is >= '\u00C0' and <= '\u00D6') ||
        (ch is >= '\u00D8' and <= '\u00F6') ||
        (ch is >= '\u00F8' and <= '\u02FF') ||
        (ch is >= '\u0370' and <= '\u037D') ||
        (ch is >= '\u037F' and <= '\u1FFF') ||
        (ch is >= '\u200C' and <= '\u200D') ||
        (ch is >= '\u2070' and <= '\u218F') ||
        (ch is >= '\u2C00' and <= '\u2FEF') ||
        (ch is >= '\u3001' and <= '\uD7FF') ||
        (ch is >= '\uF900' and <= '\uFDCF') ||
        (ch is >= '\uFDF0' and <= '\uFFFD');
}
