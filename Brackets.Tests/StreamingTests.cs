namespace Brackets.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Brackets.Streaming;

[TestClass]
public class StreamingTests
{
    private sealed class LineCollector : IRecordBuilder
    {
        private readonly List<string> lines = new();

        public Encoding Encoding => Encoding.Default;
        public List<string> Lines => lines;

        public ValueTask<int> BuildAsync(ReadOnlySpan<char> recordSpan, CancellationToken cancellationToken)
        {
            this.lines.Add(recordSpan.ToString());
            return ValueTask.FromResult(recordSpan.Length);
        }

        public ValueTask StartAsync()
        {
            this.lines.Clear();
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task ScanAsync_ManyLines_Scanned()
    {
        using var stream = CreateStream(
            """
            abc
            123

            bbbb
            f
            aaa
            """);

        var collector = new LineCollector();
        await RecordScanner.ScanAsync(stream, collector, default);

        Assert.AreEqual(6, collector.Lines.Count);
        CollectionAssert.AreEquivalent(new[] { "abc", "123", "", "bbbb", "f", "aaa" }, collector.Lines);
    }

    private Stream CreateStream(string str, Encoding encoding = null)
    {
        return new MemoryStream((encoding ?? Encoding.Default).GetBytes(str));
    }
}
