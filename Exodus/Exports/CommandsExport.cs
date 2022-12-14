using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class CommandsExport : IExport
{
    public readonly List<CommandPair> Export;
    public readonly List<ImportCommand> Import;
    public void Finalize(string folder)
    {
        Console.WriteLine("Running commands...");
        foreach (var pair in Export)
        {
            string file = IOUtils.MakeFilesafe((pair.Export.Name + " " + pair.Export.Args).GetHashCode() + ".txt");
            var (name, args) = (pair.Export.Name, pair.Export.Args);
            (args, TextWriter stream) = args.Contains("@@@") ? (args.Replace("@@@", Path.GetFullPath(Path.Combine(folder, file))), Console.Out) : (args, File.CreateText(Path.Combine(folder, file)));
            Console.WriteLine(name + " " + args);
            Redoable.Do(() =>
            {
                var result = new ProcessWrapper(folder, name, args, stream, Console.Out).Result;
                if (!pair.Export.FailOk && result.ExitCode != 0)
                    throw new ApplicationException(result.Error);
            });
            stream?.Flush();
            Import.Add(new ImportCommand(pair.Import, file));
        }
        Export.Clear();
    }
    public void Perform(string folder)
    {
        Console.WriteLine("Running commands...");
        foreach (var import in Import)
        {
            string args = import.Command.Args.Replace("@@@", Path.GetFullPath(Path.Combine(folder, import.File)));
            Console.WriteLine(import.Command.Name + " " + args);
            Redoable.Do(() =>
            {
                var result = new ProcessWrapper(folder, import.Command.Name, args, Console.Out, Console.Out).Result;
                if (!import.Command.FailOk && result.ExitCode != 0)
                    throw new ApplicationException(result.Error);
            });
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

public record ImportCommand(CommandArgs Command, string File)
{
    [YamlParser.Parser]
    public ImportCommand(YamlNode node) : this(default, default)
    {
        this.File = "";
        if (node is YamlMappingNode map && map.Children.TryGetValue("command", out var c))
        {
            this.Command = new CommandArgs(c);
            if (map.Children.TryGetValue("file", out var f))
                this.File = f.String();
        }
        else
            this.Command = new CommandArgs(node);
    }
}

public class CommandArgs
{
    public readonly string Name;
    public readonly string Args;
    public readonly bool FailOk = false;
    [YamlParser.Parser]
    public CommandArgs(YamlNode node)
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
            this.Name = Environment.ExpandEnvironmentVariables(YamlParser.Parse<string>(map["name"]));
            this.Args = YamlParser.Parse<string>(map["args"]);
            if (map.Children.TryGetValue("fail_ok", out var fail_ok))
                this.FailOk = YamlParser.Parse<bool>(fail_ok);
        }
    }
    public CommandArgs(string name, string args)
    {
        Name = name;
        Args = args;
    }
}