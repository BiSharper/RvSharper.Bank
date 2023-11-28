using System.Text;
using System.Text.RegularExpressions;
using BiSharper.Rv.Bank.Models;

namespace BiSharper.Rv.Bank.IO;

public partial class FileBankReader(
    RvEngineType engine,
    bool readInnerVersionProperties = false,
    bool innerVersionSignifiesEnd = true,
    bool ignoreImpossibleOffsets = true,
    bool headerOffsetsAreImpossible = true
)
{
    public static readonly FileBankReader DefaultDayZ = new FileBankReader(RvEngineType.OldEnfusion);
    public const char PboDirSeparator = '\\';
    public readonly RvEngineType Engine = engine;
    public bool CalculateOffsets => Engine >= RvEngineType.RealVirtuality3;
    public readonly bool ReadInnerVersionProperties = readInnerVersionProperties;
    public readonly bool InnerVersionSignifiesEnd = innerVersionSignifiesEnd;
    public readonly bool IgnoreImpossibleOffsets = ignoreImpossibleOffsets;
    public readonly bool HeaderOffsetsAreImpossible = headerOffsetsAreImpossible;


    public struct BankEntryDto
    {
        public EntryMime Mime { get; set; }
        public string Name { get; set; }
        public uint Length { get; set; }
        public long Offset { get; set; }
        public uint Timestamp { get; set; }
        public uint BufferLength { get; set; }

        public static implicit operator BankEntry(BankEntryDto d) => new BankEntry(d.Mime, d.Name, d.Length, d.Offset, d.Timestamp, d.BufferLength);

        public static explicit operator BankEntryDto(BankEntry b) => new BankEntryDto()
        {
            Mime = b.Mime,
            Name = b.Name,
            Length = b.Length,
            Offset = b.Offset,
            Timestamp = b.Timestamp,
            BufferLength = b.BufferLength
        };
    }

    public virtual void ReadEntries(Stream stream, IDictionary<string, BankEntry> entries, IDictionary<string, string> properties)
    {
        var offset = (int)stream.Position;
        var first = true;
        for (;;)
        {
            var entryMeta = ReadEntry(stream);

            if (CalculateOffsets)
            {
                entryMeta.Offset = (long) (ulong) offset;
                offset += (int)entryMeta.BufferLength;
            }

            if (entryMeta.Name.Length > 0)
            {
                entryMeta.Name = NormalizePath(entryMeta.Name);
                entries[entryMeta.Name] = entryMeta;
            }
            else if(entryMeta is { Mime: EntryMime.Version, BufferLength: 0, Timestamp: 0 })
            {
                if (first || ReadInnerVersionProperties) ReadVersionProperties(stream, properties);
                if(InnerVersionSignifiesEnd && !first) break;
            }
            else
            {
                break;
            }

            first = false;
        }
        var headerEnd = (uint)stream.Position;

        foreach (var (name, meta) in entries)
        {
            var correctedMeta = CalculateOffsets ? meta with { Offset = meta.Offset + headerEnd } : meta;
            if (!(IgnoreImpossibleOffsets && ((correctedMeta.Offset < headerEnd && HeaderOffsetsAreImpossible) ||
                                              correctedMeta.Offset > stream.Length||
                                              correctedMeta.Offset + correctedMeta.BufferLength >= stream.Length)))
            {
                entries[name] = correctedMeta;
            }
        }

    }

    public static void ReadVersionProperties(Stream input, IDictionary<string, string> properties)
    {
        for (;;)
        {
            var name = ReadEntryName(input);
            if(name.Length == 0) break;
            var value = ReadEntryName(input);

            if (name.Equals("prefix", StringComparison.OrdinalIgnoreCase))
            {
                NormalizePrefix(ref value);
            }
            properties.Add(name, value);
        }
    }

    public static void NormalizePrefix(ref string prefix)
    {
        prefix = NormalizePath(prefix);
        prefix += PboDirSeparator;
    }

    public static string NormalizePath(string path)
    {
        return PathNormalizationRegex().Replace(path, @"\").ToLower().TrimStart(PboDirSeparator).TrimEnd(PboDirSeparator);
    }

    private static BankEntryDto ReadEntry(Stream input) => new BankEntryDto
    {
        Name = ReadEntryName(input),
        Mime = (EntryMime)TakeInt(input),
        Length = (uint)TakeInt(input),
        Offset = (long)(ulong)TakeInt(input),
        Timestamp = (uint)TakeInt(input),
        BufferLength = (uint)TakeInt(input)
    };


    private const int IntBufferSize = sizeof(int);
    private static int TakeInt(Stream input)
    {

        Span<byte> buffer = stackalloc byte[IntBufferSize];
        var foundBytes = input.Read(buffer);
        if (foundBytes != IntBufferSize)
        {
            throw new IOException($"Expected enough room for {IntBufferSize} bytes at position {input.Position} but could only read {foundBytes}.");
        }

        return BitConverter.ToInt32(buffer);
    }


    private static readonly byte[] NameBuffer = new byte[1024];
    private static string ReadEntryName(Stream input)
    {

        int i;
        for (i = 0; i < NameBuffer.Length; i++)
        {
            var current = input.ReadByte();
            if(current <= 0) break;

            NameBuffer[i] = (byte)current;
        }

        return Encoding.UTF8.GetString(NameBuffer[..i]);
    }

    [GeneratedRegex(@"[\\/]+")]
    private static partial Regex PathNormalizationRegex();

}