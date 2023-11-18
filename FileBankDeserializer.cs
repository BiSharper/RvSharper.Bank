using System.Text;
using System.Text.RegularExpressions;
using BiSharper.Rv.Bank.Models;

namespace BiSharper.Rv.Bank;

public partial class FileBank
{
    public const char PboDirSeparator = '\\';
    
    public FileBank(
        Stream input,
        string defaultPrefix,
        bool calculateOffsets = true,
        bool readPropertiesOnInnerVersion = false,
        bool breakOnInnerVersion = true,
        bool ignoreImpossibleOffsets = true,
        bool headerOffsetIsImpossible = true
    )
    {
        DefaultPrefix = defaultPrefix;
        _input = input;
        _binaryLength = _input.Length;
        
        var entries = new Dictionary<string, BankEntry>();
        var offset = (int)input.Position;
        var first = true;
        
        for (;;)
        {
            var (entryName, entryMeta) = ReadEntry(input, this);
            
            if (calculateOffsets)
            {
                entryMeta.Offset = (long) (ulong) offset;
                offset += (int)entryMeta.BufferLength;
            }

            if (entryName.Length > 0)
            {
                NormalizePath(ref entryName);
                entries[entryName] = entryMeta;
            }
            else if(entryMeta is { Mime: EntryMime.Version, BufferLength: 0, Timestamp: 0 })
            {
                if (first || readPropertiesOnInnerVersion) ReadVersionProperties(input, _properties);
                if(breakOnInnerVersion && !first) break;
            }
            else
            {
                break;
            }
            
            first = false;
        }
        var headerEnd = (uint)input.Position;

        foreach (var (name, meta) in entries)
        {
            var correctedMeta = calculateOffsets ? meta with { Offset = meta.Offset + headerEnd } : meta;
            if (!(ignoreImpossibleOffsets && ((correctedMeta.Offset < headerEnd && headerOffsetIsImpossible) ||
                                              correctedMeta.Offset > _binaryLength|| 
                                              correctedMeta.Offset + correctedMeta.BufferLength >= _binaryLength)))
            {
                _dataEntries[name] = correctedMeta;
            }
        }   
    }

    private static void ReadVersionProperties(Stream input, IDictionary<string, string> properties)
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

    private static void NormalizePrefix(ref string prefix)
    {
        NormalizePath(ref prefix);
        prefix += PboDirSeparator;
    }

    public static void NormalizePath(ref string path)
    {
        path = PathNormalizationRegex().Replace(path, @"\").ToLower().TrimStart(PboDirSeparator).TrimEnd(PboDirSeparator);
    }

    private static (string, BankEntry) ReadEntry(Stream input, FileBank owner)
    {
        var entryName = ReadEntryName(input);
        return (
            entryName,
            new BankEntry
            {
                Name = entryName,
                Mime = (EntryMime)TakeInt(input),
                Length = (uint)TakeInt(input),
                Offset = (long)(ulong)TakeInt(input),
                Timestamp = (uint)TakeInt(input),
                BufferLength = (uint)TakeInt(input),
                Owner = owner
            }
        );
    }
    

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