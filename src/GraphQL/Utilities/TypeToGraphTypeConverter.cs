using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class TypeToGraphTypeConverter
    {
        private readonly IDictionary<Type, IGraphType> _typeMap;

        public TypeToGraphTypeConverter()
        {
            var stringType = new GraphQLTypeReference("String");
            var booleanType = new GraphQLTypeReference("Boolean");
            var integerType = new GraphQLTypeReference("Int");
            var floatType = new GraphQLTypeReference("Float");
            var decimalType = new GraphQLTypeReference("Decimal");
            var dateType = new GraphQLTypeReference("Date");
            var idType = new GraphQLTypeReference("ID");

            _typeMap = new Dictionary<Type, IGraphType>
            {
                { typeof(void), booleanType },
                { typeof(string), stringType},
                { typeof(bool), booleanType},
                { typeof(byte), integerType},
                { typeof(char), integerType},
                { typeof(short), integerType},
                { typeof(ushort), integerType },
                { typeof(int), integerType },
                { typeof(uint), integerType },
                { typeof(long), integerType },
                { typeof(ulong), integerType },
                { typeof(float), floatType },
                { typeof(double), floatType },
                { typeof(decimal), decimalType },
                { typeof(DateTime), dateType },
                { typeof(Guid), idType },
            };
        }

        public IGraphType Convert(Type type)
        {
            _typeMap.TryGetValue(type, out IGraphType graphType);
            return graphType;
        }

        public void Register(Type type, IGraphType graphType)
        {
            _typeMap[type] = graphType;
        }
    }
}