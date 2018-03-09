using System;

namespace GraphQL.Dynamic.Types.LiteralGraphType
{
    public class RemoteLiteralGraphTypeMetadataAttribute : Attribute
    {
        public string RemoteMoniker { get; set; }
        public string RemoteUrl { get; set; }
        public string Name { get; set; }

        public RemoteLiteralGraphTypeMetadataAttribute(string remoteMoniker, string remoteUrl, string name)
        {
            RemoteMoniker = remoteMoniker;
            RemoteUrl = remoteUrl;
            Name = name;
        }
    }
}
