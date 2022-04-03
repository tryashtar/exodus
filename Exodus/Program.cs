using Exodus;
using Microsoft.Win32;
using TryashtarUtils.Utility;

var node = TryashtarUtils.Utility.YamlHelper.ParseFile("config.yaml");
var settings = Exodus.YamlHelper.Parse<ExportSettings>(node);
Console.WriteLine();
