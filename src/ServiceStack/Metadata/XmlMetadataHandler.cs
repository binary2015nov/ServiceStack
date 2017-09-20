using System;
using ServiceStack.Serialization;

namespace ServiceStack.Metadata
{
    public class XmlMetadataHandler : BaseMetadataHandler
    {
        public override Format Format => Format.Xml;

        protected override string CreateMessage(Type dtoType)
        {
            return DataContractSerializer.Instance.Parse(dtoType);
        }
    }
}