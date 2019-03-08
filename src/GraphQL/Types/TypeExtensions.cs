using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public static class TypeExtensions
    {
        public static IGraphType GraphTypeFromType(this IType type, ISchema schema)
        {
            if (type == null) return null;

            if (type is NonNullType nonnull)
            {
                var ofType = GraphTypeFromType(nonnull.Type, schema);
                if (ofType == null)
                {
                    return null;
                }
                var nonnullGraphType = typeof(NonNullGraphType<>).MakeGenericType(ofType.GetType());
                var instance = (NonNullGraphType)Activator.CreateInstance(nonnullGraphType);
                instance.ResolvedType = ofType;
                return instance;
            }

            if (type is ListType list)
            {
                var ofType = GraphTypeFromType(list.Type, schema);
                if (ofType == null)
                {
                    return null;
                }
                var listGraphType = typeof(ListGraphType<>).MakeGenericType(ofType.GetType());
                var instance = (ListGraphType)Activator.CreateInstance(listGraphType);
                instance.ResolvedType = ofType;
                return instance;
            }

            if (type is NamedType named)
            {
                return schema.FindType(named.Name);
            }

            return null;
        }

        public static string Name(this IType type)
        {
            if (type is NonNullType nonnull)
            {
                return Name(nonnull.Type);
            }

            if (type is ListType list)
            {
                return Name(list.Type);
            }

            return ((NamedType)type).Name;
        }

        public static string FullName(this IType type)
        {
            if (type is NonNullType nonnull)
            {
                return "{0}!".ToFormat(FullName(nonnull.Type));
            }

            if (type is ListType list)
            {
                return "[{0}]".ToFormat(FullName(list.Type));
            }

            return ((NamedType)type).Name;
        }
    }
}
