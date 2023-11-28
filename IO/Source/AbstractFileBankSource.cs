using System.Collections.Concurrent;
using BiSharper.Rv.Bank.Models;

namespace BiSharper.Rv.Bank.IO.Source;

public abstract class AbstractFileBankSource(FileBankReader reader)
{
    protected readonly object SynchronizationLock = new();
    protected readonly ConcurrentDictionary<string, BankEntry> SourceEntries = new();
    protected readonly ConcurrentDictionary<string, string> SourceProperties = new();


    public abstract string BankName { get; }

    protected readonly FileBankReader Reader = reader;

    public abstract bool Writable { get; }

    public bool Synchronized
    {
        get
        {
            lock (SynchronizationLock) return _synchronized;
        }
        protected set
        {
            lock (SynchronizationLock) _synchronized = value;
        }
    }
    private bool _synchronized = true;

    public abstract long BankLength { get; }

    protected abstract void PopulateFromSource();
    protected abstract Task<Stream> CreateStreamAsync();


    public IEnumerable<KeyValuePair<string, BankEntry>> Entries => SourceEntries.AsEnumerable();

    public virtual async Task<ReadOnlyMemory<byte>> GetRawDataAsync(BankEntry entry, CancellationToken token = default)
    {
        var readLength = entry.BufferLength;
        var offset = entry.Offset;


        if (!SourceEntries.ContainsKey(entry.Name))
            throw new IOException(
                $"[FileBankSource] The provided entry does not belong to this source.");

        if (BankLength - offset < readLength)
            throw new IOException(
                $"[FileBankSource] There is not enough room to read {readLength} byte(s) from offset {offset}");

        return await GetContentAsync(offset, readLength, token);
    }

    protected virtual async Task<ReadOnlyMemory<byte>> GetContentAsync(long offset, long length,
        CancellationToken token = default)
    {
        if (offset < 0 || offset > BankLength)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (length < 0 || offset + length > BankLength)
            throw new ArgumentOutOfRangeException(nameof(length));
        var buffer = new byte[length];

        await using (var stream = await CreateStreamAsync())
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var read = await stream.ReadAsync(buffer.AsMemory(0, (int)length), token);

            if (read < length) Array.Resize(ref buffer, read);
        }

        return new ReadOnlyMemory<byte>(buffer);
    }


}