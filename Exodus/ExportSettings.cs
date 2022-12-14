using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

public class ExportSettings
{
    public readonly List<IExport> Sequence;
    [YamlParser.Parser]
    private ExportSettings(YamlSequenceNode seq)
    {
        Sequence = new();
        foreach (YamlMappingNode map in seq.Children)
        {
            foreach (YamlScalarNode child in map.Children.Keys)
            {
                var value = map.Children[child];
                if (child.Value == "registry")
                    Sequence.Add(YamlParser.Parse<RegistryExport>(value));
                else if (child.Value == "installs")
                    Sequence.Add(YamlParser.Parse<InstallExport>(value));
                else if (child.Value == "files")
                    Sequence.Add(YamlParser.Parse<FilesExport>(value));
                else if (child.Value == "credentials")
                    Sequence.Add(YamlParser.Parse<CredentialsExport>(value));
                else if (child.Value == "commands")
                    Sequence.Add(YamlParser.Parse<CommandsExport>(value));
                else
                    throw new NotSupportedException(child.Value);
            }
        }
    }
    [YamlParser.Serializer]
    private YamlSequenceNode Serialize()
    {
        var node = new YamlSequenceNode();
        foreach (var item in Sequence)
        {
            if (item is RegistryExport r)
                node.Add(new YamlMappingNode("registry", YamlParser.Serialize(r)));
            else if (item is InstallExport i)
                node.Add(new YamlMappingNode("installs", YamlParser.Serialize(i)));
            else if (item is FilesExport f)
                node.Add(new YamlMappingNode("files", YamlParser.Serialize(f)));
            else if (item is CredentialsExport c)
                node.Add(new YamlMappingNode("credentials", YamlParser.Serialize(c)));
            else if (item is CommandsExport m)
                node.Add(new YamlMappingNode("commands", YamlParser.Serialize(m)));
            else
                throw new NotSupportedException();
        }
        return node;
    }
    public void CreateExport(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var item in Sequence)
        {
            item.Finalize(folder);
        }
        Console.WriteLine("Writing config...");
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport(string folder)
    {
        foreach (var item in Sequence)
        {
            item.Perform(folder);
        }
    }
}

public interface IExport
{
    public void Finalize(string folder);
    public void Perform(string folder);
}
