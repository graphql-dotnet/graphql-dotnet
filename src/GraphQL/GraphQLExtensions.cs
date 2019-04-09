using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace GraphQL
{
    public static class GraphQLExtensions
    {
        private static readonly Regex TrimPattern = new Regex("[\\[!\\]]", RegexOptions.Compiled);

        public static string TrimGraphQLTypes(this string name)
        {
            return TrimPattern.Replace(name, string.Empty).Trim();
        }

        public static bool IsCompositeType(this IGraphType type)
        {
            return type is IObjectGraphType ||
                   type is IInterfaceGraphType ||
                   type is UnionGraphType;
        }

        public static bool IsLeafType(this IGraphType type)
        {
            var namedType = type.GetNamedType();
            return namedType is ScalarGraphType || namedType is EnumerationGraphType;
        }

        public static bool IsInputType(this IGraphType type)
        {
            var namedType = type.GetNamedType();
            return namedType is ScalarGraphType ||
                   namedType is EnumerationGraphType ||
                   namedType is IInputObjectGraphType;
        }

        public static IGraphType GetNamedType(this IGraphType type)
        {
            IGraphType unmodifiedType = type;

            if (type is NonNullGraphType nonNull)
            {
                return GetNamedType(nonNull.ResolvedType);
            }

            if (type is ListGraphType list)
            {
                return GetNamedType(list.ResolvedType);
            }

            return unmodifiedType;
        }

        public static IGraphType BuildNamedType(this Type type, Func<Type, IGraphType> resolve = null)
        {
            if (resolve == null)
            {
                resolve = t => (IGraphType)Activator.CreateInstance(t);
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>))
                {
                    var nonNull = (NonNullGraphType)Activator.CreateInstance(type);
                    nonNull.ResolvedType = BuildNamedType(type.GenericTypeArguments[0], resolve);
                    return nonNull;
                }

                if (type.GetGenericTypeDefinition() == typeof(ListGraphType<>))
                {
                    var list = (ListGraphType)Activator.CreateInstance(type);
                    list.ResolvedType = BuildNamedType(type.GenericTypeArguments[0], resolve);
                    return list;
                }
            }

            return resolve(type) ??
                   throw new InvalidOperationException(
                       $"Expected non-null value, {nameof(resolve)} delegate return null for \"${type}\"");
        }

        public static Type GetNamedType(this Type type)
        {
            if (type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>) ||
                    type.GetGenericTypeDefinition() == typeof(ListGraphType<>)))
            {
                return GetNamedType(type.GenericTypeArguments[0]);
            }

            return type;
        }

        private static readonly IEnumerable<string> EmptyStringArray = new string[0];

        public static IEnumerable<string> IsValidLiteralValue(this IGraphType type, IValue valueAst, ISchema schema)
        {
            if (type is NonNullGraphType nonNull)
            {
                var ofType = nonNull.ResolvedType;

                if (valueAst == null || valueAst is NullValue)
                {
                    if (ofType != null)
                    {
                        return new[] { $"Expected \"{ofType.Name}!\", found null."};
                    }

                    return new[] { "Expected non-null value, found null"};
                }

                return IsValidLiteralValue(ofType, valueAst, schema);
            }
            else if (valueAst is NullValue)
            {
                return EmptyStringArray;
            }

            if (valueAst == null)
            {
                return EmptyStringArray;
            }

            // This function only tests literals, and assumes variables will provide
            // values of the correct type.
            if (valueAst is VariableReference)
            {
                return EmptyStringArray;
            }

            if (type is ListGraphType list)
            {
                var ofType = list.ResolvedType;

                if (valueAst is ListValue listValue)
                {
                    return listValue.Values
                        .SelectMany(value =>
                            IsValidLiteralValue(ofType, value, schema)
                                .Select((err, index) => $"In element #{index + 1}: {err}")
                        )
                        .ToList();
                }

                return IsValidLiteralValue(ofType, valueAst, schema);
            }

            if (type is IInputObjectGraphType inputType)
            {
                if (!(valueAst is ObjectValue objValue))
                {
                    return new[] {$"Expected \"{inputType.Name}\", found not an object."};
                }

                var fields = inputType.Fields.ToList();
                var fieldAsts = objValue.ObjectFields.ToList();

                var errors = new List<string>();

                // ensure every provided field is defined
                foreach (var providedFieldAst in fieldAsts)
                {
                    var found = fields.Find(x => x.Name == providedFieldAst.Name);
                    if (found == null)
                    {
                        errors.Add($"In field \"{providedFieldAst.Name}\": Unknown field.");
                    }
                }

                // ensure every defined field is valid
                foreach (var field in fields)
                {
                    var fieldAst = fieldAsts.Find(x => x.Name == field.Name);
                    var result = IsValidLiteralValue(field.ResolvedType, fieldAst?.Value, schema);

                    errors.AddRange(result.Select(err => $"In field \"{field.Name}\": {err}"));
                }

                return errors;
            }

            var scalar = (ScalarGraphType)type;

            var parseResult = scalar.ParseLiteral(valueAst);

            if (parseResult == null)
            {
                return new[] { $"Expected type \"{type.Name}\", found {AstPrinter.Print(valueAst)}." };
            }

            return EmptyStringArray;
        }

        public static string NameOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            var member = (MemberExpression)expression.Body;
            return member.Member.Name;
        }

        public static string DescriptionOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? (expr.Member.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description
                : null;
        }

        public static string DeprecationReasonOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? (expr.Member.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault() as ObsoleteAttribute)?.Message
                : null;
        }

        public static object DefaultValueOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? (expr.Member.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute)?.Value
                : null;
        }

        public static TMetadataProvider WithMetadata<TMetadataProvider>(this TMetadataProvider provider, string key, object value)
            where TMetadataProvider : IProvideMetadata
        {
            provider.Metadata[key] = value;
            return provider;
        }

        /// <summary>
        /// Provided a type and a super type, return true if the first type is either
        /// equal or a subset of the second super type (covariant).
        /// </summary>
        public static bool IsSubtypeOf(this IGraphType maybeSubType, IGraphType superType, ISchema schema)
        {
            if (maybeSubType.Equals(superType))
            {
                return true;
            }

            // If superType is non-null, maybeSubType must also be nullable.
            if (superType is NonNullGraphType sup1)
            {
                if (maybeSubType is NonNullGraphType sub)
                {
                    return IsSubtypeOf(sub.ResolvedType, sup1.ResolvedType, schema);
                }

                return false;
            }
            else if (maybeSubType is NonNullGraphType sub)
            {
                return IsSubtypeOf(sub.ResolvedType, superType, schema);
            }

            // If superType type is a list, maybeSubType type must also be a list.
            if (superType is ListGraphType sup)
            {
                if (maybeSubType is ListGraphType sub)
                {
                    return IsSubtypeOf(sub.ResolvedType, sup.ResolvedType, schema);
                }

                return false;
            }
            else if (maybeSubType is ListGraphType)
            {
                // If superType is not a list, maybeSubType must also be not a list.
                return false;
            }

            // If superType type is an abstract type, maybeSubType type may be a currently
            // possible object type.
            if (superType is IAbstractGraphType type &&
                maybeSubType is IObjectGraphType)
            {
                return type.IsPossibleType(maybeSubType);
            }

            return false;
        }

        /// <summary>
        /// Provided two composite types, determine if they "overlap". Two composite
        /// types overlap when the Sets of possible concrete types for each intersect.
        ///
        /// This is often used to determine if a fragment of a given type could possibly
        /// be visited in a context of another type.
        ///
        /// This function is commutative.
        /// </summary>
        public static bool DoTypesOverlap(this ISchema schema, IGraphType typeA, IGraphType typeB)
        {
            if (typeA.Equals(typeB))
            {
                return true;
            }

            var b = typeB as IAbstractGraphType;

            if (typeA is IAbstractGraphType a)
            {
                if (b != null)
                {
                    return a.PossibleTypes.Any(type => b.IsPossibleType(type));
                }

                return a.IsPossibleType(typeB);
            }

            if (b != null)
            {
                return b.IsPossibleType(typeA);
            }

            return false;
        }

        public static IValue AstFromValue(this object value, ISchema schema, IGraphType type)
        {
            if (type is NonNullGraphType nonnull)
            {
                return AstFromValue(value, schema, nonnull.ResolvedType);
            }

            if (value == null || type == null)
            {
                return new NullValue();
            }

            // Convert IEnumerable to GraphQL list. If the GraphQLType is a list, but
            // the value is not an IEnumerable, convert the value using the list's item type.
            if (type is ListGraphType listType)
            {
                var itemType = listType.ResolvedType;

                if (!(value is string) && value is IEnumerable list)
                {
                    var values = list
                        .Cast<object>()
                        .Select(item => AstFromValue(item, schema, itemType))
                        .ToList();

                    return new ListValue(values);
                }

                return AstFromValue(value, schema, itemType);
            }

            // Populate the fields of the input object by creating ASTs from each value
            // in the dictionary according to the fields in the input type.
            if (type is IInputObjectGraphType input)
            {
                if (!(value is Dictionary<string, object> dict))
                {
                    return null;
                }

                var fields = dict
                    .Select(pair =>
                    {
                        var fieldType = input.GetField(pair.Key)?.ResolvedType;
                        return new ObjectField(pair.Key, AstFromValue(pair.Value, schema, fieldType));
                    })
                    .ToList();

                return new ObjectValue(fields);
            }


            Invariant.Check(
                type.IsInputType(),
                $"Must provide Input Type, cannot use: {type}");


            var inputType = type as ScalarGraphType;

            // Since value is an internally represented value, it must be serialized
            // to an externally represented value before converting into an AST.
            var serialized = inputType.Serialize(value);
            if (serialized == null)
            {
                return null;
            }

            if (serialized is bool b)
            {
                return new BooleanValue(b);
            }

            if (serialized is int i)
            {
                return new IntValue(i);
            }

            if (serialized is long l)
            {
                return new LongValue(l);
            }

            if (serialized is decimal @decimal)
            {
                return new DecimalValue(@decimal);
            }

            if (serialized is double d)
            {
                return new FloatValue(d);
            }

            if (serialized is DateTime time)
            {
                return new DateTimeValue(time);
            }

            if (serialized is Uri uri)
            {
                return new UriValue(uri);
            }

            if (serialized is DateTimeOffset offset)
            {
                return new DateTimeOffsetValue(offset);
            }

            if (serialized is TimeSpan span)
            {
                return new TimeSpanValue(span);
            }

            if (serialized is Guid guid)
            {
                return new GuidValue(guid);
            }

            if(serialized is short int16)
            {
                return new ShortValue(int16);
            }

            if (serialized is ushort uint16)
            {
                return new UShortValue(uint16);
            }

            if (serialized is uint uint32)
            {
                return new UIntValue(uint32);
            }

            if (serialized is ulong uint64)
            {
                return new ULongValue(uint64);
            }

            if (serialized is string)
            {
                if (type is EnumerationGraphType)
                {
                    return new EnumValue(serialized.ToString());
                }

                return new StringValue(serialized.ToString());
            }

            var converter = schema.FindValueConverter(serialized, type);
            if (converter != null)
            {
                return converter.Convert(serialized, type);
            }

            throw new ExecutionError($"Cannot convert value to AST: {serialized}");
        }
    }
}
