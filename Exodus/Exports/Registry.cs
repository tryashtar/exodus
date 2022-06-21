using Microsoft.Win32;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class RegistryExport
{
    public readonly Dictionary<RegistryPath, RegistryValue> Set;
    public readonly HashSet<RegistryPath> Copy;
    public readonly HashSet<RegistryPath> Delete;
    public readonly List<FileAssociation> Associations;
    public void Finalize()
    {
        Console.WriteLine("Finalizing registry...");
        foreach (var item in Copy)
        {
            FinalizeSingle(item);
        }
        Copy.Clear();
    }
    private void FinalizeSingle(RegistryPath item)
    {
        var val = item.GetAsValue();
        var key = item.GetAsKey();
        if (val != null)
            Set[item] = val;
        if (key != null)
        {
            foreach (var sub in key.GetValueNames())
            {
                FinalizeSingle(item.Append(sub));
            }
            foreach (var sub in key.GetSubKeyNames())
            {
                FinalizeSingle(item.Append(sub));
            }
        }
        if (val == null && key == null)
            Delete.Add(item);
    }
    public void Perform()
    {
        Console.WriteLine("Importing registry...");
        foreach (var assoc in Associations)
        {
            // we can't do this in finalize phase because the hash part needs to run on the destination machine
            new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}", null).SetValue(new RegistryValue(assoc.Desc));
            new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}", "FriendlyTypeName").SetValue(new RegistryValue(assoc.Desc));
            new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}\shell\open\command", null).SetValue(new RegistryValue(assoc.Path));
            foreach (var ext in assoc.Extensions)
            {
                string ext_fix = ext.StartsWith('.') ? ext : '.' + ext;
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}", null).SetValue(new RegistryValue(assoc.Id));
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}", "OpenWithProgids").Delete();
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext_fix}\UserChoice", "ProgId").SetValue(new RegistryValue(assoc.Id));
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext_fix}\UserChoice", "Hash").SetValue(new RegistryValue(StupidFileExtensionRegistryHack.GetHash(assoc.Id, ext_fix)));
            }
        }
        foreach (var item in Delete)
        {
            item.Delete();
        }
        foreach (var item in Set)
        {
            item.Key.SetValue(item.Value);
        }
    }
}

public class FileAssociation
{
    public readonly string Id;
    public readonly string Desc;
    public readonly string Path;
    public readonly List<string> Extensions;
}
