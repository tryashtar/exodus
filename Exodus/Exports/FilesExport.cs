using System.IO.Compression;
using TryashtarUtils.Utility;

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
            zip.CreateEntryFromAny(exp, exp);
            Extract.Add(new FileMove(exp.Replace('\\', '/'), item.To, item.Method));
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
            if (item.Method == FolderWrite.Replace && Directory.Exists(dest))
                IOUtils.WipeDirectory(dest);
            zip.ExtractDirectoryEntry(item.From, dest, true);
        }
    }
}

public enum FolderWrite
{
    Replace,
    Merge
}
