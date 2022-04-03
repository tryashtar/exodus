using Exodus;
using TryashtarUtils.Utility;

var node = YamlHelper.ParseFile("config.yaml");
var settings = YamlParser.Parse<ExportSettings>(node);
Console.WriteLine();
