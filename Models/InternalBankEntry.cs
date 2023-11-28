namespace BiSharper.Rv.Bank.Models;

public struct InternalBankEntry(Stream data, EntryMime mime, string name, uint timestamp, uint bufferLength = 0, long offset = 0): IEquatable<InternalBankEntry>, IBankEntry
{
    public Stream Data { get; } = data;
    public EntryMime Mime { get; init; } = mime;
    public string Name { get; init; } = name;
    public uint Length => (uint)Data.Length;
    public long Offset { get; set; } = offset;
    public uint Timestamp { get; init; } = timestamp;
    public uint BufferLength { get; init; } = bufferLength;

    public bool Equals(InternalBankEntry other) => Mime == other.Mime && Name == other.Name && Length == other.Length && Offset == other.Offset && Timestamp == other.Timestamp && BufferLength == other.BufferLength;
    public override bool Equals(object? obj) => obj is InternalBankEntry other && Equals(other);
    public static bool operator ==(InternalBankEntry left, InternalBankEntry right) => left.Equals(right);
    public static bool operator !=(InternalBankEntry left, InternalBankEntry right) => !(left == right);
    public override int GetHashCode() =>  HashCode.Combine((int)Mime, Name, Length, Offset, Timestamp, BufferLength);

}