using System.Text.Json;

namespace Exodus;

public static class WingetWrapper
{
    public static bool IsInstalled(string name)
    {
        var cmd = ProcessWrapper.RunCommand("winget", $"list --exact \"{name}\"");
        return cmd.ExitCode == 0;
    }

    public static IEnumerable<string> InstalledPackages()
    {
        var file = Path.GetTempFileName();
        var cmd = ProcessWrapper.RunCommand("winget", $"export --output \"{file}\" --source winget");
        if (cmd.ExitCode != 0)
            throw new Exception(cmd.Error);
        var json = JsonDocument.Parse(File.OpenRead(file)).RootElement;
        foreach (var source in json.GetProperty("Sources").EnumerateArray())
        {
            foreach (var package in source.GetProperty("Packages").EnumerateArray())
            {
                yield return package.GetProperty("PackageIdentifier").GetString();
            }
        }
    }

    public static void Install(string name)
    {
        var result = new ProcessWrapper(Directory.GetCurrentDirectory(), "winget", $"install \"{name}\"").Result;
    }

    public static void Uninstall(string name)
    {
        var result = new ProcessWrapper(Directory.GetCurrentDirectory(), "winget", $"uninstall \"{name}\"").Result;
    }
}
