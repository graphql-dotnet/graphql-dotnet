using System;
using GraphQL.Types;

namespace GraphQL.StarWars
{
    public class StarWarsSchema : Schema
    {
        public StarWarsSchema(Func<Type, GraphType> resolveType)
            : base(resolveType)
        {
            Query = (ObjectGraphType)resolveType(typeof (StarWarsQuery));
        }
    }
}
