using Microsoft.Win32;
using System.Collections.ObjectModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

public class ExportSettings
{
    public readonly Registry Registry;
}

public class Registry
{
    public readonly RegistryAssignments Set;
    public readonly RegistryCopies Copy;
    public readonly RegistryDeletions Delete;
}

public class RegistryAssignments
{
    private readonly Dictionary<RegistryPath, RegistryValue> Assignments;
}

public class RegistryPath
{

}

public class RegistryValue
{

}