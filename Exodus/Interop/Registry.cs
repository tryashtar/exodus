using Microsoft.Win32;
using System.Security.AccessControl;
using YamlDotNet.RepresentationModel;

namespace Exodus;

public class RegistryPath
{
    private readonly RegistryHive TopLevel;
    private readonly string FolderPath;
    private readonly string? KeyName;
    public bool IsFolder => KeyName == null;
    private static readonly char[] Slashes = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    // an actual value in the registry
    // for keys, is the value of the key
    // for folders, is the value of (Default)
    public RegistryValue? GetAsValue()
    {
        var val = GetValue();
        if (val == null)
            return null;
        return new RegistryValue(val, GetValueType().Value);
    }

    public RegistryKey? GetAsFolder()
    {
        AssertFolderMode(true);
        return RelevantFolder(false);
    }

    public bool Exists()
    {
        if (IsFolder)
            return RelevantFolder(false) != null;
        return GetValue() != null;
    }

    private void AssertFolderMode(bool folder)
    {
        if (IsFolder && !folder)
            throw new InvalidOperationException($"Expected registry key path, got {this}");
        if (!IsFolder && folder)
            throw new InvalidOperationException($"Expected registry folder path, got {this}");
    }

    public void Delete()
    {
        try
        {
            var folder = ParentFolder(true);
            if (folder != null)
            {
                if (IsFolder)
                {
                    string folder_name = FolderPath[(FolderPath.LastIndexOfAny(Slashes) + 1)..];
                    folder.DeleteSubKeyTree(folder_name, false);
                    folder.DeleteSubKey(folder_name, false);
                }
                else
                    folder.DeleteValue(KeyName, false);
            }
        }
        catch
        {
            Console.WriteLine($"Failed to delete {this}");
            throw;
        }
    }

    public void SetValue(RegistryValue value)
    {
        try
        {
            var folder = RelevantFolder(true);
            if (folder == null)
                folder = RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).CreateSubKey(FolderPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            folder.SetValue(KeyName, value.Value, value.Type);
        }
        catch
        {
            Console.WriteLine($"Failed to set {this}");
            throw;
        }
    }

    private void TakePermission(string folder_path)
    {
        var key = RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(folder_path, RegistryRights.ChangePermissions);
        var rs = key.GetAccessControl();
        rs.AddAccessRule(
            new RegistryAccessRule(
                Environment.UserDomainName + "\\" + Environment.UserName,
                RegistryRights.WriteKey
                | RegistryRights.ReadKey
                | RegistryRights.Delete
                | RegistryRights.FullControl,
                AccessControlType.Allow));
    }

    // either this folder, or the folder containing the key
    private RegistryKey? RelevantFolder(bool writable)
    {
        try
        {
            return RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(FolderPath, writable);
        }
        catch
        {
            TakePermission(FolderPath);
            return RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(FolderPath, writable);
        }
    }

    // the folder containing the key or folder
    private RegistryKey? ParentFolder(bool writable)
    {
        string parent_path = FolderPath[..FolderPath.LastIndexOfAny(Slashes)];
        try
        {
            return RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(parent_path, writable);
        }
        catch
        {
            TakePermission(parent_path);
            return RegistryKey.OpenBaseKey(TopLevel, RegistryView.Default).OpenSubKey(parent_path, writable);
        }
    }

    private object? GetValue()
    {
        return RelevantFolder(false)?.GetValue(KeyName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
    }

    private RegistryValueKind? GetValueType()
    {
        return RelevantFolder(false)?.GetValueKind(KeyName);
    }

    public IEnumerable<RegistryPath> SubPaths()
    {
        AssertFolderMode(true);
        var folder = GetAsFolder();
        foreach (var sub in folder.GetValueNames())
        {
            yield return new RegistryPath(this.TopLevel, this.FolderPath, sub);
        }
        foreach (var sub in folder.GetSubKeyNames())
        {
            yield return new RegistryPath(this.TopLevel, this.FolderPath + Slashes[0] + sub, null);
        }
    }

    public RegistryPath(RegistryHive top, string path, string? key)
    {
        TopLevel = top;
        FolderPath = path;
        KeyName = key;
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
        KeyName = val[(last + 1)..];
        FolderPath = val[(index + 1)..last];
        if (KeyName.Length == 0)
            KeyName = null;
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
        return start + Slashes[0] + FolderPath + Slashes[0] + KeyName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TopLevel, FolderPath, KeyName);
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
