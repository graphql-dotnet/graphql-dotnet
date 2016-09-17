using GraphQL.Resolvers;
using System;

namespace GraphQL.Types
{
    public interface IFieldType : IHaveDefaultValue, IReferenceTarget
    {
        string Name { get; set; }
        string Description { get; set; }
        string DeprecationReason { get; set; }
        QueryArguments Arguments { get; set; }
    }

    public class FieldType : IFieldType
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public object DefaultValue { get; set; }
        public IGraphType Type { get; set; }
        public QueryArguments Arguments { get; set; }
        public IFieldResolver Resolver { get; set; }
    }
}
