using Exodus;
using TryashtarUtils.Utility;

var node = YamlHelper.ParseFile("config.yaml");
var settings = YamlParser.Parse<ExportSettings>(node);
var again = YamlParser.Serialize<ExportSettings>(settings);
settings.CreateExport("config_out");
YamlHelper.SaveToFile(again, "config_round.yaml");
Console.WriteLine();
