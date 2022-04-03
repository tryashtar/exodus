﻿using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using TryashtarUtils.Utility;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Exodus;

public static class YamlParser
{
    public static T Parse<T>(YamlNode node)
    {
        return (T)Parse(node, typeof(T));
    }

    private static object Parse(YamlNode node, Type type)
    {
        if (type == typeof(string))
            return ((YamlScalarNode)node).Value;
        var result = Activator.CreateInstance(type);
        if (result is IDictionary dict)
        {
            var args = type.GetGenericArguments();
            foreach (var (key, value) in (YamlMappingNode)node)
            {
                dict.Add(Parse(key, args[0]), Parse(value, args[1]));
            }
            return dict;
        }
        var collection_type = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collection_type != null)
        {
            dynamic list = result;
            var arg = type.GetGenericArguments()[0];
            foreach (var item in (YamlSequenceNode)node)
            {
                dynamic parsed = Parse(item, arg);
                list.Add(parsed);
            }
            return list;
        }
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var root = FindRootField(fields);
        if (root != null)
        {
            root.SetValue(result, Parse(node, root.FieldType));
            return result;
        }
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var parser = FindParserMethod(methods);
        if (parser != null)
        {
            var param_type = parser.GetParameters()[0].ParameterType;
            parser.Invoke(result, new[] { Parse(node, param_type) });
            return result;
        }
        var map = (YamlMappingNode)node;
        var unused_nodes = map.Children.Keys.ToHashSet();
        foreach (var field in fields.Where(x => x.IsPublic))
        {
            string name = GetNameFromField(field);
            var subnode = map.TryGet(name);
            if (subnode == null)
            {
                if (!IsOptional(type, field))
                    throw new InvalidDataException($"While parsing {type.Name}, field {name} was missing!");
                continue;
            }
            field.SetValue(result, Parse(subnode, field.FieldType));
            unused_nodes.Remove(name);
        }
        if (unused_nodes.Count > 0)
            throw new InvalidDataException($"While parsing {type.Name}, found unused nodes: {String.Join(", ", unused_nodes)}");
        return result;
    }

    private static MethodInfo FindParserMethod(MethodInfo[] methods)
    {
        foreach (var method in methods)
        {
            var att = method.GetCustomAttribute<ParserAttribute>();
            if (att != null)
                return method;
        }
        return null;
    }

    private static FieldInfo FindRootField(FieldInfo[] fields)
    {
        foreach (var field in fields)
        {
            var att = field.GetCustomAttribute<RootAttribute>();
            if (att != null)
                return field;
        }
        return null;
    }

    private static bool IsOptional(Type type, FieldInfo field)
    {
        var att = type.GetCustomAttribute<OptionalFieldsAttribute>();
        if (att != null)
            return true;
        return false;
    }

    private static string GetNameFromField(FieldInfo field)
    {
        return StringUtils.PascalToSnake(field.Name);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class OptionalFieldsAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)]
    public class RootAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class ParserAttribute : Attribute { }
}
