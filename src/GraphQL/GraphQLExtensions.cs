using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using GraphQL.Language;
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

        public static bool IsLeafType(this GraphType type, ISchema schema)
        {
            var namedType = type.GetNamedType(schema);
            return namedType is ScalarGraphType || namedType is EnumerationGraphType;
        }

        public static GraphType GetNamedType(this GraphType type, ISchema schema)
        {
            GraphType unmodifiedType = type;

            if (type is NonNullGraphType)
            {
                var nonNull = (NonNullGraphType) type;
                return GetNamedType(schema.FindType(nonNull.Type), schema);
            }

            if (type is ListGraphType)
            {
                var list = (ListGraphType) type;
                return GetNamedType(schema.FindType(list.Type), schema);
            }

            return unmodifiedType;
        }

        public static Type GetNamedType(this Type type)
        {
            if (type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>) ||
                    type.GetGenericTypeDefinition() == typeof(ListGraphType<>)))
            {
                return GetNamedType(type.GetGenericArguments()[0]);
            }

            return type;
        }

        public static IEnumerable<string> IsValidLiteralValue(this GraphType type, IValue valueAst, ISchema schema)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = (NonNullGraphType) type;
                var ofType = schema.FindType(nonNull.Type);

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
                var ofType = schema.FindType(list.Type);

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
                    var result = IsValidLiteralValue(schema.FindType(field.Type), fieldAst?.Value, schema);

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

        public static bool IsSubtypeOf(this GraphType maybeSubType, GraphType superType, ISchema schema)
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
                    return IsSubtypeOf(schema.FindType(sub.Type), schema.FindType(sup.Type), schema);
                }

                return false;
            }
            else if (maybeSubType is NonNullGraphType)
            {
                var sub = (NonNullGraphType) maybeSubType;
                return IsSubtypeOf(schema.FindType(sub.Type), superType, schema);
            }

            // If superType type is a list, maybeSubType type must also be a list.
            if (superType is ListGraphType)
            {
                if (maybeSubType is ListGraphType)
                {
                    var sub = (ListGraphType) maybeSubType;
                    var sup = (ListGraphType) superType;
                    return IsSubtypeOf(schema.FindType(sub.Type), schema.FindType(sup.Type), schema);
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
            if (superType is GraphQLAbstractType &&
                maybeSubType is ObjectGraphType)
            {
                return ((GraphQLAbstractType) superType).IsPossibleType(maybeSubType);
            }

            return false;
        }
    }
}
