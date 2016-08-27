using System;
using GraphQL.Language;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public static class TypeExtensions
    {
        public static GraphType GraphTypeFromType(this IType type, ISchema schema)
        {
            if (type is NonNullType)
            {
                var nonnull = (NonNullType)type;
                var ofType = GraphTypeFromType(nonnull.Type, schema);
                var nonnullGraphType = typeof(NonNullGraphType<>).MakeGenericType(ofType.GetType());
                return (GraphType)Activator.CreateInstance(nonnullGraphType);
            }

            if (type is ListType)
            {
                var list = (ListType)type;
                var ofType = GraphTypeFromType(list.Type, schema);
                var listGraphType = typeof(ListGraphType<>).MakeGenericType(ofType.GetType());
                return (GraphType)Activator.CreateInstance(listGraphType);
            }

            if (type is NamedType)
            {
                var named = (NamedType)type;
                return schema.FindType(named.Name);
            }

            return null;
        }

        public static string Name(this IType type)
        {
            if (type is NonNullType)
            {
                var nonnull = (NonNullType)type;
                return Name(nonnull.Type);
            }

            if (type is ListType)
            {
                var list = (ListType)type;
                return Name(list.Type);
            }

            return ((NamedType)type).Name;
        }

        public static string FullName(this IType type)
        {
            if (type is NonNullType)
            {
                var nonnull = (NonNullType)type;
                return "{0}!".ToFormat(FullName(nonnull.Type));
            }

            if (type is ListType)
            {
                var list = (ListType)type;
                return "[{0}]".ToFormat(FullName(list.Type));
            }

            return ((NamedType)type).Name;
        }
    }
}
