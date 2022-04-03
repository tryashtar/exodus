using Exodus;
using Microsoft.Win32;
using TryashtarUtils.Utility;

var serializer = new YamlDotNet.Serialization.Serializer();
var deserializer = new YamlDotNet.Serialization.Deserializer();
var key = new NestedRegistry(Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"));
var config = deserializer.Deserialize<ExodusConfig>(File.OpenText("config.yaml"));
serializer.Serialize(Console.Out, key);
