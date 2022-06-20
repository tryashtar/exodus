using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Security.AccessControl;
using TryashtarUtils.Utility;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class ExportSettings
{
    public readonly RegistryExport Registry;
    public readonly WingetExport Winget;
    public readonly FilesExport Files;
    public void CreateExport(string folder)
    {
        Registry.Finalize();
        Winget.Finalize();
        Files.Finalize(Path.Combine(folder, "files.zip"));
        Console.WriteLine("Writing config...");
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport()
    {
        Registry.Perform();
        Winget.Perform();
        Files.Perform("files.zip");
    }
}

public class FileAssociation
{
    public readonly string Id;
    public readonly string Desc;
    public readonly string Path;
    public readonly List<string> Extensions;
}

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
        foreach (var assoc in Associations)
        {
            Set.Add(new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}", null), new RegistryValue(assoc.Desc));
            Set.Add(new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}", "FriendlyTypeName"), new RegistryValue(assoc.Desc));
            Set.Add(new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{assoc.Id}\shell\open\command", null), new RegistryValue(assoc.Path));
            foreach (var ext in assoc.Extensions)
            {
                string ext_fix = ext.StartsWith('.') ? ext : '.' + ext;
                Set.Add(new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}", null), new RegistryValue(assoc.Id));
                Delete.Add(new RegistryPath(RegistryHive.CurrentUser, @$"Software\Classes\{ext_fix}", "OpenWithProgids"));
            }
        }
        Associations.Clear();
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
public class WingetExport
{
    public readonly HashSet<string> Install;
    public readonly HashSet<string> Uninstall;
    public readonly HashSet<string> Copy;
    public void Finalize()
    {
        Console.WriteLine("Finalizing winget...");
        if (Copy.Count > 0)
        {
            var packages = WingetWrapper.InstalledPackages().ToHashSet();
            foreach (var item in Copy)
            {
                if (packages.Contains(item))
                    Install.Add(item);
                else
                    Uninstall.Add(item);
            }
            Copy.Clear();
        }
    }
    public void Perform()
    {
        Console.WriteLine("Installing packages...");
        if (Install.Count > 0)
        {
            var packages = WingetWrapper.InstalledPackages().ToHashSet();
            foreach (var item in Install)
            {
                if (!packages.Contains(item))
                    WingetWrapper.Install(item);
            }
        }
        foreach (var item in Uninstall)
        {
            WingetWrapper.Uninstall(item);
        }
    }
}

public record FileMove(string From, string To, FolderWrite Method)
{
    [YamlParser.Parser]
    private FileMove(YamlNode node) : this(default, default, default) // cringe
    {
        if (node is YamlScalarNode simple)
        {
            this.From = simple.Value;
            this.To = simple.Value;
            this.Method = FolderWrite.Replace;
        }
        else
        {
            var map = (YamlMappingNode)node;
            var both = map.TryGet("file");
            if (both != null)
            {
                this.From = YamlParser.Parse<string>(both);
                this.To = this.From;
            }
            else
            {
                this.From = YamlParser.Parse<string>(map["from"]);
                this.To = YamlParser.Parse<string>(map["to"]);
            }
            var method = map.TryGet("method");
            if (method == null)
                this.Method = FolderWrite.Replace;
            else
                this.Method = YamlParser.Parse<FolderWrite>(method);
        }
    }
}

[YamlParser.OptionalFields(true)]
public class FilesExport
{
    public readonly List<FileMove> Copy;
    public readonly HashSet<string> Delete;
    public readonly HashSet<string> Wipe;
    public readonly List<FileMove> Extract;
    public void Finalize(string zip_path)
    {
        Console.WriteLine("Zipping files...");
        using var stream = File.Create(zip_path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var item in Copy)
        {
            var exp = Environment.ExpandEnvironmentVariables(item.From);
            Console.WriteLine("   " + exp);
            zip.CreateEntryFromAny(exp, exp);
            Extract.Add(new FileMove(exp.Replace('\\', '/'), item.To, item.Method));
        }
        Copy.Clear();
    }
    public void Perform(string zip_path)
    {
        foreach (var item in Delete)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            if (Directory.Exists(exp))
                Directory.Delete(exp, true);
            else if (File.Exists(exp))
                File.Delete(exp);
        }
        foreach (var item in Wipe)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            if (Directory.Exists(exp))
                IOUtils.WipeDirectory(exp);
        }
        Console.WriteLine("Extracting zip...");
        using var stream = File.OpenRead(zip_path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var item in Extract)
        {
            var dest = Environment.ExpandEnvironmentVariables(item.To);
            if (item.Method == FolderWrite.Replace && Directory.Exists(dest))
                IOUtils.WipeDirectory(dest);
            zip.ExtractDirectoryEntry(item.From, dest, true);
        }
    }
}

