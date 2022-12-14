using System.IO.Compression;
using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class FilesExport : IExport
{
    public readonly List<FileMove> Copy;
    public readonly HashSet<string> Delete;
    public readonly HashSet<string> Wipe;
    public readonly List<FileMove> Extract;
    public void Finalize(string folder)
    {
        Console.WriteLine("Zipping files...");
        using var stream = new FileStream(Path.Combine(folder, "files.zip"), FileMode.OpenOrCreate, FileAccess.Write);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var item in Copy)
        {
            var exp = Environment.ExpandEnvironmentVariables(item.From);
            Console.WriteLine("   " + exp);
            string safe = exp.Replace(':', '_');
            bool ShouldExclude(string path)
            {
                foreach (var ex in item.Exclude)
                {
                    if (path == Path.Combine(exp, ex))
                        return true;
                }
                return false;
            }
            Redoable.Do(() =>
            {
                if (Directory.Exists(exp))
                    zip.CreateEntryFromDirectory(exp, safe, predicate: x => !ShouldExclude(x));
                else
                    zip.CreateEntryFromFile(exp, safe);
            });
            Extract.Add(new FileMove(safe.Replace('\\', '/'), item.To, item.Method));
        }
        Copy.Clear();
    }
    public void Perform(string folder)
    {
        foreach (var item in Delete)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            Redoable.Do(() =>
            {
                if (Directory.Exists(exp))
                    Directory.Delete(exp, true);
                else if (File.Exists(exp))
                    File.Delete(exp);
            });
        }
        foreach (var item in Wipe)
        {
            var exp = Environment.ExpandEnvironmentVariables(item);
            Redoable.Do(() =>
            {
                if (Directory.Exists(exp))
                    IOUtils.WipeDirectory(exp);
            });
        }
        Console.WriteLine("Extracting zip...");
        using var stream = File.OpenRead(Path.Combine(folder, "files.zip"));
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var item in Extract)
        {
            var dest = Environment.ExpandEnvironmentVariables(item.To);
            Console.WriteLine("   " + dest);
            var entry = zip.GetEntry(item.From.Replace('/', '\\'));
            Redoable.Do(() =>
            {
                if (item.Method == FolderWrite.Replace && Directory.Exists(dest))
                    IOUtils.WipeDirectory(dest);
                if (entry != null)
                    entry.ExtractToFile(dest, true);
                else
                    zip.ExtractDirectoryEntry(item.From, dest, true);
            });
        }
    }
}

public enum FolderWrite
{
    Replace,
    Merge
}

public class FileMove
{
    public readonly string From;
    public readonly string To;
    public readonly FolderWrite Method = FolderWrite.Replace;
    public readonly string[] Exclude = Array.Empty<string>();
    public FileMove(string from, string to, FolderWrite method)
    {
        From = from;
        To = to;
        Method = method;
    }
    [YamlParser.Parser]
    private FileMove(YamlNode node)
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
            if (method != null)
                this.Method = YamlParser.Parse<FolderWrite>(method);
            var exclude = map.TryGet("exclude");
            if (exclude != null)
                this.Exclude = YamlParser.Parse<string[]>(exclude);
        }
    }
}
