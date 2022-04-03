using Microsoft.Win32;
using System.Collections.ObjectModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

[YamlHelper.OptionalFields]
public class ExportSettings
{
    public readonly RegistryExport Registry;
}

[YamlHelper.OptionalFields]
public class RegistryExport
{
    public readonly RegistryAssignments Set;
    public readonly RegistryCopies Copy;
    public readonly RegistryDeletions Delete;
}

public class RegistryAssignments
{
    [YamlHelper.Root]
    private readonly Dictionary<RegistryPath, RegistryValue> Assignments;
}

public class RegistryCopies
{
    [YamlHelper.Root]
    private readonly Dictionary<RegistryPath, RegistryPath> Copies;
}

public class RegistryDeletions
{
    [YamlHelper.Root]
    private readonly HashSet<RegistryPath> Delete;
}

public class RegistryPath
{
    [YamlHelper.Root]
    private string Path;
}

public class RegistryValue
{
    [YamlHelper.Root]
    private string Value;
}