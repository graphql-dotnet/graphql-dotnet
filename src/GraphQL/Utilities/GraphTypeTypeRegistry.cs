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
                [typeof(DateTimeOffset)] = typeof(DateTimeOffsetGraphType),
                [typeof(TimeSpan)] = typeof(TimeSpanSecondsGraphType),
                [typeof(Guid)] = typeof(IdGraphType),
                [typeof(short)] = typeof(ShortGraphType),
                [typeof(ushort)] = typeof(UShortGraphType),
                [typeof(ulong)] = typeof(ULongGraphType),
                [typeof(uint)] = typeof(UIntGraphType),
                [typeof(byte)] = typeof(ByteGraphType),
                [typeof(sbyte)] = typeof(SByteGraphType),
            };
        }

        public static void Register<T, TGraph>() where TGraph : GraphType
        {
            Register(typeof(T), typeof(TGraph));
        }

        public static void Register(Type clrType, Type graphType)
        {
            _entries[clrType ?? throw new ArgumentNullException(nameof(clrType))] = graphType ?? throw new ArgumentNullException(nameof(graphType));
        }

        public static Type Get<TClr>() => Get(typeof(TClr));

        public static Type Get(Type clrType) => _entries.TryGetValue(clrType, out var graphType) ? graphType : null;

        public static bool Contains(Type clrType) => _entries.ContainsKey(clrType);
    }
}
