using Microsoft.Win32;
using System.Collections.ObjectModel;
using TryashtarUtils.Utility;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

[YamlParser.OptionalFields]
public class ExportSettings
{
    public readonly RegistryExport Registry;
    public readonly WingetExport Winget;
    public readonly FilesExport Files;
    public void CreateExport(string folder)
    {

    }
}

[YamlParser.OptionalFields]
public class RegistryExport
{
    public readonly Dictionary<RegistryPath, RegistryValue> Set;
    public readonly HashSet<RegistryPath> Copy;
    public readonly HashSet<RegistryPath> Delete;
}

[YamlParser.OptionalFields]
public class WingetExport
{
    public readonly HashSet<string> Install;
    public readonly HashSet<string> Uninstall;
}

public class FilesExport
{
    public readonly HashSet<string> Copy;
    public readonly HashSet<string> Delete;
}

public class RegistryValue
{
    [YamlParser.Root]
    private readonly string Value;
}

public class RegistryPath
{
    private readonly RegistryHive TopLevel;
    private readonly string PathRemainder;
    private static readonly char[] Slashes = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
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
        PathRemainder = val[(index + 1)..];
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
        return start + Slashes[0] + PathRemainder;
    }

    public override int GetHashCode()
    {
        return (TopLevel, PathRemainder).GetHashCode();
    }
}