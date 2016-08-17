using System;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs.Configuration
{
    /// <summary>
    ///     Schema with every known complicated setup for testing
    /// </summary>
    public class AdvancedSchema : GraphQL.Types.Schema
    {
        public AdvancedSchema(Func<Type, GraphType> resolveType)
            : base(resolveType)
        {
            Query = (ObjectGraphType)resolveType(typeof (AdvancedQuery));
        }
    }
}
