#if !LITE
using System;
using System.IO;
using ServiceStack.Text;

namespace ServiceStack.Serialization
{
    public partial class DataContractSerializer
    {
        public object DeserializeFromString(string xmlString, Type type)
        {
            return XmlSerializer.Deserialize(xmlString, type);   
        }

        public T DeserializeFromString<T>(string xmlString)
        {
            return XmlSerializer.Deserialize<T>(xmlString);
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            return XmlSerializer.Deserialize<T>(stream);
        }
    }
}
#endif