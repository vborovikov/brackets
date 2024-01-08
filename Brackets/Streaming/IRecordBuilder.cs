namespace Brackets.Streaming;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public interface IRecordBuilder
{
    Encoding Encoding { get; }

    ValueTask StartAsync();

    ValueTask StopAsync();

    ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken);
}

public interface IMultilineBuilder : IRecordBuilder
{
    char Encloser { get; }
}

public interface IElementBuilder : IRecordBuilder
{
    char Opener { get; }

    char Closer { get; }
}
