namespace Brackets.Streaming;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public interface IRecordBuilder
{
    Encoding Encoding { get; }

    char Opener { get; }

    char Closer { get; }

    char Encloser { get; }

    ValueTask StartAsync();

    ValueTask StopAsync();

    ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken);
}
