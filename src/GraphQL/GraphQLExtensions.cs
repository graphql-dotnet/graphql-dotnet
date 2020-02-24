using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.RegularExpressions;

namespace GraphQL
{
    public static class GraphQLExtensions
    {
        private static readonly Regex _trimPattern = new Regex("[\\[!\\]]", RegexOptions.Compiled);

        public static string TrimGraphQLTypes(this string name)
        {
            return _trimPattern.Replace(name, string.Empty).Trim();
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
            return namedType is ScalarGraphType;
        }

        // https://graphql.github.io/graphql-spec/June2018/#sec-Input-and-Output-Types
        public static bool IsInputType(this Type type)
        {
            var namedType = type.GetNamedType();
            return typeof(ScalarGraphType).IsAssignableFrom(namedType) ||
                   typeof(IInputObjectGraphType).IsAssignableFrom(namedType);
        }

        // https://graphql.github.io/graphql-spec/June2018/#sec-Input-and-Output-Types
        public static bool IsInputType(this IGraphType type)
        {
            var namedType = type.GetNamedType();
            return namedType is ScalarGraphType ||
                   namedType is IInputObjectGraphType;
        }

        // https://graphql.github.io/graphql-spec/June2018/#sec-Input-and-Output-Types
        public static bool IsOutputType(this Type type)
        {
            var namedType = type.GetNamedType();
            return typeof(ScalarGraphType).IsAssignableFrom(namedType) ||
                   typeof(IObjectGraphType).IsAssignableFrom(namedType) ||
                   typeof(IInterfaceGraphType).IsAssignableFrom(namedType) ||
                   typeof(UnionGraphType).IsAssignableFrom(namedType);
        }

        // https://graphql.github.io/graphql-spec/June2018/#sec-Input-and-Output-Types
        public static bool IsOutputType(this IGraphType type)
        {
            var namedType = type.GetNamedType();
            return namedType is ScalarGraphType ||
                   namedType is IObjectGraphType ||
                   namedType is IInterfaceGraphType ||
                   namedType is UnionGraphType;
        }

        public static bool IsInputObjectType(this IGraphType type)
        {
            var namedType = type.GetNamedType();
            return namedType is IInputObjectGraphType;
        }

        /// <summary>
        /// Determines if this graph type is an introspection type
        /// </summary>
        public static bool IsIntrospectionType(this IGraphType graphType) => graphType?.Name?.StartsWith("__") ?? false;

        /// <summary>
        /// Determines if this field is an introspection type
        /// </summary>
        public static bool IsIntrospectionField(this FieldType fieldType) => fieldType?.Name?.StartsWith("__") ?? false;

        public static IGraphType GetNamedType(this IGraphType type)
        {
            return type switch
            {
                NonNullGraphType nonNull => GetNamedType(nonNull.ResolvedType),
                ListGraphType list => GetNamedType(list.ResolvedType),
                _ => type
            };
        }

        public static Type GetNamedType(this Type type)
        {
            if (!type.IsGenericType)
                return type;

            var genericDef = type.GetGenericTypeDefinition();
            return genericDef == typeof(NonNullGraphType<>) || genericDef == typeof(ListGraphType<>)
                ? GetNamedType(type.GenericTypeArguments[0])
                : type;
        }

        /// <summary>
        /// An Interface defines a list of fields; Object types that implement that interface are guaranteed to implement those fields.
        /// Whenever the type system claims it will return an interface, it will return a valid implementing type.
        /// </summary>
        /// <param name="iface"></param>
        /// <param name="type"></param>
        /// <param name="throwError"> Set to <c>true</c> to generate an error if the type does not match the interface. </param>
        /// <returns></returns>
        public static bool IsValidInterfaceFor(this IInterfaceGraphType iface, IObjectGraphType type, bool throwError = true)
        {
            foreach (var field in iface.Fields)
            {
                var found = type.GetField(field.Name);

                if (found == null)
                {
                    return throwError ? throw new ArgumentException($"Type '{type.Name}' ({type.GetType().GetFriendlyName()}) does not implement '{iface.Name}' interface. Type '{type.Name}' has no field '{field.Name}'.") : false;
                }

                if (found.Type != field.Type)
                {
                    return throwError ? throw new ArgumentException($"Type '{type.Name}' ({type.GetType().GetFriendlyName()}) does not implement '{iface.Name}' interface. Field '{type.Name}.{field.Name}' must be of type '{field.Type.GetFriendlyName()}', but in fact it is of type '{found.Type.GetFriendlyName()}'.") : false;
                }
            }

            return true;
        }

        public static IGraphType BuildNamedType(this Type type, Func<Type, IGraphType> resolve = null)
        {
            resolve ??= t => (IGraphType)Activator.CreateInstance(t);

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
                return Array.Empty<string>();
            }

            if (valueAst == null)
            {
                return Array.Empty<string>();
            }

            // This function only tests literals, and assumes variables will provide
            // values of the correct type.
            if (valueAst is VariableReference)
            {
                return Array.Empty<string>();
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
                    return new[] { $"Expected \"{inputType.Name}\", found not an object." };
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

            return Array.Empty<string>();
        }

        public static string NameOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            var member = (MemberExpression)expression.Body;
            return member.Member.Name;
        }

        public static string DescriptionOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? expr.Member.Description()
                : null;
        }

        public static string DeprecationReasonOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? expr.Member.ObsoleteMessage()
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

        private static readonly NullValue _null = new NullValue();

        public static IValue AstFromValue(this object value, ISchema schema, IGraphType type)
        {
            if (type is NonNullGraphType nonnull)
            {
                return AstFromValue(value, schema, nonnull.ResolvedType);
            }

            if (value == null || type == null)
            {
                return _null;
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

            return serialized switch
            {
                null => null,
                bool b => new BooleanValue(b),
                int i => new IntValue(i),
                BigInteger bi => new BigIntValue(bi),
                long l => new LongValue(l),
                decimal @decimal => new DecimalValue(@decimal),
                double d => new FloatValue(d),
                DateTime time => new DateTimeValue(time),
                Uri uri => new UriValue(uri),
                DateTimeOffset offset => new DateTimeOffsetValue(offset),
                TimeSpan span => new TimeSpanValue(span),
                Guid guid => new GuidValue(guid),
                sbyte @sbyte => new SByteValue(@sbyte),
                byte @byte => new ByteValue(@byte),
                short @short => new ShortValue(@short),
                ushort uint16 => new UShortValue(uint16),
                uint uint32 => new UIntValue(uint32),
                ulong uint64 => new ULongValue(uint64),
                string str => type is EnumerationGraphType ? (IValue)new EnumValue(str) : new StringValue(str),
                _ => Convert()
            };

            IValue Convert()
            {
                var converter = schema.FindValueConverter(serialized, type);
                return converter != null
                    ? converter.Convert(serialized, type)
                    : throw new ExecutionError($"Cannot convert value to AST: {serialized}");
            }
        }
    }
}
