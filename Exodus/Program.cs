using Exodus;
using TryashtarUtils.Utility;

if (File.Exists("export.yaml"))
{
    var node = YamlHelper.ParseFile("export.yaml");
    var settings = YamlParser.Parse<ExportSettings>(node);
#if DEBUG
    var again = YamlParser.Serialize<ExportSettings>(settings);
    YamlHelper.SaveToFile(again, "export_round.yaml");
#endif
    settings.CreateExport("export");
#if DEBUG
    var extra_parse = YamlParser.Parse<ExportSettings>(YamlHelper.ParseFile(@"export\import.yaml"));
#endif
}
else if (File.Exists("import.yaml"))
{
    var node = YamlHelper.ParseFile("import.yaml");
    var settings = YamlParser.Parse<ExportSettings>(node);
    settings.PerformImport();
}