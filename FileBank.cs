using BiSharper.Rv.Bank.IO.Source;
using BiSharper.Rv.Bank.Models;

namespace BiSharper.Rv.Bank;

public class FileBank(AbstractFileBankSource source)
{
    public virtual string DefaultPrefix => source.BankName;
    
    // public string? GetProperty(string name) => source.SourceProperties.GetValueOrDefault(name);
    //
    // public string Prefix => GetProperty("prefix") ?? DefaultPrefix;
    //
    // public bool HasProperty(string name) => source.SourceProperties.ContainsKey(name);
    //
    // public bool HasEntry(string name) =>
    //     source.SourceEntries.ContainsKey(name);
    //
    // public BankEntry? GetEntry(string name) => source.SourceEntries[name];
}