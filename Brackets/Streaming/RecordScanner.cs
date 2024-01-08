namespace Brackets.Streaming;

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

public static class RecordScanner
{
    public static async Task ScanAsync(string filePath, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(filePath,
            FileMode.Open, FileAccess.Read, FileShare.Read, RecordBuffer.FileBufferLength,
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
            BufferSize = RecordBuffer.FileBufferLength,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        });
        await ScanAsync(fileStream, builder, cancellationToken);
    }

    public static Task ScanAsync(Stream stream, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        var dataPipe = new Pipe();

        var loading = LoadAsync(stream, dataPipe.Writer, cancellationToken);
        var reading = builder switch
        {
            IElementBuilder elementBuilder => ReadElementsAsync(dataPipe.Reader, elementBuilder, cancellationToken),
            IMultilineBuilder multilineBuilder => ReadMultilinesAsync(dataPipe.Reader, multilineBuilder, cancellationToken),
            _ => ReadLinesAsync(dataPipe.Reader, builder, cancellationToken)
        };

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

    private static async Task ReadElementsAsync(PipeReader reader, IElementBuilder builder, CancellationToken cancellationToken)
    {
        await builder.StartAsync();
        var exception = default(Exception);

        try
        {
            using var recordBuffer = new RecordBuffer(RecordBuffer.DefaultBufferLength);
            using var recordDecoder = new RecordDecoder(builder.Encoding, RecordBuffer.DefaultBufferLength);
            var enclosed = false;

            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                if (readResult.IsCanceled)
                    break;

                recordDecoder.Decode(readResult.Buffer);
                var recordRead = RecordReadResult.Empty;
                do
                {
                    recordRead = recordDecoder.TryReadElement(recordBuffer, builder.Opener, builder.Closer, ref enclosed, out var recordLength);
                    if (recordRead == RecordReadResult.EndOfRecord)
                    {
                        var charsConsumed = await builder.BuildAsync(recordBuffer.PreviewRecord(recordLength), cancellationToken);
                        recordBuffer.Offset(recordLength, charsConsumed);
                        recordDecoder.TryChangeEncoding(builder.Encoding, forceDecode: true);
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

    private static async Task ReadMultilinesAsync(PipeReader reader, IMultilineBuilder builder, CancellationToken cancellationToken)
    {
        await builder.StartAsync();
        var exception = default(Exception);

        try
        {
            using var recordBuffer = new RecordBuffer(RecordBuffer.FileBufferLength);
            using var recordDecoder = new RecordDecoder(builder.Encoding, RecordBuffer.FileBufferLength);
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
                    recordRead = recordDecoder.TryReadMultiline(recordBuffer, builder.Encloser, ref ignoreLineBreak, out var recordLength);
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

    private static async Task ReadLinesAsync(PipeReader reader, IRecordBuilder builder, CancellationToken cancellationToken)
    {
        await builder.StartAsync();
        var exception = default(Exception);

        try
        {
            using var recordBuffer = new RecordBuffer(RecordBuffer.FileBufferLength);
            using var recordDecoder = new RecordDecoder(builder.Encoding, RecordBuffer.FileBufferLength);

            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                if (readResult.IsCanceled)
                    break;

                recordDecoder.Decode(readResult.Buffer, builder.Encoding);
                var recordRead = RecordReadResult.Empty;
                do
                {
                    recordRead = recordDecoder.TryReadLine(recordBuffer, out var recordLength);
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
