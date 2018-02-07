using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public static class TypeExtensions
    {
        public static IGraphType GraphTypeFromType(this IType type, ISchema schema)
        {
            if (type == null) return null;

            if (type is NonNullType)
            {
                var nonNull = (NonNullType)type;
                var ofType = GraphTypeFromType(nonNull.Type, schema);
                if(ofType == null)
                {
                    return null;
                }
                var nonNullGraphType = typeof(NonNullGraphType<>).MakeGenericType(ofType.GetType());
                var instance = (NonNullGraphType)Activator.CreateInstance(nonNullGraphType);
                instance.ResolvedType = ofType;
                return instance;
            }

            if (type is ListType)
            {
                var list = (ListType)type;
                var ofType = GraphTypeFromType(list.Type, schema);
                if(ofType == null)
                {
                    return null;
                }
                var listGraphType = typeof(ListGraphType<>).MakeGenericType(ofType.GetType());
                var instance = (ListGraphType)Activator.CreateInstance(listGraphType);
                instance.ResolvedType = ofType;
                return instance;
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
                var nonNull = (NonNullType)type;
                return Name(nonNull.Type);
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
                var nonNull = (NonNullType)type;
                return "{0}!".ToFormat(FullName(nonNull.Type));
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
