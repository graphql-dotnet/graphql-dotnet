using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL.Introspection;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug68NonNullEnumGraphTypeTests
    {
        private readonly IDocumentExecuter _executer = new DocumentExecuter();

        ExecutionResult ExecuteQuery(ISchema schema, string query)
        {
            return _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query;
            }).GetAwaiter().GetResult();
        }

        [Fact]
        public void only_nullable_is_happy()
        {
            VerifyIntrospection(new NullableSchema(true, false));
        }

        [Fact]
        public void only_nonnullable_is_happy()
        {
            VerifyIntrospection(new NullableSchema(false, true));
        }

        [Fact]
        public void both_is_happy()
        {
            VerifyIntrospection(new NullableSchema(true, true));
        }

        public void VerifyIntrospection(ISchema schema)
        {
            var result = ExecuteQuery(schema, SchemaIntrospection.IntrospectionQuery);
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
            result.Errors.ShouldBeNull();
        }

        public enum Foo
        {
            Bar,
            Baz
        }

        public class NullableSchema : Schema
        {
            public NullableSchema(bool includeNullable, bool includeNonNullable)
            {
                var query = new ObjectGraphType();
                if (includeNullable)
                    query.Field<NullableSchemaType>("nullable", resolve: c => new NullableSchemaType());
                if (includeNonNullable)
                    query.Field<NonNullableSchemaType>("nonNullable", resolve: c => new NonNullableSchemaType());

                Query = query;
            }
        }

        public class NullableSchemaType : ObjectGraphType
        {
            public NullableSchemaType()
            {
                Field<EnumType<Foo>>("a", resolve: _ => Foo.Bar);
            }
        }

        public class NonNullableSchemaType : ObjectGraphType
        {
            public NonNullableSchemaType()
            {
                Field<NonNullGraphType<EnumType<Foo>>>("a", resolve: _ => Foo.Bar);
            }
        }
    }

    /// <summary>
    /// Copy of EnumType[T] from unreleased repo GraphQL-conventions.
    /// See https://github.com/graphql-dotnet/conventions.
    /// </summary>
    public class EnumType<T> : EnumerationGraphType
      where T : struct
    {
        public EnumType()
        {
            if (!typeof(T).GetTypeInfo().IsEnum)
            {
                throw new ArgumentException($"{typeof(T).Name} must be of type enum");
            }

            var type = typeof(T);
            Name = DeriveGraphQlName(type.Name);

            foreach (var enumName in type.GetTypeInfo().GetEnumNames())
            {
                var enumMember = type
                  .GetMember(enumName, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                  .First();

                var name = DeriveEnumValueName(enumMember.Name);

                AddValue(name, null, Enum.Parse(type, enumName));
            }
        }

        public override object ParseValue(object value)
        {
            var found = Values.FirstOrDefault(
              v =>
                StringComparer.OrdinalIgnoreCase.Equals(PureValue(v.Name), PureValue(value)) ||
                StringComparer.OrdinalIgnoreCase.Equals(PureValue(v.Value.ToString()), PureValue(value)));
            return found?.Name;
        }

        public object GetValue(object value)
        {
            var found =
              Values.FirstOrDefault(
                v => StringComparer.OrdinalIgnoreCase.Equals(PureValue(v.Name), PureValue(value)));
            return found?.Value;
        }

        static string PureValue(object value)
        {
            return value.ToString().Replace("\"", "").Replace("'", "").Replace("_", "");
        }

        static string DeriveGraphQlName(string name)
        {
            return $"{char.ToUpperInvariant(name[0])}{name.Substring(1)}";
        }

        static string DeriveEnumValueName(string name)
        {
            return Regex
              .Replace(name, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4")
              .ToUpperInvariant();
        }
    }
}
