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
    public readonly CommandsExport Commands;
    public void CreateExport(string folder)
    {
        Credentials.Finalize();
        Winget.Finalize();
        Registry.Finalize();
        Files.Finalize(Path.Combine(folder, "files.zip"));
        Commands.Finalize(folder);
        Console.WriteLine("Writing config...");
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport()
    {
        Credentials.Perform();
        Winget.Perform();
        Registry.Perform();
        Files.Perform("files.zip");
        Commands.Perform();
    }
}

[YamlParser.OptionalFields(true)]
public class CommandsExport
{
    public readonly List<ExportCommand> Export;
    public readonly List<string> Import;
    public void Finalize(string folder)
    {
        Console.WriteLine("Running commands...");
        for (int i = 0; i < Export.Count; i++)
        {
            string file = i + ".txt";
            var (name, args) = Split(Export[i].Export);
            (args, TextWriter stream) = args.Contains("@@@") ? (args.Replace("@@@", file), null) : (args, File.CreateText(Path.Combine(folder, file)));
            var result = new ProcessWrapper(folder, name, args, stream, null).Result;
            if (result.ExitCode != 0)
                Console.WriteLine($"    Error {result.ExitCode}: {result.Error}");
            Import.Add(Export[i].Import.Replace("@@@", file));
        }
        Export.Clear();
    }
    public void Perform()
    {
        Console.WriteLine("Running commands...");
        foreach (var import in Import)
        {
            var (name, args) = Split(import);
            var result = ProcessWrapper.RunCommand(name, args);
            if (result.ExitCode != 0)
                Console.WriteLine($"    Error {result.ExitCode}: {result.Error}");
        }
    }
    private (string name, string args) Split(string command)
    {
        int index = command.IndexOf(' ');
        return (command[..index], command[(index + 1)..]);
    }
}

public record ExportCommand(string Export, string Import)
{
    [YamlParser.Parser]
    public ExportCommand(string[] list) : this(default, default)
    {
        this.Export = list[0];
        this.Import = list[1];
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
