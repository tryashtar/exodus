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
        Credentials.Finalize();
        Installs.Finalize();
        Registry.Finalize();
        Files.Finalize(Path.Combine(folder, "files.zip"));
        Commands.Finalize(folder);
        Console.WriteLine("Writing config...");
        YamlHelper.SaveToFile(YamlParser.Serialize(this), Path.Combine(folder, "import.yaml"));
    }
    public void PerformImport()
    {
        Credentials.Perform();
        Installs.Perform();
        Registry.Perform();
        Files.Perform("files.zip");
        Commands.Perform();
    }
}

