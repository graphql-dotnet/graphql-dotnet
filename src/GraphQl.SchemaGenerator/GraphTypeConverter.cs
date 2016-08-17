using System;
using System.Collections.Generic;
using GraphQL.SchemaGenerator.Extensions;
using GraphQL.SchemaGenerator.Types;
using GraphQL.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator
{
    public class GraphTypeConverter
    {
        public static Type ConvertTypeToGraphType(Type propertyType, bool isNotNull = false, bool isInputType = false)
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

            var graphType = BaseGraphType(propertyType, isInputType);

            if (graphType != null && isNotNull )
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
        private static Type BaseGraphType(Type propertyType, bool isInputType = false)
        {
            if (propertyType == typeof(EnumValues))
            {
                return typeof(EnumerationGraphType);
            }

            if (propertyType.IsEnum)
            {
                return typeof(EnumerationGraphTypeWrapper<>).MakeGenericType(propertyType);
            }

            if (propertyType == typeof(void))
            {
                return null;
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

            if (isDecimalType(propertyType))
            {
                return typeof(DecimalGraphType);
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

            if (propertyType.IsAssignableToGenericType(typeof(IDictionary<,>)))
            {
                var genericArgs = propertyType.GetGenericArguments();
                var keyGraphType = ConvertTypeToGraphType(genericArgs[0], isInputType: isInputType);
                var valueGraphType = ConvertTypeToGraphType(genericArgs[1], isInputType: isInputType);
                var keyValuePairGraphType = typeof(KeyValuePairGraphType<,>).MakeGenericType(
                    keyGraphType, valueGraphType);

                if (isInputType)
                {
                    var inputPairGraphType = typeof(KeyValuePairInputGraphType<,>).MakeGenericType(
                        keyGraphType, valueGraphType);

                    return typeof(ListGraphType<>).MakeGenericType(inputPairGraphType);
                }

                return typeof(ListGraphType<>).MakeGenericType(keyValuePairGraphType);
            }

            if (propertyType.IsArray)
            {
                var itemType = propertyType.GetElementType();
                var itemGraphType = ConvertTypeToGraphType(itemType, isInputType: isInputType);
                if (itemGraphType != null)
                {
                    return typeof(ListGraphType<>).MakeGenericType(itemGraphType);
                }
            }

            if (propertyType.IsAssignableToGenericType(typeof(IEnumerable<>)))
            {
                var itemType = propertyType.GetGenericArguments()[0];
                var itemGraphType = ConvertTypeToGraphType(itemType, isInputType: isInputType);
                if (itemGraphType != null)
                {
                    return typeof(ListGraphType<>).MakeGenericType(itemGraphType);
                }
            }

            if (propertyType.IsInterface || propertyType.IsAbstract)
            {
                return typeof(InterfaceGraphTypeWrapper<>).MakeGenericType(propertyType);
            }

            if (isInputType)
            {
                return typeof(InputObjectGraphTypeWrapper<>).MakeGenericType(propertyType);
            }

            return typeof(ObjectGraphTypeWrapper<>).MakeGenericType(propertyType);
        }

        private static bool isDecimalType(Type type)
        {
            var typeCode = Type.GetTypeCode(type);

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
            var typeCode = Type.GetTypeCode(type);

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