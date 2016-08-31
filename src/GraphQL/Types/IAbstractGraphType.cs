using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public interface IAbstractGraphType
    {
        Func<object, IObjectGraphType> ResolveType { get; set; }

        IEnumerable<IObjectGraphType> PossibleTypes { get; }

        void AddPossibleType(IObjectGraphType type);
    }


    public static class AbstractGraphTypeExtensions {

        public static bool IsPossibleType(this IAbstractGraphType abstractType, IGraphType type)
        {
            return abstractType.PossibleTypes.Any(x => x.Equals(type));
        }

        public static IObjectGraphType GetObjectType(this IAbstractGraphType abstractType, object value)
        {
            return abstractType.ResolveType != null 
                ? abstractType.ResolveType(value) 
                : GetTypeOf(abstractType, value);
        }

        public static IObjectGraphType GetTypeOf(this IAbstractGraphType abstractType, object value)
        {
            return abstractType.PossibleTypes.FirstOrDefault(type => type.IsTypeOf != null && type.IsTypeOf(value));
        }
    }
}
