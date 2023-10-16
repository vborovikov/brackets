namespace Brackets.Streaming;

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

static class RecordScanner
{
    public static async Task ScanAsync(string filePath, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(filePath,
            FileMode.Open, FileAccess.Read, FileShare.Read, RecordBuffer.DefaultBufferLength,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await ScanAsync(fileStream, builder, cancellationToken);
    }

    public static async Task ScanAsync(FileInfo fileInfo, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        await using var fileStream = fileInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = RecordBuffer.DefaultBufferLength,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        });
        await ScanAsync(fileStream, builder, cancellationToken);
    }

    public static Task ScanAsync(Stream stream, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        var dataPipe = new Pipe();

        var loading = LoadAsync(stream, dataPipe.Writer, cancellationToken);
        var reading = builder.Opener != builder.Closer ?
            ReadRecordsAsync(dataPipe.Reader, builder, cancellationToken) :
            ReadLinesAsync(dataPipe.Reader, builder, cancellationToken);

        return Task.WhenAll(loading, reading);
    }

    private static async Task LoadAsync(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
    {
        const int minimumBufferSize = RecordBuffer.DefaultBufferLength;
        var exception = default(Exception);
        while (true)
        {
            var memory = writer.GetMemory(minimumBufferSize);
            try
            {
                var bytesRead = await stream.ReadAsync(memory, cancellationToken);
                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);
            }
            catch (Exception x)
            {
                exception = x;
                break;
            }

            var flushResult = await writer.FlushAsync(cancellationToken);
            if (flushResult.IsCanceled || flushResult.IsCompleted)
                break;
        }

        await writer.CompleteAsync(exception);
    }

    private static async Task ReadRecordsAsync(PipeReader reader, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        await builder.StartAsync();
        var exception = default(Exception);

        try
        {
            using var recordBuffer = new RecordBuffer();
            var recordDecoder = new RecordDecoder(builder.Encoding);
            var enclosed = false;

            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                if (readResult.IsCanceled)
                    break;

                recordDecoder.Decode(readResult.Buffer, builder.Encoding);
                var recordRead = RecordReadResult.Empty;
                do
                {
                    recordRead = recordDecoder.TryReadRecord(recordBuffer, builder.Opener, builder.Closer, ref enclosed, out var recordLength);
                    if (recordRead == RecordReadResult.EndOfRecord)
                    {
                        var charsConsumed = await builder.BuildAsync(recordBuffer.PreviewRecord(recordLength), cancellationToken);
                        recordBuffer.Offset(recordLength, charsConsumed);
                    }
                    else
                    {
                        recordBuffer.Offset(recordLength);
                    }
                } while (recordRead != RecordReadResult.Empty);

                reader.AdvanceTo(recordDecoder.SequencePosition);
                if (readResult.IsCompleted)
                {
                    if (recordBuffer.CanMakeRecord)
                    {
                        await builder.BuildAsync(recordBuffer.MakeRecord(), cancellationToken);
                    }

                    break;
                }
            }
        }
        catch (Exception x)
        {
            exception = x;
        }
        finally
        {
            await reader.CompleteAsync(exception);
            await builder.StopAsync();
        }
    }

    private static async Task ReadLinesAsync(PipeReader reader, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        await builder.StartAsync();
        var exception = default(Exception);

        try
        {
            using var recordBuffer = new RecordBuffer();
            var recordDecoder = new RecordDecoder(builder.Encoding);
            var ignoreLineBreak = false;

            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                if (readResult.IsCanceled)
                    break;

                recordDecoder.Decode(readResult.Buffer, builder.Encoding);
                var recordRead = RecordReadResult.Empty;
                do
                {
                    recordRead = recordDecoder.TryReadLine(recordBuffer, builder.Encloser, ref ignoreLineBreak, out var recordLength);
                    if (recordRead == RecordReadResult.EndOfRecord)
                    {
                        await builder.BuildAsync(recordBuffer.MakeRecord(recordLength), cancellationToken);
                    }
                    else
                    {
                        recordBuffer.Offset(recordLength);
                    }
                } while (recordRead != RecordReadResult.Empty);

                reader.AdvanceTo(recordDecoder.SequencePosition);
                if (readResult.IsCompleted)
                {
                    if (recordBuffer.CanMakeRecord)
                    {
                        await builder.BuildAsync(recordBuffer.MakeRecord(), cancellationToken);
                    }

                    break;
                }
            }
        }
        catch (Exception x)
        {
            exception = x;
        }
        finally
        {
            await reader.CompleteAsync(exception);
            await builder.StopAsync();
        }
    }
}
