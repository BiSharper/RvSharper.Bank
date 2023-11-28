namespace BiSharper.Rv.Bank.Models;

public interface IBankEntry
{
    public EntryMime Mime { get; }
    public string Name { get; }
    public uint Length { get; }
    public long Offset { get; }
    public uint Timestamp { get; }
    public uint BufferLength { get; }


}