using Microsoft.Win32;
using System.Collections.ObjectModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Exodus;

public class ExodusConfig : IYamlConvertible
{
    public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    {
        parser.Consume<MappingStart>();
        
        parser.Consume<MappingEnd>();
    }

    public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    {
        throw new NotImplementedException();
    }
}
