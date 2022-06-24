using Microsoft.Win32;
using YamlDotNet.RepresentationModel;

namespace Exodus;

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
    public RegistryKey? GetAsKey()
    {
        return RelevantKey(false)?.OpenSubKey(Key, false);
    }

    public void Delete()
    {
        var key = RelevantKey(true);
        if (key != null)
        {
            key.DeleteValue(Key, false);
            key.DeleteSubKey(Key, false);
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
        var key = RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(KeyPath, writable);
        return key;
    }

    private object? GetValue()
    {
        return RelevantKey(false)?.GetValue(Key, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
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
