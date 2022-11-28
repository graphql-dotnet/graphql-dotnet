using System.Collections;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Extensions;

public class GraphQLExtensionsTests
{
    public class IsValidDefaultTestData : IEnumerable<object[]>
    {
        public class Person
        {
            public int Age { get; set; }

            public string Name { get; set; }
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new BooleanGraphType(), true, true };
            yield return new object[] { new BooleanGraphType(), false, true };
            yield return new object[] { new BooleanGraphType(), null, true };

            yield return new object[] { new NonNullGraphType(new BooleanGraphType()), false, true };
            yield return new object[] { new NonNullGraphType(new BooleanGraphType()), null, false };

            yield return new object[] { new ListGraphType(new BooleanGraphType()), null, true };
            yield return new object[] { new ListGraphType(new BooleanGraphType()), new object[] { true, false, null }, true };
            yield return new object[] { new NonNullGraphType(new ListGraphType(new BooleanGraphType())), null, false };
            yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, null }, false };
            yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, true }, true };

            yield return new object[] { new InputObjectGraphType<Person>(), null, true };
            yield return new object[] { new NonNullGraphType(new InputObjectGraphType<Person>()), null, false };
            yield return new object[] { new InputObjectGraphType<Person>(), new Person(), true };
            yield return new object[] { new InputObjectGraphType<Person>(), "aaa", false };

            // https://github.com/graphql-dotnet/graphql-dotnet/issues/2334
            yield return new object[] { new ListGraphType(new BooleanGraphType()), true, true };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class IsValidDefaultExceptionTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { null, 0, new ArgumentNullException("type") };
            yield return new object[] { new ObjectGraphType(), 0, new ArgumentOutOfRangeException("type", "Must provide Input Type, cannot use ObjectGraphType 'Object'") };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ToASTTestData : IEnumerable<object[]>
    {
        public class Person
        {
            public int Age { get; set; }

            public string Name { get; set; }
        }

        public class PersonInputType : InputObjectGraphType<Person>
        {
            public PersonInputType()
            {
                // ResolvedType should be set (it usually happens during schema initialization)
                Field(p => p.Name).FieldType.ResolvedType = new StringGraphType();
                Field(p => p.Age).FieldType.ResolvedType = new NonNullGraphType(new IntGraphType());
            }
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new BooleanGraphType(), true, new GraphQLTrueBooleanValue() };
            yield return new object[] { new BooleanGraphType(), false, new GraphQLFalseBooleanValue() };
            yield return new object[] { new BooleanGraphType(), null, new GraphQLNullValue() };

            yield return new object[] { new NonNullGraphType(new BooleanGraphType()), false, new GraphQLFalseBooleanValue() };

            yield return new object[] { new ListGraphType(new BooleanGraphType()), null, new GraphQLNullValue() };
            yield return new object[] { new ListGraphType(new BooleanGraphType()), new object[] { true, false, null }, new GraphQLListValue { Values = new List<GraphQLValue> { new GraphQLTrueBooleanValue(), new GraphQLFalseBooleanValue(), new GraphQLNullValue() } } };
            yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, true }, new GraphQLListValue { Values = new List<GraphQLValue> { new GraphQLTrueBooleanValue(), new GraphQLFalseBooleanValue(), new GraphQLTrueBooleanValue() } } };

            yield return new object[] { new InputObjectGraphType<Person>(), null, new GraphQLNullValue() };
            yield return new object[] { new PersonInputType(), new Person { Name = "Tom", Age = 42 }, new GraphQLObjectValue
            {
                Fields = new List<GraphQLObjectField>
                {
                    new GraphQLObjectField { Name = new GraphQLName("Name"), Value = new GraphQLStringValue("Tom") },
                    new GraphQLObjectField { Name = new GraphQLName("Age"), Value = new GraphQLIntValue(42) }
                }
            } };
            yield return new object[] { new PersonInputType(), new Person { }, new GraphQLObjectValue
            {
                Fields = new List<GraphQLObjectField>
                {
                    new GraphQLObjectField { Name = new GraphQLName("Age"), Value = new GraphQLIntValue(0) }
                }
            } };

            yield return new object[] { new ListGraphType(new BooleanGraphType()), true, new GraphQLTrueBooleanValue() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ToASTExceptionTestData : IEnumerable<object[]>
    {
        public class Person
        {
            public int Age { get; set; }

            public string Name { get; set; }
        }

        public class BadPersonInputType : InputObjectGraphType<Person>
        {
            public override GraphQLValue ToAST(object value) => null;
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new ObjectGraphType(), 0, new ArgumentOutOfRangeException("type", "Must provide Input Type, cannot use ObjectGraphType 'Object'") };
            yield return new object[] { new BadPersonInputType(), new Person(), new InvalidOperationException("Unable to get an AST representation of the input object type 'BadPersonInputType' for 'GraphQL.Tests.Extensions.GraphQLExtensionsTests+ToASTExceptionTestData+Person'.") };
            yield return new object[] { new NonNullGraphType(new BooleanGraphType()), null, new InvalidOperationException($"Unable to get an AST representation of null value for type 'Boolean!'.") };
            yield return new object[] { new NonNullGraphType(new ListGraphType(new BooleanGraphType())), null, new InvalidOperationException($"Unable to get an AST representation of null value for type '[Boolean]!'.") };
            yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, null }, new InvalidOperationException($"Unable to get an AST representation of null value for type 'Boolean!'.") };
            yield return new object[] { new NonNullGraphType(new InputObjectGraphType<Person>()), null, new InvalidOperationException($"Unable to get an AST representation of null value for type 'InputObjectGraphType_1!'.") };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(IsValidDefaultTestData))]
    public void IsValidDefault_Test(IGraphType type, object value, bool expected) => type.IsValidDefault(value).ShouldBe(expected);

    [Theory]
    [ClassData(typeof(IsValidDefaultExceptionTestData))]
    public void IsValidDefault_Exception_Test(IGraphType type, object value, Exception expected)
    {
        Should.Throw(() => type.IsValidDefault(value), expected.GetType()).Message.ShouldBe(expected.Message);
    }

    [Theory]
    [ClassData(typeof(ToASTTestData))]
    public void ToAST_Test(IGraphType type, object value, GraphQLValue expected)
    {
        var actual = type.ToAST(value).Print();
        var result = expected.Print();
        actual.ShouldBe(result);
    }

    [Theory]
    [ClassData(typeof(ToASTExceptionTestData))]
    public void ToAST_Exception_Test(IGraphType type, object value, Exception expected)
    {
        Should.Throw(() => type.ToAST(value), expected.GetType()).Message.ShouldBe(expected.Message);
    }

    private class TestSchemaTypes : SchemaTypes
    {
    }

    [Fact]
    public void BuildGraphQLType_ResolveReturnNull_Throws()
    {
        var types = new TestSchemaTypes();
        Should.Throw<InvalidOperationException>(() => types.BuildGraphQLType(typeof(ListGraphType<ListGraphType<EpisodeEnum>>), _ => null));
    }
}
