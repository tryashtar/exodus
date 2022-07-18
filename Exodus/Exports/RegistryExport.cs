using Microsoft.Win32;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class RegistryExport
{
    public readonly Dictionary<RegistryPath, RegistryValue> Set;
    public readonly HashSet<RegistryPath> Copy;
    public readonly HashSet<RegistryPath> Delete;
    public readonly FileAssociationExport Associations;
    public void Finalize()
    {
        Console.WriteLine("Finalizing registry...");
        foreach (var item in Copy)
        {
            FinalizeSingle(item);
        }
        Copy.Clear();
        Associations.Finalize();
    }
    private void FinalizeSingle(RegistryPath item)
    {
        var val = item.GetAsValue();
        if (val != null)
            Set[item] = val;
        if (item.IsFolder)
        {
            foreach (var sub in item.SubPaths())
            {
                FinalizeSingle(sub);
            }
        }
        if (!item.Exists())
            Delete.Add(item);
    }
    public void Perform()
    {
        Console.WriteLine("Importing registry...");
        Associations.Perform();
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

[YamlParser.OptionalFields(true)]
public class FileAssociationExport
{
    public readonly List<FileAssociation> Set;
    public readonly List<string> Copy;
    public readonly List<string> Delete;

    private static string FixExtension(string ext)
    {
        return ext.StartsWith('.') ? ext : '.' + ext;
    }

    public void Finalize()
    {
        var assocs = new Dictionary<string, FileAssociation>();
        foreach (var extension in Copy)
        {
            string ext_fix = FixExtension(extension);
            var id = new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext_fix}\UserChoice", "ProgId").GetAsValue();
            if (id == null)
                Delete.Add(ext_fix);
            else
            {
                string idv = (string)id.Value;
                if (assocs.TryGetValue(idv, out var assoc))
                    assoc.Extensions.Add(ext_fix);
                else
                {
                    // CLASSES_ROOT merges system/user associations, so is a more complete view
                    var desc = new RegistryPath(RegistryHive.ClassesRoot, @$"{idv}", "FriendlyTypeName").GetAsValue();
                    if (desc == null)
                        desc = new RegistryPath(RegistryHive.ClassesRoot, @$"{idv}", null).GetAsValue();
                    var path = new RegistryPath(RegistryHive.ClassesRoot, @$"{idv}\shell\open\command", null).GetAsValue();
                    if (path == null)
                        throw new FormatException($"Found file extension handler for {ext_fix} but couldn't find path!");
                    var new_assoc = new FileAssociation() { Id = idv, Desc = (string)desc?.Value, Path = (string)path.Value };
                    new_assoc.Extensions.Add(ext_fix);
                    assocs[idv] = new_assoc;
                }
            }
        }
        Set.AddRange(assocs.Values);
        Copy.Clear();
    }

    public void Perform()
    {
        foreach (var ext in Delete)
        {
            string ext_fix = FixExtension(ext);
            new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes", ext_fix).Delete();
            new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts", ext_fix).Delete();
        }
        foreach (var assoc in Set)
        {
            // we can't do this in finalize phase because the hash part needs to run on the destination machine
            if (assoc.Desc != null)
            {
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}", null).SetValue(new RegistryValue(assoc.Desc));
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}", "FriendlyTypeName").SetValue(new RegistryValue(assoc.Desc));
            }
            new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}\shell\open\command", null).SetValue(new RegistryValue(assoc.Path));
            foreach (var ext in assoc.Extensions)
            {
                string ext_fix = FixExtension(ext);
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}", null).SetValue(new RegistryValue(assoc.Id));
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}\OpenWithProgids", null).Delete();
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}\OpenWithList", null).Delete();
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext_fix}\UserChoice", null).Delete();
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext_fix}\UserChoice", "ProgId").SetValue(new RegistryValue(assoc.Id));
                new RegistryPath(RegistryHive.CurrentUser, @$"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext_fix}\UserChoice", "Hash").SetValue(new RegistryValue(StupidFileExtensionRegistryHack.GetHash(assoc.Id, ext_fix)));
            }
        }
    }
}

public class FileAssociation
{
    public string Id { get; init; }
    public string? Desc { get; init; }
    public string Path { get; init; }
    public readonly List<string> Extensions = new();
}
