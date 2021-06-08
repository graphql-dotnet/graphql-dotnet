using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL
{
    internal static class AssemblyExtensions
    {
        public static List<(Type ClrType, Type GraphType)> GetClrTypeMappings(this Assembly assembly)
        {
            var typesToRegister = new Type[]
            {
                typeof(ObjectGraphType<>),
                typeof(InputObjectGraphType<>),
                typeof(EnumerationGraphType<>),
            };

            var types = assembly
                .GetTypes()
                .Where(x => !x.IsAbstract && !x.IsInterface);

            var typeMappings = new List<(Type clrType, Type graphType)>();
            foreach (var graphType in types)
            {
                //skip types marked with the DoNotRegister attribute
                if (graphType.GetCustomAttributes(false).Any(y => y.GetType() == typeof(DoNotRegisterAttribute)))
                    continue;
                //get the base type
                var baseType = graphType.BaseType;
                while (baseType != null)
                {
                    //skip types marked with the DoNotRegister attribute
                    if (baseType.GetCustomAttributes(false).Any(y => y.GetType() == typeof(DoNotRegisterAttribute)))
                        break;
                    //look for generic types that match our list above
                    if (baseType.IsConstructedGenericType && typesToRegister.Contains(baseType.GetGenericTypeDefinition()))
                    {
                        //get the base type
                        var clrType = baseType.GetGenericArguments()[0];
                        //and register it
                        if (clrType != typeof(object))
                            typeMappings.Add((clrType, graphType));
                        //skip to the next type
                        break;
                    }
                    //look up the inheritance chain for a match
                    baseType = baseType.BaseType;
                }
            }

            return typeMappings;
        }
    }
}
