using System.IO.Compression;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class InstallExport : IExport
{
    public readonly WingetExport Winget;
    public List<DownloadInstall> Downloads;
    private static readonly HttpClient WebClient = new();
    public void Finalize(string folder)
    {
        Winget.Finalize();
    }

    public void Perform(string folder)
    {
        Winget.Perform();
        if (Downloads.Count > 0)
            Console.WriteLine("Extra installs...");
        foreach (var download in Downloads)
        {
            Console.WriteLine("    " + download.Url);
            string path = Path.GetTempFileName();
            using (var s = WebClient.GetStreamAsync(download.Url).Result)
            {
                using var fs = new FileStream(path, FileMode.Create);
                s.CopyTo(fs);
            }
            if (download.ZipFileName != null)
            {
                string temp_folder = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(path));
                using var zip = ZipFile.OpenRead(path);
                zip.ExtractToDirectory(temp_folder);
                path = Path.Combine(temp_folder, download.ZipFileName);
            }
            var result = ProcessWrapper.RunCommand(path, download.Args);
            if (result.ExitCode != 0)
                Console.WriteLine($"    Error {result.ExitCode}: {result.Error}");
        }
    }
}

[YamlParser.OptionalFields]
public class DownloadInstall
{
    public readonly string Url;
    public readonly string ZipFileName;
    public readonly string Args;
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
        foreach (var item in Uninstall)
        {
            Console.WriteLine("    Uninstalling " + item);
            WingetWrapper.Uninstall(item);
        }
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
    }
}
