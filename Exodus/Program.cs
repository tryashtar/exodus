using Exodus;
using Microsoft.Win32;

var key = new NestedRegistry(Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"));
var serializer = new YamlDotNet.Serialization.Serializer();
serializer.Serialize(Console.Out, key);
