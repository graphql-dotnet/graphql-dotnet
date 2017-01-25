using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    public static class GraphQLExtensions
    {
        public static string TrimGraphQLTypes(this string name)
        {
            return Regex.Replace(name, "[\\[!\\]]", "").Trim();
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
                   namedType is InputObjectGraphType;
        }

        public static IGraphType GetNamedType(this IGraphType type)
        {
            IGraphType unmodifiedType = type;

            if (type is NonNullGraphType)
            {
                var nonNull = (NonNullGraphType) type;
                return GetNamedType(nonNull.ResolvedType);
            }

            if (type is ListGraphType)
            {
                var list = (ListGraphType) type;
                return GetNamedType(list.ResolvedType);
            }

            return unmodifiedType;
        }

        public static IGraphType BuildNamedType(this Type type, Func<Type, IGraphType> resolve = null)
        {
            if (resolve == null)
            {
                resolve = t => (IGraphType) Activator.CreateInstance(t);
            }

            if (type.GetTypeInfo().IsGenericType)
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

            return resolve(type);
        }

        public static Type GetNamedType(this Type type)
        {
            if (type.GetTypeInfo().IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>) ||
                    type.GetGenericTypeDefinition() == typeof(ListGraphType<>)))
            {
                return GetNamedType(type.GenericTypeArguments[0]);
            }

            return type;
        }

        public static IEnumerable<string> IsValidLiteralValue(this IGraphType type, IValue valueAst, ISchema schema)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = (NonNullGraphType) type;
                var ofType = nonNull.ResolvedType;

                if (valueAst == null)
                {
                    if (ofType != null)
                    {
                        return new[] { $"Expected \"{ofType.Name}!\", found null."};
                    }

                    return new[] { "Expected non-null value, found null"};
                }

                return IsValidLiteralValue(ofType, valueAst, schema);
            }

            if (valueAst == null)
            {
                return new string[] {};
            }

            // This function only tests literals, and assumes variables will provide
            // values of the correct type.
            if (valueAst is VariableReference)
            {
                return new string[] {};
            }

            if (type is ListGraphType)
            {
                var list = (ListGraphType)type;
                var ofType = list.ResolvedType;

                if (valueAst is ListValue)
                {
                    var index = 0;
                    return ((ListValue) valueAst).Values.Aggregate(new string[] {}, (acc, value) =>
                    {
                        var errors = IsValidLiteralValue(ofType, value, schema);
                        var result = acc.Concat(errors.Map(err => $"In element #{index}: {err}")).ToArray();
                        index++;
                        return result;
                    });
                }

                return IsValidLiteralValue(ofType, valueAst, schema);
            }

            if (type is InputObjectGraphType)
            {
                if (!(valueAst is ObjectValue))
                {
                    return new[] {$"Expected \"{type.Name}\", found not an object."};
                }

                var inputType = (InputObjectGraphType) type;

                var fields = inputType.Fields.ToList();
                var fieldAsts = ((ObjectValue) valueAst).ObjectFields.ToList();

                var errors = new List<string>();

                // ensure every provided field is defined
                fieldAsts.Apply(providedFieldAst =>
                {
                    var found = fields.FirstOrDefault(x => x.Name == providedFieldAst.Name);
                    if (found == null)
                    {
                        errors.Add($"In field \"{providedFieldAst.Name}\": Unknown field.");
                    }
                });

                // ensure every defined field is valid
                fields.Apply(field =>
                {
                    var fieldAst = fieldAsts.FirstOrDefault(x => x.Name == field.Name);
                    var result = IsValidLiteralValue(field.ResolvedType, fieldAst?.Value, schema);

                    errors.AddRange(result.Map(err=> $"In field \"{field.Name}\": {err}"));
                });

                return errors;
            }

            var scalar = (ScalarGraphType) type;

            var parseResult = scalar.ParseLiteral(valueAst);

            if (parseResult == null)
            {
                return new [] {$"Expected type \"{type.Name}\", found {AstPrinter.Print(valueAst)}."};
            }

            return new string[] {};
        }

        public static string NameOf<T, P>(this Expression<Func<T, P>> expression)
        {
            var member = (MemberExpression) expression.Body;
            return member.Member.Name;
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
            if (superType is NonNullGraphType)
            {
                if (maybeSubType is NonNullGraphType)
                {
                    var sub = (NonNullGraphType) maybeSubType;
                    var sup = (NonNullGraphType) superType;
                    return IsSubtypeOf(sub.ResolvedType, sup.ResolvedType, schema);
                }

                return false;
            }
            else if (maybeSubType is NonNullGraphType)
            {
                var sub = (NonNullGraphType) maybeSubType;
                return IsSubtypeOf(sub.ResolvedType, superType, schema);
            }

            // If superType type is a list, maybeSubType type must also be a list.
            if (superType is ListGraphType)
            {
                if (maybeSubType is ListGraphType)
                {
                    var sub = (ListGraphType) maybeSubType;
                    var sup = (ListGraphType) superType;
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
            if (superType is IAbstractGraphType &&
                maybeSubType is IObjectGraphType)
            {
                return ((IAbstractGraphType) superType).IsPossibleType(maybeSubType);
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

            var a = typeA as IAbstractGraphType;
            var b = typeB as IAbstractGraphType;

            if (a != null)
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
            if (type is NonNullGraphType)
            {
                var nonnull = (NonNullGraphType)type;
                return AstFromValue(value, schema, nonnull.ResolvedType);
            }

            if (value == null || type == null)
            {
                return null;
            }

            // Convert IEnumerable to GraphQL list. If the GraphQLType is a list, but
            // the value is not an IEnumerable, convert the value using the list's item type.
            if (type is ListGraphType)
            {
                var listType = (ListGraphType) type;
                var itemType = listType.ResolvedType;

                if (!(value is string) && value is IEnumerable)
                {
                    var list = (IEnumerable)value;
                    var values = list.Map(item => AstFromValue(item, schema, itemType));
                    return new ListValue(values);
                }

                return AstFromValue(value, schema, itemType);
            }

            // Populate the fields of the input object by creating ASTs from each value
            // in the dictionary according to the fields in the input type.
            if (type is InputObjectGraphType)
            {
                if (!(value is Dictionary<string, object>))
                {
                    return null;
                }

                var input = (InputObjectGraphType) type;
                var dict = (Dictionary<string, object>)value;

                var fields = dict
                    .Select(pair =>
                    {
                        var field = input.Fields.FirstOrDefault(x => x.Name == pair.Key);
                        var fieldType = field?.ResolvedType;
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

            if (serialized is bool)
            {
                return new BooleanValue((bool)serialized);
            }

            if (serialized is int)
            {
                return new IntValue((int)serialized);
            }

            if (serialized is long)
            {
                return new LongValue((long)serialized);
            }

            if (serialized is decimal)
            {
                return new DecimalValue((decimal)serialized);
            }

            if (serialized is double)
            {
                return new FloatValue((double)serialized);
            }

            if (serialized is DateTime)
            {
                return new DateTimeValue((DateTime)serialized);
            }

            if (serialized is string)
            {
                if (type is EnumerationGraphType)
                {
                    return new EnumValue(serialized.ToString());
                }

                return new StringValue(serialized.ToString());
            }

            throw new ExecutionError($"Cannot convert value to AST: {serialized}");
        }
    }
}
