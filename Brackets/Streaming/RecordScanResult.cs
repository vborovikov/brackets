namespace Brackets.Streaming
{
    /// <summary>
    /// Represents the result of scanning a record.
    /// </summary>
    public enum RecordScanResult
    {
        /// <summary>
        /// Indicates that the internal buffer is empty and the record scanning might not be complete.
        /// </summary>
        Empty,
        /// <summary>
        /// Indicates that the record scanning is complete.
        /// </summary>
        EndOfRecord,
        /// <summary>
        /// Indicates that the end of a record buffer has been reached.
        /// </summary>
        EndOfData,
    }
}
