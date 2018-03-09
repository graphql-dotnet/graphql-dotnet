using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Dynamic.Types.LiteralGraphType
{

    public class LiteralGraphType<T> : ObjectGraphType<T>
    {
        private bool _hasAddedFields;

        public override string CollectTypes(TypeCollectionContext context)
        {
            Name = Name ?? typeof(T).Name;

            if (!_hasAddedFields)
            {
                var fields = GetFields().Where(f => f != null).ToList();
                foreach (var field in fields)
                {
                    AddField(field);
                }

                _hasAddedFields = true;
            }

            return base.CollectTypes(context);
        }

        private IEnumerable<FieldType> GetFields()
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            var members = typeof(T)
                .GetProperties(bindingFlags)
                .AsEnumerable<MemberInfo>()
                .Union(typeof(T).GetFields(bindingFlags))
                .Select(member =>
                {
                    var type = GetTypeForMemberInfo(member);

                    return new TypedLiteralGraphTypeMemberInfo
                    {
                        DeclaringTypeName = member.DeclaringType.FullName,
                        Name = member.Name,
                        Type = LiteralGraphTypeHelpers.GetLiteralGraphTypeMemberInfoTypeForType(type),
                        IsList = IsListType(type),
                        ActualType = type,
                        GetValueFn = MakeGetValueFnForMemberInfo(member)
                    };
                })
                .ToList();

            FieldTypeResolver complexTypeResolver = member =>
            {
                if (!(member is TypedLiteralGraphTypeMemberInfo typedMember))
                {
                    return null;
                }

                var maybeActualType = typedMember.ActualType;
                if (maybeActualType == null)
                {
                    return null;
                }

                var literalGraphType = typedMember.IsList
                    // IEnumerable<T> -> ListGraphType<LiteralGraphType<T>> 
                    ? typeof(ListGraphType<>).MakeGenericType(typeof(LiteralGraphType<>).MakeGenericType(GetListElementType(maybeActualType)))
                    // T -> LiteralGraphType<T>
                    : typeof(LiteralGraphType<>).MakeGenericType(maybeActualType);

                return typeof(LiteralGraphTypeHelpers)
                    .GetMethod(nameof(LiteralGraphTypeHelpers.CreateFieldType), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(literalGraphType)
                    .Invoke(null, new[] { member }) as FieldType;
            };

            return members
                .Select(member =>
                {
                    try
                    {
                        // Find the field type 
                        var fieldType = LiteralGraphTypeHelpers.GetFieldTypeForMember(member, complexTypeResolver);

                        return fieldType;
                    }
                    catch (Exception)
                    {
                        // TODO: log

                        return null;
                    }
                })
                .ToList();
        }

        private Type GetListElementType(Type type)
        {
            var listType = IsGenericListType(type)
                ? type
                : type.GetInterfaces()?.FirstOrDefault(i => IsGenericListType(i));

            if (listType == null)
            {
                return null;
            }

            return listType.GetGenericArguments().FirstOrDefault();

        }
        private static bool IsListType(Type type) => type.GetInterfaces().Any(i => i == typeof(IEnumerable));
        private static bool IsGenericListType(Type maybeListType) => maybeListType.IsGenericType && maybeListType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        private GetValueFn MakeGetValueFnForMemberInfo(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return ctx => field.GetValue(ctx.Source);
            }

            if (member is PropertyInfo property)
            {
                return ctx => property.GetValue(ctx.Source);
            }

            throw new NotImplementedException($"Tried calling GetValueFn with unknown member type: {member.MemberType}");
        }

        private static Type GetTypeForMemberInfo(MemberInfo member)
        {
            return member is FieldInfo field
                ? field.FieldType
                : member is PropertyInfo property
                    ? property.PropertyType
                    : null;
        }

        internal class TypedLiteralGraphTypeMemberInfo : LiteralGraphTypeMemberInfo
        {
            public Type ActualType { get; set; }
        }
    }
}
