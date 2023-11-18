namespace BiSharper.Rv.Bank.Models;

public enum EntryMime
{
    Decompressed = 0,
    Compressed = 0x43707273,
    Encrypted = 0x456e6372,
    Version = 0x56657273
}