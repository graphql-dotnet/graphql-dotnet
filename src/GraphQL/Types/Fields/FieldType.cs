using GraphQL.Resolvers;
using System;
using System.Diagnostics;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class FieldType : MetadataProvider, IFieldType
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public object DefaultValue { get; set; }

        public Type Type { get; set; }

        public IGraphType ResolvedType { get; set; }

        public QueryArguments Arguments { get; set; }

        public IFieldResolver Resolver { get; set; }
    }
}
