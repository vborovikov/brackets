namespace Brackets.Primitives;

using System.Runtime.CompilerServices;

static class XmlCharType
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhiteSpace(char ch) => ch <= ' ' && (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNCNameSingleChar(char ch) => IsStartNCNameSingleChar(ch) ||
        ((uint)(ch - '0') <= 9u) || ch == '-' || ch == '.' || ch == 0xB7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStartNCNameSingleChar(char ch) => ((uint)((ch | 0x20) - 'a') <= 'z' - 'a') || ch == '_';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNameSingleChar(char ch) => IsNCNameSingleChar(ch) || ch == ':';
}
