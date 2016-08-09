using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GraphQl.SchemaGenerator.Attributes;
using GraphQl.SchemaGenerator.Extensions;
using GraphQl.SchemaGenerator.Types;
using GraphQl.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    public class GraphTypeConverter
    {
        public static Type ConvertTypeToGraphType(Type propertyType, bool isNotNull = false)
        {
            if (typeof(GraphType).IsAssignableFrom(propertyType))
            {
                return propertyType;
            }

            if (propertyType.IsValueType)
            {
                if (propertyType.IsAssignableToGenericType(typeof(Nullable<>)))
                {
                    isNotNull = false;
                    propertyType = propertyType.GetGenericArguments()[0];
                }
                else
                {
                    isNotNull = true;
                }
            }

            var graphType = BaseGraphType(propertyType);

            if (graphType != null && isNotNull)
            {
                if (!typeof(NonNullGraphType).IsAssignableFrom(graphType))
                {
                    return typeof(NonNullGraphType<>).MakeGenericType(graphType);
                }
            }

            return graphType;
        }

        /// <summary>
        ///     Get the base graph type not worrying about null.
        /// </summary>
        /// <param name="propertyType">Property type.</param>
        /// <returns>Type</returns>
        /// <exception cref="NotSupportedException">Cannot support IEnumerable when wrapping an object with GraphQL</exception>
        private static Type BaseGraphType(Type propertyType)
        {
            if (propertyType.IsEnum)
            {
                return typeof(EnumerationGraphTypeWrapper<>).MakeGenericType(propertyType);
            }

            if (propertyType == typeof(string) ||
                propertyType == typeof(char))
            {
                return typeof(StringGraphType);
            }

            if (propertyType == typeof(Guid))
            {
                return typeof(StringGraphType);
            }

            if (isIntegerType(propertyType))
            {
                return typeof(IntGraphType);
            }

            if (isFloatType(propertyType))
            {
                return typeof(FloatGraphType);
            }

            if (propertyType == typeof(bool))
            {
                return typeof(BooleanGraphType);
            }

            if (propertyType == typeof(DateTime))
            {
                return typeof(DateGraphType);
            }

            if (propertyType == typeof(TimeSpan))
            {
                return typeof(TimeSpanGraphType);
            }

            if (propertyType == typeof(byte[]))
            {
                return typeof(ByteArrayGraphType);
            }

            var genericType = propertyType.GetGenericType(typeof(IDictionary<,>));
            if (genericType != null)
            {
                var genericArgs = propertyType.GetGenericArguments();
                var keyGraphType = ConvertTypeToGraphType(genericArgs[0]);
                var valueGraphType = ConvertTypeToGraphType(genericArgs[1]);
                var keyValuePairGraphType = typeof(KeyValuePairGraphType<,>).MakeGenericType(
                    keyGraphType,
                    valueGraphType);

                return typeof(ListGraphType<>).MakeGenericType(keyValuePairGraphType);
            }

            if (propertyType.IsArray)
            {
                var itemType = propertyType.GetElementType();
                var itemGraphType = ConvertTypeToGraphType(itemType);
                if (itemGraphType != null)
                {
                    return typeof(ListGraphType<>).MakeGenericType(itemGraphType);
                }
            }

            if (propertyType.IsAssignableToGenericType(typeof(IEnumerable<>)))
            {
                var itemType = propertyType.GetGenericArguments()[0];
                var itemGraphType = ConvertTypeToGraphType(itemType);
                if (itemGraphType != null)
                {
                    return typeof(ListGraphType<>).MakeGenericType(itemGraphType);
                }

                // TODO: error?
            }

            if (propertyType.IsInterface || propertyType.IsAbstract)
            {
                return typeof(InterfaceGraphTypeWrapper<>).MakeGenericType(propertyType);
            }

            return typeof(ObjectGraphTypeWrapper<>).MakeGenericType(propertyType);
        }

        private static bool isFloatType(Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static bool isIntegerType(Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
