
namespace BiSharper.Rv.Bank.IO.Source;

public class FileBankSource : AbstractFileBankSource
{
    private readonly FileInfo _source;
    public override string BankName => Path.GetFileNameWithoutExtension(_source.FullName);
    public override bool Writable { get; }
    public override long BankLength => _source.Length;

    public FileBankSource(FileInfo source, bool writable, FileBankReader reader) : base(reader)
    {
        _source = source;
        Writable = writable;
        PopulateFromSource();
    }

    protected sealed override void PopulateFromSource()
    {
        SourceEntries.Clear();
        using var stream = _source.OpenRead();
        Reader.ReadEntries(stream, SourceEntries, SourceProperties);
    }

    protected override Task<Stream> CreateStreamAsync() => Task.FromResult<Stream>(new FileStream(_source.FullName,
        FileMode.Open, FileAccess.Read, FileShare.Read,
        4096, true));
}