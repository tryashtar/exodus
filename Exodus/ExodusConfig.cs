using Microsoft.Win32;
using System.Collections.ObjectModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

public class ExodusConfig : IYamlParseable<ExodusConfig>
{
    public readonly RegistryConfig Registry;
    public static ExodusConfig Parse(IYamlContext context)
    {
        return new(context.Parse<RegistryConfig>("registry"));
    }
    private ExodusConfig(RegistryConfig registry)
    {
        Registry = registry;
    }
}

public interface IYamlParseable<T> where T : IYamlParseable<T>
{
    public static abstract T Parse(IYamlContext context);
}

public interface IYamlContext
{
    public T Parse<T>(string child) where T : IYamlParseable<T>
    {
        return T.Parse()
    }
}

public class RegistryConfig : IYamlParseable<RegistryConfig>
{
    public static RegistryConfig Parse()
    {

    }
}

