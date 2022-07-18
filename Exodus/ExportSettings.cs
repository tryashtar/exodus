using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class ExportSettings
{
    public readonly RegistryExport Registry;
    public readonly WingetExport Winget;
    public readonly FilesExport Files;
    public readonly CredentialsExport Credentials;
    public void CreateExport(string folder)
    {
        Credentials.Finalize();
        Winget.Finalize();
        Registry.Finalize();
        Files.Finalize(Path.Combine(folder, "files.zip"));
        Console.WriteLine("Writing config...");
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport()
    {
        Credentials.Perform();
        Winget.Perform();
        Registry.Perform();
        Files.Perform("files.zip");
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
        var packages = WingetWrapper.InstalledPackages().ToHashSet();
        foreach (var item in Install)
        {
            if (!packages.Contains(item))
            {
                Console.WriteLine("    Installing " + item);
                WingetWrapper.Install(item);
            }
            else
                Console.WriteLine($"    Already installed: {item}");
        }
        foreach (var item in Uninstall)
        {
            if (packages.Contains(item))
            {
                Console.WriteLine("    Removing " + item);
                WingetWrapper.Uninstall(item);
            }
            else
                Console.WriteLine($"    Not found: {item}");
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
