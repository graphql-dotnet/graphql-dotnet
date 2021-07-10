using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL
{
    internal static class AssemblyExtensions
    {
        /// <summary>
        /// Contains a list of types that are scanned for, from which a clr type mapping will be matched
        /// </summary>
        private static readonly Type[] _typesToRegister = new Type[]
            {
                typeof(ObjectGraphType<>),
                typeof(InputObjectGraphType<>),
                typeof(EnumerationGraphType<>),
            };

        /// <summary>
        /// Scans the specified assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// returns a list of mappings between matched classes and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        public static List<(Type ClrType, Type GraphType)> GetClrTypeMappings(this Assembly assembly)
        {
            //create a list of type mappings
            var typeMappings = new List<(Type clrType, Type graphType)>();

            //loop through each type in the specified assembly
            foreach (var graphType in assembly.GetTypes())
            {
                //skip types that are not graph types
                if (!typeof(IGraphType).IsAssignableFrom(graphType))
                    continue;

                //skip abstract types and interfaces
                if (graphType.IsAbstract || graphType.IsInterface)
                    continue;

                //skip types marked with the DoNotRegister attribute
                if (graphType.GetCustomAttributes(false).Any(y => y.GetType() == typeof(DoNotMapClrTypeAttribute)))
                    continue;

                //start with the base type
                var baseType = graphType.BaseType;
                while (baseType != null)
                {
                    //skip types marked with the DoNotRegister attribute
                    if (baseType.GetCustomAttributes(false).Any(y => y.GetType() == typeof(DoNotMapClrTypeAttribute)))
                        break;

                    //look for generic types that match our list above
                    if (baseType.IsConstructedGenericType && _typesToRegister.Contains(baseType.GetGenericTypeDefinition()))
                    {
                        //get the base type
                        var clrType = baseType.GetGenericArguments()[0];

                        //as long as it's not of type 'object', register it
                        if (clrType != typeof(object))
                            typeMappings.Add((clrType, graphType));

                        //skip to the next type
                        break;
                    }

                    //look up the inheritance chain for a match
                    baseType = baseType.BaseType;
                }
            }

            //return the list of type mappings
            return typeMappings;
        }
    }
}
