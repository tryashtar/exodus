using Microsoft.Win32;
using System.Collections.ObjectModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

[YamlParser.OptionalFields]
public class ExportSettings
{
    public readonly RegistryExport Registry;
}

[YamlParser.OptionalFields]
public class RegistryExport
{
    public readonly RegistryAssignments Set;
    public readonly RegistryCopies Copy;
    public readonly RegistryDeletions Delete;
}

public class RegistryAssignments
{
    [YamlParser.Root]
    private readonly Dictionary<RegistryPath, RegistryValue> Assignments;
}

public class RegistryCopies
{
    [YamlParser.Root]
    private readonly HashSet<RegistryPath> Copies;
}

public class RegistryDeletions
{
    [YamlParser.Root]
    private readonly HashSet<RegistryPath> Delete;
}

public class RegistryValue
{
    [YamlParser.Root]
    private string Value;
}

public class RegistryPath
{
    private RegistryHive TopLevel;
    private string PathRemainder;
    private static readonly char[] Slashes = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
    [YamlParser.Parser]
    private void Parse(string val)
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
}