public enum FolderWrite
{
    Replace,
    Merge
}

public class RegistryValue
{
    public readonly object Value;
    public readonly RegistryValueKind Type;
    public RegistryValue(object value, RegistryValueKind type)
    {
        Value = value;
        Type = type;
    }

    public RegistryValue(string value)
    {
        Value = value;
        Type = RegistryValueKind.String;
    }

    [YamlParser.Parser]
    private RegistryValue(YamlMappingNode node)
    {
        Type = YamlParser.Parse<RegistryValueKind>(node["type"]);
        var value = node["value"];
        if (Type == RegistryValueKind.String || Type == RegistryValueKind.ExpandString)
            Value = YamlParser.Parse<string>(value);
        else if (Type == RegistryValueKind.DWord)
            Value = YamlParser.Parse<int>(value);
        else if (Type == RegistryValueKind.QWord)
            Value = YamlParser.Parse<long>(value);
        else if (Type == RegistryValueKind.MultiString)
            Value = YamlParser.Parse<string[]>(value);
        else if (Type == RegistryValueKind.Binary)
            Value = YamlParser.Parse<byte[]>(value);
        else if (Type == RegistryValueKind.None)
            Value = Array.Empty<byte>();
    }
}

public class RegistryPath
{
    private readonly RegistryHive TopLevel;
    private readonly string KeyPath;
    private readonly string Key;
    private static readonly char[] Slashes = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    // an actual value in the registry
    public RegistryValue? GetAsValue()
    {
        var val = GetValue();
        if (val == null)
            return null;
        return new RegistryValue(val, GetValueType().Value);
    }

    // a "folder" that contains other keys/values
    public RegistryKey GetAsKey()
    {
        return RelevantKey(false)?.OpenSubKey(Key, false);
    }

    public void Delete()
    {
        var key = RelevantKey(true);
        if (key != null)
        {
            key.DeleteValue(Key, false);
            key.DeleteSubKeyTree(Key, false);
        }
    }

    public void SetValue(RegistryValue value)
    {
        try
        {
            var key = RelevantKey(true);
            if (key == null)
                key = RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).CreateSubKey(KeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            key.SetValue(Key, value.Value, value.Type);
        }
        catch
        {
            Console.WriteLine($"Failed to set {this}");
            throw;
        }
    }

    private RegistryKey? RelevantKey(bool writable)
    {
        var key = RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(KeyPath, true);
        return key;
    }

    private object? GetValue()
    {
        return RelevantKey(false)?.GetValue(Key);
    }

    private RegistryValueKind? GetValueType()
    {
        return RelevantKey(false)?.GetValueKind(Key);
    }

    public RegistryPath Append(string next)
    {
        return new RegistryPath(this.TopLevel, this.KeyPath + Slashes[0] + this.Key, next);
    }

    public RegistryPath(RegistryHive top, string path, string key)
    {
        TopLevel = top;
        KeyPath = path;
        Key = key;
    }

    [YamlParser.Parser]
    private RegistryPath(string val)
    {
        int index = val.IndexOfAny(Slashes);
        string start = val[..index];
        TopLevel = start switch
        {
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentException($"Couldn't parse registry path {val}")
        };
        int last = val.LastIndexOfAny(Slashes);
        Key = val[(last + 1)..];
        KeyPath = val[(index + 1)..last];
    }

    [YamlParser.Serializer]
    public override string ToString()
    {
        string start = TopLevel switch
        {
            RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
            RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
            RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
            RegistryHive.Users => "HKEY_USERS",
            RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
            _ => throw new ArgumentException($"Couldn't parse registry path {TopLevel}")
        };
        return start + Slashes[0] + KeyPath + Slashes[0] + Key;
    }

    public override int GetHashCode()
    {
        return (TopLevel, KeyPath, Key).GetHashCode();
    }
}