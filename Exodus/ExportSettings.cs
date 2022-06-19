using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO.Compression;
using TryashtarUtils.Utility;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport()
    {
        Registry.Perform();
        Winget.Perform();
        Files.Perform("files.zip");
    }
}

[YamlParser.OptionalFields(true)]
public class RegistryExport
{
    public readonly Dictionary<RegistryPath, RegistryValue> Set;
    public readonly HashSet<RegistryPath> Copy;
    public readonly HashSet<RegistryPath> Delete;
    public void Finalize()
    {
        Console.WriteLine("Finalizing registry...");
        foreach (var item in Copy)
        {
            if (item.Exists())
                Set[item] = item.Get();
            else
                Delete.Add(item);
        }
        Copy.Clear();
    }
    public void Perform()
    {

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
        foreach (var item in Install)
        {
            WingetWrapper.Install(item);
        }
        foreach (var item in Uninstall)
        {
            WingetWrapper.Uninstall(item);
        }
    }
}

[YamlParser.OptionalFields(true)]
public class FilesExport
{
    public readonly HashSet<string> Copy;
    public readonly Dictionary<string, string> Move;
    public readonly HashSet<string> Delete;
    public readonly Dictionary<string, string> Extract;
    public void Finalize(string zip_path)
    {
        Console.WriteLine("Zipping files...");
        using var stream = File.OpenWrite(zip_path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var item in Copy)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            Console.WriteLine("   " + exp);
            zip.CreateEntryFromAny(exp, exp);
            Extract.Add(exp, item);
        }
        Copy.Clear();
        foreach (var item in Move)
        {
            var (exp_from, exp_to) = (Environment.ExpandEnvironmentVariables(item.Key), Environment.ExpandEnvironmentVariables(item.Value));
            Console.WriteLine("   " + exp_from);
            zip.CreateEntryFromAny(exp_from, exp_to);
            Extract.Add(exp_to, item.Value);
        }
        Move.Clear();
    }
    public void Perform(string zip_path)
    {
        foreach (var item in Delete)
        {
            if (Directory.Exists(item))
                Directory.Delete(item, true);
            else if (File.Exists(item))
                File.Delete(item);
        }
    }
}

public class RegistryValue
{
    [YamlParser.Root]
    private readonly string Value;
    [YamlParser.Parser]
    public RegistryValue(string val)
    {
        Value = val;
    }
}

public class RegistryPath
{
    private readonly RegistryHive TopLevel;
    private readonly string KeyPath;
    private readonly string Key;
    private static readonly char[] Slashes = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    public RegistryValue? Get()
    {
        var val = GetValue();
        if (val == null)
            return null;
        return new RegistryValue(val.ToString());
    }

    private object? GetValue()
    {
        var key = RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(KeyPath);
        return key?.GetValue(Key);
    }

    public bool Exists()
    {
        return GetValue() != null;
    }

    public void Set(RegistryValue value)
    {

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