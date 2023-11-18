namespace BiSharper.Rv.Bank.Models;

public sealed record BankEntry
{
    public required FileBank Owner { get; init; }
    public required EntryMime Mime { get; init; } = EntryMime.Decompressed;
    public required string Name { get; init; }
    public required uint Length { get; init; }
    public required long Offset { get; set; }
    public required uint Timestamp { get; init; }
    public required uint BufferLength { get; init; }

    internal BankEntry()
    {
            
    }

    public byte[]? Read() => Owner.Read(this);
    
    public byte[]? ReadRaw() => Owner.ReadRaw(this);
    
    public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default) => 
        await Owner.ReadAsync(this, cancellationToken).ConfigureAwait(false);
    
    public async Task<byte[]?> ReadRawAsync(CancellationToken cancellationToken = default) =>
        await Owner.ReadRawAsync(this, cancellationToken).ConfigureAwait(false);
}