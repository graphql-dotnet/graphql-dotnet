using System;
using System.Linq;

namespace GraphQL.SchemaGenerator.Extensions
{
    public static class AssignableExtensions
    {
        /// <summary>
        /// Determines whether the <paramref name="genericType"/> is assignable from
        /// <paramref name="givenType"/> taking into account generic definitions
        /// </summary>
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            if (givenType == null || genericType == null)
            {
                return false;
            }

            return givenType == genericType
              || givenType.mapsToGenericTypeDefinition(genericType)
              || givenType.hasInterfaceThatMapsToGenericTypeDefinition(genericType)
              || givenType.BaseType.IsAssignableToGenericType(genericType);
        }

        private static bool hasInterfaceThatMapsToGenericTypeDefinition(this Type givenType, Type genericType)
        {
            return Enumerable.Any(givenType
                  .GetInterfaces()
                  .Where(it => it.IsGenericType), it => it.GetGenericTypeDefinition() == genericType);
        }

        private static bool mapsToGenericTypeDefinition(this Type givenType, Type genericType)
        {
            return genericType.IsGenericTypeDefinition
              && givenType.IsGenericType
              && givenType.GetGenericTypeDefinition() == genericType;
        }

        /// <summary>
        /// Determines whether the <paramref name="genericType"/> is assignable from
        /// <paramref name="givenType"/> taking into account generic definitions
        /// </summary>
        public static Type GetGenericType(this Type givenType, Type genericType)
        {
            if (givenType == null || genericType == null)
            {
                return null;
            }

            if (givenType == genericType)
            {
                return givenType;
            }
            else
            {
                var resultType = givenType.getInterfaceThatMapsToGenericTypeDefinition(genericType);
                if (resultType == null)
                {
                    resultType = givenType.BaseType.GetGenericType(genericType);
                }

                return resultType;
            }
        }

        private static Type getInterfaceThatMapsToGenericTypeDefinition(this Type givenType, Type genericType)
        {
            return givenType
              .GetInterfaces()
              .Where(it => it.IsGenericType)
              .FirstOrDefault(it => it.GetGenericTypeDefinition() == genericType);
        }
    }
}
