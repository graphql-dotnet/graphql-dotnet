using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public static class GraphTypeTypeRegistry
    {
        static readonly Dictionary<Type, Type> _entries;

        static GraphTypeTypeRegistry()
        {
            _entries = new Dictionary<Type, Type>
            {
                [typeof(int)] = typeof(IntGraphType),
                [typeof(long)] = typeof(IntGraphType),
                [typeof(double)] = typeof(FloatGraphType),
                [typeof(float)] = typeof(FloatGraphType),
                [typeof(decimal)] = typeof(DecimalGraphType),
                [typeof(string)] = typeof(StringGraphType),
                [typeof(bool)] = typeof(BooleanGraphType),
                [typeof(DateTime)] = typeof(DateGraphType),
                [typeof(DateTimeOffset)] = typeof(DateGraphType)
            };
        }

        public static void Register(Type clrType, Type graphType)
        {
            _entries[clrType] = graphType;
        }

        public static Type Get(Type clrType)
        {
            if (_entries.TryGetValue(clrType, out var graphType))
            {
                return graphType;
            }

            return null;
        }
    }
}
