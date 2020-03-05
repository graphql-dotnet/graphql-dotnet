using System.Collections.Generic;
using GraphQL.Resolvers;

namespace GraphQL.Types
{
    public interface IFieldType : IHaveDefaultValue, IProvideMetadata, INamedType
    {
        string DeprecationReason { get; }

        IEnumerable<IQueryArgument> Arguments { get; }

        IFieldResolver Resolver { get; }
    }
}
