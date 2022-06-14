using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using GraphQL.Types;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for working with graph types.
    /// </summary>
    public static class GraphQLExtensions
    {
        /// <summary>
        /// Determines if this graph type is an introspection type.
        /// </summary>
        internal static bool IsIntrospectionType(this IGraphType type) => type?.Name?.StartsWith("__", StringComparison.InvariantCulture) ?? false;

        /// <summary>
        /// Indicates if the graph type is a union, interface or object graph type.
        /// </summary>
        public static bool IsCompositeType(this IGraphType type)
        {
            return type is IObjectGraphType ||
                   type is IInterfaceGraphType ||
                   type is UnionGraphType;
        }

        /// <summary>
        /// Indicates if the graph type is a scalar graph type.
        /// </summary>
        public static bool IsLeafType(this IGraphType type)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            return namedType is ScalarGraphType ||
                   typeof(ScalarGraphType).IsAssignableFrom(namedType2);
        }

        // https://spec.graphql.org/October2021/#sec-Input-and-Output-Types
        /// <summary>
        /// Indicates if the type is an input graph type (scalar or input object).
        /// </summary>
        public static bool IsInputType(this Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();

                if (genericDef == typeof(GraphQLClrInputTypeReference<>))
                    return true;

                if (genericDef == typeof(GraphQLClrOutputTypeReference<>))
                    return false;

                if (genericDef == typeof(NonNullGraphType<>) || genericDef == typeof(ListGraphType<>))
                    return type.GenericTypeArguments[0].IsInputType();
            }

            return typeof(ScalarGraphType).IsAssignableFrom(type) ||
                   typeof(IInputObjectGraphType).IsAssignableFrom(type);
        }

        // https://spec.graphql.org/October2021/#sec-Input-and-Output-Types
        /// <summary>
        /// Indicates if the graph type is an input graph type (scalar or input object).
        /// </summary>
        public static bool IsInputType(this IGraphType type)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            return namedType is ScalarGraphType ||
                   namedType is IInputObjectGraphType ||
                   typeof(ScalarGraphType).IsAssignableFrom(namedType2) ||
                   typeof(IInputObjectGraphType).IsAssignableFrom(namedType2);
        }

        // https://spec.graphql.org/October2021/#sec-Input-and-Output-Types
        /// <summary>
        /// Indicates if the type is an output graph type (scalar, object, interface or union).
        /// </summary>
        public static bool IsOutputType(this Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();

                if (genericDef == typeof(GraphQLClrInputTypeReference<>))
                    return false;

                if (genericDef == typeof(GraphQLClrOutputTypeReference<>))
                    return true;

                if (genericDef == typeof(NonNullGraphType<>) || genericDef == typeof(ListGraphType<>))
                    return type.GenericTypeArguments[0].IsOutputType();
            }

            return typeof(ScalarGraphType).IsAssignableFrom(type) ||
                   typeof(IObjectGraphType).IsAssignableFrom(type) ||
                   typeof(IInterfaceGraphType).IsAssignableFrom(type) ||
                   typeof(UnionGraphType).IsAssignableFrom(type);
        }

        // https://spec.graphql.org/October2021/#sec-Input-and-Output-Types
        /// <summary>
        /// Indicates if the graph type is an output graph type (scalar, object, interface or union).
        /// </summary>
        public static bool IsOutputType(this IGraphType type)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            return namedType is ScalarGraphType ||
                   namedType is IObjectGraphType ||
                   namedType is IInterfaceGraphType ||
                   namedType is UnionGraphType ||
                   typeof(ScalarGraphType).IsAssignableFrom(namedType2) ||
                   typeof(IObjectGraphType).IsAssignableFrom(namedType2) ||
                   typeof(IInterfaceGraphType).IsAssignableFrom(namedType2) ||
                   typeof(UnionGraphType).IsAssignableFrom(namedType2);
            ;
        }

        /// <summary>
        /// Indicates if the graph type is an input object graph type.
        /// </summary>
        public static bool IsInputObjectType(this IGraphType type)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            return namedType is IInputObjectGraphType ||
                   typeof(IInputObjectGraphType).IsAssignableFrom(namedType2);
        }

        internal static bool IsGraphQLTypeReference(this IGraphType? type)
        {
            var (namedType, _) = type.GetNamedTypes();
            return namedType is GraphQLTypeReference;
        }

        internal static (IGraphType? resolvedType, Type? type) GetNamedTypes(this IGraphType? type)
        {
            return type switch
            {
                NonNullGraphType nonNull => nonNull.ResolvedType != null ? GetNamedTypes(nonNull.ResolvedType) : (null, GetNamedType(nonNull.Type!)),
                ListGraphType list => list.ResolvedType != null ? GetNamedTypes(list.ResolvedType) : (null, GetNamedType(list.Type!)),
                _ => (type, null)
            };
        }

        /// <summary>
        /// Unwraps any list/non-null graph type wrappers from a graph type and returns the base graph type.
        /// </summary>
        public static IGraphType GetNamedType(this IGraphType type)
        {
            if (type == null)
                return null!;

            var (namedType, _) = type.GetNamedTypes();
            return namedType ?? throw new NotSupportedException("Please set ResolvedType property before calling this method or call GetNamedType(this Type type) instead");
        }

        /// <summary>
        /// Unwraps any list/non-null graph type wrappers from a graph type and returns the base graph type.
        /// </summary>
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
        /// <param name="iface">The interface graph type.</param>
        /// <param name="type">The object graph type to verify it against.</param>
        /// <param name="throwError"> Set to <c>true</c> to generate an error if the type does not match the interface. </param>
        public static bool IsValidInterfaceFor(this IInterfaceGraphType iface, IObjectGraphType type, bool throwError = true)
        {
            foreach (var field in iface.Fields)
            {
                var found = type.GetField(field.Name);

                if (found == null)
                {
                    return throwError ? throw new ArgumentException($"Type {type.GetType().GetFriendlyName()} with name '{type.Name}' does not implement interface {iface.GetType().GetFriendlyName()} with name '{iface.Name}'. It has no field '{field.Name}'.") : false;
                }

                if (found.ResolvedType != null && field.ResolvedType != null && found.ResolvedType is not GraphQLTypeReference && field.ResolvedType is not GraphQLTypeReference)
                {
                    if (!IsSubtypeOf(found.ResolvedType, field.ResolvedType))
                        return throwError ? throw new ArgumentException($"Type {type.GetType().GetFriendlyName()} with name '{type.Name}' does not implement interface {iface.GetType().GetFriendlyName()} with name '{iface.Name}'. Field '{field.Name}' must be of type '{field.ResolvedType}' or covariant from it, but in fact it is of type '{found.ResolvedType}'.") : false;
                }
            }

            return true;
        }

        /// <summary>
        /// Examines a simple lambda expression and returns the name of the member it references.
        /// For instance, returns <c>Widget</c> given an expression of <c>x => x.Widget</c>.
        /// Unable to parse any expressions that are more complex than a simple member access.
        /// Throws an <see cref="InvalidCastException"/> if the expression is not a simple member access.
        /// </summary>
        public static string NameOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            var member = (MemberExpression)expression.Body;
            return member.Member.Name;
        }

        /// <summary>
        /// Examines a simple lambda expression and returns the description of the member it
        /// references as listed by a <see cref="DescriptionAttribute"/>.
        /// Unable to parse any expressions that are more complex than a simple member access.
        /// Returns <see langword="null"/> if the expression is not a simple member access.
        /// </summary>
        public static string? DescriptionOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? expr.Member.Description()
                : null;
        }

        /// <summary>
        /// Examines a simple lambda expression and returns the deprecation reason of the member it
        /// references as listed by a <see cref="ObsoleteAttribute"/>.
        /// Unable to parse any expressions that are more complex than a simple member access.
        /// Returns <see langword="null"/> if the expression is not a simple member access.
        /// </summary>
        public static string? DeprecationReasonOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? expr.Member.ObsoleteMessage()
                : null;
        }

        /// <summary>
        /// Examines a simple lambda expression and returns the default value of the member it
        /// references as listed by a <see cref="DefaultValueAttribute"/>.
        /// Unable to parse any expressions that are more complex than a simple member access.
        /// Returns <see langword="null"/> if the expression is not a simple member access.
        /// </summary>
        public static object? DefaultValueOf<TSourceType, TProperty>(this Expression<Func<TSourceType, TProperty>> expression)
        {
            return expression.Body is MemberExpression expr
                ? expr.Member.DefaultValue()
                : null;
        }

        /// <summary>
        /// Adds a key-value metadata pair to the specified provider.
        /// </summary>
        /// <typeparam name="TMetadataProvider"> The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining. </typeparam>
        /// <param name="provider"> Metadata provider which must implement <see cref="IProvideMetadata"/> interface. </param>
        /// <param name="key"> String key. </param>
        /// <param name="value"> Arbitrary value. </param>
        /// <returns> The reference to the specified <paramref name="provider"/>. </returns>
        public static TMetadataProvider WithMetadata<TMetadataProvider>(this TMetadataProvider provider, string key, object? value)
            where TMetadataProvider : IProvideMetadata
        {
            provider.Metadata[key] = value;
            return provider;
        }

        /// <summary>
        /// Provided a type and a super type, return <see langword="true"/> if the first type is either
        /// equal or a subset of the second super type (covariant).
        /// </summary>
        public static bool IsSubtypeOf(this IGraphType maybeSubType, IGraphType superType)
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
                    return IsSubtypeOf(sub.ResolvedType!, sup1.ResolvedType!);
                }

                return false;
            }
            else if (maybeSubType is NonNullGraphType sub)
            {
                return IsSubtypeOf(sub.ResolvedType!, superType);
            }

            // If superType type is a list, maybeSubType type must also be a list.
            if (superType is ListGraphType sup)
            {
                if (maybeSubType is ListGraphType sub)
                {
                    return IsSubtypeOf(sub.ResolvedType!, sup.ResolvedType!);
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
            if (superType is IAbstractGraphType type && maybeSubType is IObjectGraphType)
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
        public static bool DoTypesOverlap(IGraphType typeA, IGraphType typeB)
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
                    // DO NOT USE LINQ ON HOT PATH
                    foreach (var type in a.PossibleTypes.List)
                    {
                        if (b.IsPossibleType(type))
                            return true;
                    }

                    return false;
                }

                return a.IsPossibleType(typeB);
            }

            if (b != null)
            {
                return b.IsPossibleType(typeA);
            }

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the provided value is a valid default value
        /// for the specified input graph type.
        /// </summary>
        public static bool IsValidDefault(this IGraphType type, object? value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type is NonNullGraphType nonNullGraphType)
            {
                return value != null && nonNullGraphType.ResolvedType!.IsValidDefault(value);
            }

            if (value == null)
            {
                return true;
            }

            // Convert IEnumerable to GraphQL list. If the GraphQLType is a list, but
            // the value is not an IEnumerable, convert the value using the list's item type.
            if (type is ListGraphType listType)
            {
                var itemType = listType.ResolvedType!;

                if (value is not string && value is IEnumerable list)
                {
                    foreach (var item in list)
                    {
                        if (!IsValidDefault(itemType, item))
                            return false;
                    }
                    return true;
                }
                else
                {
                    return IsValidDefault(itemType, value);
                }
            }

            if (type is IInputObjectGraphType inputObjectGraphType)
            {
                return inputObjectGraphType.IsValidDefault(value);
            }

            if (type is ScalarGraphType scalar)
                return scalar.IsValidDefault(value);

            throw new ArgumentOutOfRangeException(nameof(type), $"Must provide Input Type, cannot use {type.GetType().Name} '{type}'");
        }

        /// <summary>
        /// Attempts to serialize a value into an AST representation for a specified graph type.
        /// May throw exceptions during the serialization process.
        /// </summary>
        public static GraphQLValue ToAST(this IGraphType type, object? value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type is NonNullGraphType nonnull)
            {
                var astValue = ToAST(nonnull.ResolvedType!, value);

                if (astValue is GraphQLNullValue)
                    throw new InvalidOperationException($"Unable to get an AST representation of {(value == null ? "null" : $"'{value}'")} value for type '{nonnull}'.");

                return astValue;
            }

            if (type is ScalarGraphType scalar)
            {
                return scalar.ToAST(value) ?? scalar.ThrowASTConversionError(value);
            }

            if (value == null)
            {
                return GraphQLValuesCache.Null;
            }

            // Convert IEnumerable to GraphQL list. If the GraphQLType is a list, but
            // the value is not an IEnumerable, convert the value using the list's item type.
            if (type is ListGraphType listType)
            {
                var itemType = listType.ResolvedType!;

                if (value is not string && value is IEnumerable list)
                {
                    var values = list
                        .Cast<object>()
                        .Select(item => ToAST(itemType, item))
                        .ToList();

                    return new GraphQLListValue { Values = values };
                }

                return ToAST(itemType, value);
            }

            // Populate the fields of the input object by creating ASTs from each value
            // in the dictionary according to the fields in the input type.
            if (type is IInputObjectGraphType input)
            {
                return input.ToAST(value) ?? throw new InvalidOperationException($"Unable to get an AST representation of the input object type '{input.Name}' for '{value}'.");
            }

            throw new ArgumentOutOfRangeException(nameof(type), $"Must provide Input Type, cannot use {type.GetType().Name} '{type}'");
        }

        internal static string? MergeComments(this ASTNode node)
        {
            string? merged = null;
            if (node.Comments?.Count > 0)
            {
                //TODO: rewrite this when migrating from comments to descriptions
                for (int i = 0; i < node.Comments!.Count; ++i)
                {
                    merged += node.Comments[i].Value;
                    if (i < node.Comments.Count - 1)
                        merged += Environment.NewLine;
                }
            }
            return merged;
        }

        // optimized version of Print(this ASTNode node) for primitive values
        internal static string Print(this IGraphType type, object value)
        {
            return (type, value) switch
            {
                (StringGraphType, string s) when s.IndexOfAny(_escapes) == -1 => "\"" + s + "\"",
                (BooleanGraphType, false) => "false",
                (BooleanGraphType, true) => "true",
                _ => type.ToAST(value).Print(),
            };
        }

        private static readonly char[] _escapes = new char[] { '\b', '\f', '\n', '\r', '\t', '\\', '"' };

        /// <summary>
        /// Returns a string representation of the specified node.
        /// </summary>
        internal static string Print(this ASTNode node)
        {
            using var writer = new StringWriter();
            _sdlPrinter.PrintAsync(node, writer).GetAwaiter().GetResult(); // actually is sync
            return writer.ToString()!;
        }

        private static readonly SDLPrinter _sdlPrinter = new();

        internal static object? ParseAnyLiteral(this GraphQLValue value) => value switch
        {
            GraphQLObjectValue v => ParseObject(v),
            GraphQLListValue v => ParseList(v),
            GraphQLIntValue v => ParseInt(v),
            GraphQLFloatValue v => ParseFloat(v),
            GraphQLStringValue v => v.Value.Length == 0 ? string.Empty : (string)v.Value,
            GraphQLBooleanValue v => (v.Value == "true").Boxed(),
            GraphQLEnumValue e => e.Name.StringValue, //TODO:think about it later if/when refactoring federation
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        private static readonly ReadOnlyDictionary<string, object?> _empty = new(new Dictionary<string, object?>());

        private static IDictionary<string, object?> ParseObject(GraphQLObjectValue v)
        {
            if (v.Fields == null || v.Fields.Count == 0)
            {
                return _empty;
            }
            else
            {
                var @object = new Dictionary<string, object?>(v.Fields.Count);
                foreach (var field in v.Fields)
                    @object.Add(field.Name.StringValue, ParseAnyLiteral(field.Value));
                return @object;
            }
        }

        private static IList<object?> ParseList(GraphQLListValue v)
        {
            if (v.Values == null || v.Values.Count == 0)
            {
                return Array.Empty<object?>();
            }
            else
            {
                var array = new object?[v.Values.Count];
                for (int i = 0; i < v.Values.Count; ++i)
                    array[i] = ParseAnyLiteral(v.Values[i]);
                return array;
            }
        }

        private static object ParseInt(GraphQLIntValue v)
        {
            if (v.Value.Length == 0)
                throw new InvalidOperationException("Invalid number (empty string)");

            if (Int.TryParse(v.Value, out int intResult))
            {
                return intResult;
            }

            // If the value doesn't fit in an integer, revert to using long...
            if (Long.TryParse(v.Value, out long longResult))
            {
                return longResult;
            }

            // If the value doesn't fit in an long, revert to using decimal...
            if (Decimal.TryParse(v.Value, out decimal decimalResult))
            {
                return decimalResult;
            }

            // If the value doesn't fit in an decimal, revert to using BigInteger...
            if (BigInt.TryParse(v.Value, out var bigIntegerResult))
            {
                return bigIntegerResult;
            }

            // Since BigInteger can contain any valid integer (arbitrarily large), this is impossible to trigger via an invalid query
            throw new InvalidOperationException($"Invalid number {v.Value}");
        }

        private static object ParseFloat(GraphQLFloatValue v)
        {
            if (v.Value.Length == 0)
                throw new InvalidOperationException("Invalid number (empty string)");

            // the idea is to see if there is a loss of accuracy of value
            // for example, 12.1 or 12.11 is double but 12.10 is decimal
            if (!Double.TryParse(v.Value, out double dbl))
            {
                ThrowLiteralConversionError(v);
            }

            //it is possible for a FloatValue to overflow a decimal; however, with a double, it just returns Infinity or -Infinity
            if (Decimal.TryParse(v.Value, out decimal dec))
            {
                // Cast the decimal to our struct to avoid the decimal.GetBits allocations.
                var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dec);
                decimal temp = new(dbl);
                var dblAsDecBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref temp);
                if (!decBits.Equals(dblAsDecBits))
                    return dec;
            }

            return dbl;
        }

        private static object ThrowLiteralConversionError(GraphQLValue input, string? description = null)
        {
            var value = input is IHasValueNode node ? node.Value.ToString() : input?.ToString();
            if (description != null)
                description = "; " + description;
            throw new InvalidOperationException($"Unable to convert '{value}' literal from AST representation to any possible type{description}");
        }
    }
}
