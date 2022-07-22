using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class ExportSettings
{
    public readonly RegistryExport Registry;
    public readonly InstallExport Installs;
    public readonly FilesExport Files;
    public readonly CredentialsExport Credentials;
    public readonly CommandsExport Commands;
    public void CreateExport(string folder)
    {
        Directory.CreateDirectory(folder);
        Credentials.Finalize();
        Installs.Finalize();
        Registry.Finalize();
        Commands.Finalize(folder);
        Files.Finalize(Path.Combine(folder, "files.zip"));
        Console.WriteLine("Writing config...");
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport()
    {
        Credentials.Perform();
        Installs.Perform();
        Registry.Perform();
        Commands.Perform();
        Files.Perform("files.zip");
    }
}

