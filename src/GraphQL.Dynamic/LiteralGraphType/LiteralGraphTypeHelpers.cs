using System;
using System.Collections.Concurrent;
using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Dynamic.Types.LiteralGraphType
{
    internal delegate FieldType FieldTypeResolver(LiteralGraphTypeMemberInfo m);
    internal delegate object GetValueFn(ResolveFieldContext context);

    internal static class LiteralGraphTypeHelpers
    {
        private static ConcurrentDictionary<string, FieldTypeResolver> MemberFieldTypeResolverMappings { get; set; } = new ConcurrentDictionary<string, FieldTypeResolver>();
        private static ConcurrentDictionary<string, FieldType> MemberFieldTypeMappings { get; set; } = new ConcurrentDictionary<string, FieldType>();

        private static ConcurrentDictionary<LiteralGraphTypeMemberInfoType, Type> LiteralGraphTypeMemberInfoTypeMappings { get; set; } = new ConcurrentDictionary<LiteralGraphTypeMemberInfoType, Type>();
        private static ConcurrentDictionary<LiteralGraphTypeMemberInfoType, FieldTypeResolver> LiteralGraphTypeMemberInfoFieldTypeResolverMappings { get; set; } = new ConcurrentDictionary<LiteralGraphTypeMemberInfoType, FieldTypeResolver>();

        static LiteralGraphTypeHelpers()
        {
            PopulateMappings();
        }

        private static void PopulateTypeMapping(LiteralGraphTypeMemberInfoType type, Type actualType, FieldTypeResolver resolver)
        {
            LiteralGraphTypeMemberInfoTypeMappings.TryAdd(type, actualType);
            LiteralGraphTypeMemberInfoFieldTypeResolverMappings.TryAdd(type, resolver);
        }

        private static void PopulateMappings()
        {
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.String, typeof(string), CreateFieldType<StringGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.Int, typeof(int), CreateFieldType<IntGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.Long, typeof(long), CreateFieldType<IntGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.Float, typeof(float), CreateFieldType<FloatGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.Double, typeof(double), CreateFieldType<FloatGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.Boolean, typeof(bool), CreateFieldType<BooleanGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.Guid, typeof(Guid), CreateFieldType<IdGraphType>);
            PopulateTypeMapping(LiteralGraphTypeMemberInfoType.DateTimeOffset, typeof(DateTimeOffset), CreateFieldType<DateGraphType>);
        }

        internal static string MakeMemberFieldTypeResolverMapKey(LiteralGraphTypeMemberInfo member) => $"{member.DeclaringTypeName}.{member.Name}";

        private static string MakeMemberFieldTypeMapKey(LiteralGraphTypeMemberInfo member) => $"{member.DeclaringTypeName}.{member.Name}";

        internal static IFieldResolver CreateFieldResolverFor(LiteralGraphTypeMemberInfo member)
        {
            return new FuncFieldResolver<object>(ctx => member.GetValueFn(ctx));
        }

        internal static FieldType CreateFieldType<TResolvedType>(LiteralGraphTypeMemberInfo member)
            where TResolvedType : IGraphType, new()
        {
            return new FieldType
            {
                Name = member.Name,
                Type = typeof(TResolvedType),
                Resolver = CreateFieldResolverFor(member)
            };
        }

        private static void PatchFieldType(FieldType target, FieldType source)
        {
            target.Name = source.Name;
            target.Type = source.Type;
            target.Resolver = source.Resolver;
        }

        internal static FieldTypeResolver MakeFieldTypeResolverForMember(LiteralGraphTypeMemberInfo member, FieldTypeResolver complexFieldTypeResolver)
        {
            var type = member.Type;
            if (type == LiteralGraphTypeMemberInfoType.Complex)
            {
                return complexFieldTypeResolver;
            }

            LiteralGraphTypeMemberInfoFieldTypeResolverMappings.TryGetValue(type, out var resolver);

            return resolver;
        }

        internal static LiteralGraphTypeMemberInfoType GetLiteralGraphTypeMemberInfoTypeForType(Type type)
        {
            var memberInfoType = LiteralGraphTypeMemberInfoTypeMappings.Keys.FirstOrDefault(key => LiteralGraphTypeMemberInfoTypeMappings[key] == type);
            if (memberInfoType != LiteralGraphTypeMemberInfoType.Unknown)
            {
                return memberInfoType;
            }

            return type.IsPrimitive
                ? LiteralGraphTypeMemberInfoType.Unknown
                : LiteralGraphTypeMemberInfoType.Complex;
        }

        internal static FieldTypeResolver GetFieldTypeResolverForMember(LiteralGraphTypeMemberInfo member, FieldTypeResolver complexFieldTypeResolver)
        {
            var memberFieldTypeResolverMapKey = MakeMemberFieldTypeResolverMapKey(member);

            // If we don't already know the fieldtype resolver for this member, we need to figure it out
            if (!MemberFieldTypeResolverMappings.TryGetValue(memberFieldTypeResolverMapKey, out var fieldTypeResolver) || fieldTypeResolver == null)
            {
                // Try to create a new field type resolver
                fieldTypeResolver = MakeFieldTypeResolverForMember(member, complexFieldTypeResolver);
                if (fieldTypeResolver == null)
                {
                    return null;
                }

                // Save the resolver
                MemberFieldTypeResolverMappings.AddOrUpdate(memberFieldTypeResolverMapKey, fieldTypeResolver, (m, old) => fieldTypeResolver);
            }

            return fieldTypeResolver;
        }

        internal static FieldType GetFieldTypeForMember(LiteralGraphTypeMemberInfo member, FieldTypeResolver complexFieldTypeResolver)
        {
            var memberFieldTypeMapKey = MakeMemberFieldTypeMapKey(member);

            // Try to find an existing FieldType for this member
            if (!MemberFieldTypeMappings.TryGetValue(memberFieldTypeMapKey, out var fieldType))
            {
                // Throw a new FieldType in to indicate we're in the middle of resolving a FieldType
                fieldType = new FieldType();
                MemberFieldTypeMappings.AddOrUpdate(memberFieldTypeMapKey, fieldType, (m, old) => fieldType);

                // Get the fieldtype resolver
                var fieldTypeResolver = GetFieldTypeResolverForMember(member, complexFieldTypeResolver);

                // Execute the resolver
                var actualFieldType = fieldTypeResolver(member);

                // Copy over fields
                PatchFieldType(fieldType, actualFieldType);
            }

            return fieldType;
        }
    }
}
