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
            if (item.Method == FolderWrite.Replace && Directory.Exists(dest))
                IOUtils.WipeDirectory(dest);
            var entry = zip.GetEntry(item.From);
            if (entry != null)
                entry.ExtractToFile(dest, true);
            else
                zip.ExtractDirectoryEntry(item.From, dest, true);
        }
    }
}

public enum FolderWrite
{
    Replace,
    Merge
}
