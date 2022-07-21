using YamlDotNet.RepresentationModel;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class CommandsExport
{
    public readonly List<CommandPair> Export;
    public readonly List<CommandArgs> Import;
    public void Finalize(string folder)
    {
        Console.WriteLine("Running commands...");
        for (int i = 0; i < Export.Count; i++)
        {
            string file = i + ".txt";
            var (name, args) = (Export[i].Export.Name, Export[i].Export.Args);
            (args, TextWriter stream) = args.Contains("@@@") ? (args.Replace("@@@", file), Console.Out) : (args, File.CreateText(Path.Combine(folder, file)));
            var result = new ProcessWrapper(folder, name, args, stream, Console.Out).Result;
            if (result.ExitCode != 0)
                Console.WriteLine($"    Error {result.ExitCode}");
            stream?.Flush();
            Import.Add(new CommandArgs(Export[i].Import.Name, Export[i].Import.Args.Replace("@@@", file)));
        }
        Export.Clear();
    }
    public void Perform()
    {
        Console.WriteLine("Running commands...");
        foreach (var import in Import)
        {
            var result = new ProcessWrapper(Directory.GetCurrentDirectory(), import.Name, import.Args, Console.Out, Console.Out).Result;
            if (result.ExitCode != 0)
                Console.WriteLine($"    Error {result.ExitCode}");
        }
    }
}

public record CommandPair(CommandArgs Export, CommandArgs Import)
{
    [YamlParser.Parser]
    public CommandPair(CommandArgs[] list) : this(default, default)
    {
        this.Export = list[0];
        this.Import = list[1];
    }
}

public class CommandArgs
{
    public readonly string Name;
    public readonly string Args;
    [YamlParser.Parser]
    private CommandArgs(YamlNode node)
    {
        if (node is YamlScalarNode simple)
        {
            int index = simple.Value.IndexOf(' ');
            Name = simple.Value[..index];
            Args = simple.Value[(index + 1)..];
        }
        else
        {
            var map = (YamlMappingNode)node;
            this.Name = YamlParser.Parse<string>(map["name"]);
            this.Args = YamlParser.Parse<string>(map["args"]);
        }
    }
    public CommandArgs(string name, string args)
    {
        Name = name;
        Args = args;
    }
}