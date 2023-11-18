using System.Buffers;
using BiSharper.Common.Compression;
using BiSharper.Rv.Bank.Models;

namespace BiSharper.Rv.Bank;

public partial class FileBank
{
    public async Task<byte[]?> ReadRawAsync(BankEntry meta, CancellationToken cancellationToken = default)
    {
        await _readLock.WaitAsync(cancellationToken);
        try
        {
            if (meta.Offset > _binaryLength)
            {
                return null;
            }
            if (meta.BufferLength == 0)
            {
                return Array.Empty<byte>();
            }
            _input.Seek(meta.Offset, SeekOrigin.Begin);
            var bufferSize = (int)meta.BufferLength;
            using var ms = new MemoryStream(bufferSize);
            await _input.CopyToAsync(ms, cancellationToken);
            return ms.ToArray();
        }
        finally
        {
            _readLock.Release();
        }
    }
    
    public async Task<byte[]?> ReadRawAsync(string name, CancellationToken cancellationToken = default) => GetEntry(name) is not { } meta ? null : await ReadRawAsync(meta, cancellationToken);
    
    public async Task<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) => GetEntry(name) is not { } meta ? null : await ReadAsync(meta, cancellationToken);
    
    public async Task<byte[]?> ReadAsync(BankEntry meta, CancellationToken cancellationToken = default)
    {
        var rentedBuffer = await ReadRawAsync(meta, cancellationToken); 
        if (rentedBuffer == null || rentedBuffer.Length == 0)
        {
            return rentedBuffer;
        }

        try
        {
            switch (meta.Mime)
            {
                case EntryMime.Decompressed:
                    return rentedBuffer;
                case EntryMime.Compressed:
                {
                    var goal = rentedBuffer.Length;
                    return BisCompatableLZSS.Decode(rentedBuffer, out var decompressed, (uint)goal) != goal ? rentedBuffer : decompressed;
                }
                case EntryMime.Encrypted:
                    throw new Exception("Encrypted entries may not be read!");
                case EntryMime.Version:
                    throw new Exception("Version entries may not be read!");
                default:
                    throw new Exception("Unknown entry may not be read!");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
        
    }
    
}