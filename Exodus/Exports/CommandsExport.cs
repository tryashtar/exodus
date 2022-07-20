namespace Exodus;

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
            stream?.Flush();
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
