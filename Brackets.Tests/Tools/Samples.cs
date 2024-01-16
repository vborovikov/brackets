namespace Brackets.Tests.Tools;

using System.Reflection;
using System.Text;

static class Samples
{
    private static Assembly assembly;

    static Samples()
    {
        assembly = Assembly.GetExecutingAssembly();
    }

    public static string GetString(string fileName)
    {
        using var fileStream = GetStream(fileName);
        using var fileReader = new StreamReader(fileStream, Encoding.UTF8);
        return fileReader.ReadToEnd();
    }

    public static Stream GetStream(string fileName)
    {
        return assembly.GetManifestResourceStream($"Brackets.Tests.Samples.{fileName}");
    }
}
