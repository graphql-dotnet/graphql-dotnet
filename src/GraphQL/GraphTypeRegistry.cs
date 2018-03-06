using System;
using System.Collections.Generic;

using GraphQL.Types;

namespace GraphQL
{
    public static class GraphTypeRegistry
    {
        static Dictionary<Type, Type> _entries;

        static GraphTypeRegistry()
        {
            _entries = new Dictionary<Type, Type>();

            _entries[typeof(int)] = typeof(IntGraphType);
            _entries[typeof(long)] = typeof(IntGraphType);
            _entries[typeof(double)] = typeof(FloatGraphType);
            _entries[typeof(float)] = typeof(FloatGraphType);
            _entries[typeof(decimal)] = typeof(DecimalGraphType);
            _entries[typeof(string)] = typeof(StringGraphType);
            _entries[typeof(bool)] = typeof(BooleanGraphType);
            _entries[typeof(DateTime)] = typeof(DateGraphType);
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
