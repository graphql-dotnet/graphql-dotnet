using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public interface IAbstractGraphType
    {
        Func<object, ObjectGraphType> ResolveType { get; set; }

        List<ObjectGraphType> PossibleTypes { get; }        
    }


    public static class AbstractGraphTypeExtensions {
        public static void AddPossibleType(this IAbstractGraphType abstractType, ObjectGraphType type)
        {
            if (type != null && !abstractType.PossibleTypes.Contains(type))
            {
                abstractType.PossibleTypes.Add(type);
            }
        }

        public static bool IsPossibleType(this IAbstractGraphType abstractType, GraphType type)
        {
            return abstractType.PossibleTypes.Any(x => x.Equals(type));
        }

        public static ObjectGraphType GetObjectType(this IAbstractGraphType abstractType, object value)
        {
            return abstractType.ResolveType != null 
                ? abstractType.ResolveType(value) 
                : GetTypeOf(abstractType, value);
        }

        public static ObjectGraphType GetTypeOf(this IAbstractGraphType abstractType, object value)
        {
            return abstractType.PossibleTypes.FirstOrDefault(type => type.IsTypeOf != null && type.IsTypeOf(value));
        }
    }
}
