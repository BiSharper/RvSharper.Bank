using BiSharper.Rv.Bank.IO.Source;

namespace BiSharper.Rv.Bank.Models;

public readonly struct BankEntry(EntryMime mime, string name, uint length, long offset, uint timestamp, uint bufferLength): IEquatable<BankEntry>, IBankEntry
{
    public EntryMime Mime { get; init; } = mime;
    public string Name { get; init; } = name;
    public uint Length { get; init; } = length;
    public long Offset { get; init; } = offset;
    public uint Timestamp { get; init; } = timestamp;
    public uint BufferLength { get; init; } = bufferLength;

    public bool Equals(BankEntry other) => Mime == other.Mime && Name == other.Name && Length == other.Length && Offset == other.Offset && Timestamp == other.Timestamp && BufferLength == other.BufferLength;
    public override bool Equals(object? obj) => obj is BankEntry other && Equals(other);
    public static bool operator ==(BankEntry left, BankEntry right) => left.Equals(right);
    public static bool operator !=(BankEntry left, BankEntry right) => !(left == right);
    public override int GetHashCode() =>  HashCode.Combine((int)Mime, Name, Length, Offset, Timestamp, BufferLength);

}