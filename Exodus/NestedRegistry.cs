using Microsoft.Win32;
using System.Collections.ObjectModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

public class NestedRegistry : IYamlConvertible
{
    public readonly RegistryKey Key;
    public readonly Lazy<NestedRegistry?> Parent;
    public readonly string Name;
    public readonly string Value;
    public readonly Lazy<ReadOnlyDictionary<string, NestedRegistry>> Subkeys;
    public readonly Lazy<ReadOnlyDictionary<string, string>> Values;
    public NestedRegistry[] Path
    {
        get
        {
            var list = new List<NestedRegistry>();
            var item = this;
            while (item != null)
            {
                list.Add(item);
                item = item.Parent.Value;
            }
            list.Reverse();
            return list.ToArray();
        }
    }
    [YamlIgnore]
    public string StringPath => String.Join('\\', Path.Select(x => x.Name));
    public NestedRegistry(RegistryKey key) : this(key, null) { }
    private NestedRegistry(RegistryKey key, NestedRegistry? parent)
    {
        Key = key;
        Name = System.IO.Path.GetFileName(key.Name);
        Parent = new(() => parent);
        Subkeys = new(ScanSubkeys);
        Values = new(ScanValues);
    }

    private ReadOnlyDictionary<string, NestedRegistry> ScanSubkeys()
    {
        var list = new Dictionary<string, NestedRegistry>();
        foreach (var name in Key.GetSubKeyNames())
        {
            list.Add(name, new NestedRegistry(Key.OpenSubKey(name), this));
        }
        return new(list);
    }

    private ReadOnlyDictionary<string, string> ScanValues()
    {
        var list = new Dictionary<string, string>();
        foreach (var name in Key.GetValueNames())
        {
            list.Add(name, Key.GetValue(name).ToString());
        }
        return new(list);
    }

    public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    {
        throw new NotImplementedException();
    }

    public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    {
        emitter.Emit(new MappingStart());
        if (Value != null)
        {
            emitter.Emit(new Scalar("(Default)"));
            emitter.Emit(new Scalar(Value));
        }
        if (Values.Value.Count > 0)
        {
            foreach (var (name, value) in Values.Value.OrderBy(x => x.Key))
            {
                emitter.Emit(new Scalar(name));
                emitter.Emit(new Scalar(value));
            }
        }
        if (Subkeys.Value.Count > 0)
        {
            foreach (var (name, obj) in Subkeys.Value.OrderBy(x => x.Key))
            {
                emitter.Emit(new Scalar(name));
                nestedObjectSerializer(obj);
            }
        }
        emitter.Emit(new MappingEnd());
    }
}
