using System.IO.Compression;
using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class FilesExport
{
    public readonly List<FileMove> Copy;
    public readonly HashSet<string> Delete;
    public readonly HashSet<string> Wipe;
    public readonly List<FileMove> Extract;
    public void Finalize(string zip_path)
    {
        Console.WriteLine("Zipping files...");
        Directory.CreateDirectory(Path.GetDirectoryName(zip_path));
        using var stream = File.Create(zip_path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var item in Copy)
        {
            var exp = Environment.ExpandEnvironmentVariables(item.From);
            Console.WriteLine("   " + exp);
            bool success = false;
            while (!success)
            {
                try
                {
                    zip.CreateEntryFromAny(exp, exp.Replace(':', '_'));
                    Extract.Add(new FileMove(exp.Replace('\\', '/').Replace(':', '_'), item.To, item.Method));
                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("    Error: " + ex.ToString());
                    Console.WriteLine("    Redo?");
                    success = Console.ReadLine().ToLower() == "n";
                }
            }
        }
        Copy.Clear();
    }
    public void Perform(string zip_path)
    {
        foreach (var item in Delete)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            if (Directory.Exists(exp))
                Directory.Delete(exp, true);
            else if (File.Exists(exp))
                File.Delete(exp);
        }
        foreach (var item in Wipe)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            if (Directory.Exists(exp))
                IOUtils.WipeDirectory(exp);
        }
        Console.WriteLine("Extracting zip...");
        using var stream = File.OpenRead(zip_path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var item in Extract)
        {
            var dest = Environment.ExpandEnvironmentVariables(item.To);
            Console.WriteLine("   " + dest);
            var entry = zip.GetEntry(item.From.Replace('/','\\'));
            bool success = false;
            while (!success)
            {
                try
                {
                    if (item.Method == FolderWrite.Replace && Directory.Exists(dest))
                        IOUtils.WipeDirectory(dest);
                    if (entry != null)
                        entry.ExtractToFile(dest, true);
                    else
                        zip.ExtractDirectoryEntry(item.From, dest, true);
                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("    Error: " + ex.ToString());
                    Console.WriteLine("    Redo?");
                    success = Console.ReadLine().ToLower() == "n";
                }
            }
        }
    }
}

public enum FolderWrite
{
    Replace,
    Merge
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
