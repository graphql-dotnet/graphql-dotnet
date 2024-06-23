using GraphQL.Federation;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;

namespace GraphQL.Tests.Federation;

public class EntityResolverTests
{
    private readonly EntityResolver _resolver = EntityResolver.Instance;

    [Fact]
    public void ConvertRepresentations_Should_Throw_ArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _resolver.ConvertRepresentations(null!, Array.Empty<object>()));
        var schema = Schema.For("type Query { hello: String }");
        Should.Throw<ArgumentNullException>(() => _resolver.ConvertRepresentations(schema, null!));
    }

    [Fact]
    public void ConvertRepresentations_Should_Throw_InvalidOperationException()
    {
        var schema = Schema.For("type Query { hello: String }");
        var representations = new List<object> { new { __typename = "TestObject" } };
        Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
    }

    [Fact]
    public void ConvertRepresentations_SimpleDictionary()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" } } };
        var result = _resolver.ConvertRepresentations(schema, representations);
        var ret = result.ShouldHaveSingleItem();
        var obj = ret.Value.ShouldBeOfType<TestObject>();
        obj.Id.ShouldBe("1");
    }

    [Fact]
    public void ConvertRepresentations_ValueConverterWorks()
    {
        var schema = CreateSchema<TestSimpleObject2>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" } } };
        var result = _resolver.ConvertRepresentations(schema, representations);
        var ret = result.ShouldHaveSingleItem();
        var obj = ret.Value.ShouldBeOfType<TestSimpleObject2>();
        obj.Id.ShouldBe(1);
    }

    [Fact]
    public void ConvertRepresentations_MultipleDictionary()
    {
        var schema = CreateSchema<TestObject>("id name age");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "name", "Test" }, { "age", 42 } } };
        var result = _resolver.ConvertRepresentations(schema, representations);
        var ret = result.ShouldHaveSingleItem();
        var obj = ret.Value.ShouldBeOfType<TestObject>();
        obj.Id.ShouldBe("1");
        obj.Name.ShouldBe("Test");
        obj.Age.ShouldBe(42);
    }

    [Fact]
    public void ConvertRepresentations_NestedDictionary()
    {
        var schema = CreateSchema<TestObject>("id child { id }");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "child", new Dictionary<string, object>() { { "id", "2" } } } } };
        var result = _resolver.ConvertRepresentations(schema, representations);
        var ret = result.ShouldHaveSingleItem();
        var obj = ret.Value.ShouldBeOfType<TestObject>();
        obj.Id.ShouldBe("1");
        obj.Child.ShouldNotBeNull();
        obj.Child.Id.ShouldBe("2");
    }

    [Fact]
    public void ConvertRepresentations_InvalidNestedDictionary()
    {
        var schema = CreateSchema<TestObject>("id child { id }");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "child", "abc" } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'child' is an object graph type but the value is not a dictionary");
    }

    [Fact]
    public void ConvertRepresentations_Lists()
    {
        var schema = CreateSchema<TestObject>("id numbers");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "numbers", new int[] { 1, 2, 3 } } } };
        var result = _resolver.ConvertRepresentations(schema, representations);
        var ret = result.ShouldHaveSingleItem();
        var obj = ret.Value.ShouldBeOfType<TestObject>();
        obj.Id.ShouldBe("1");
        obj.Numbers.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ConvertRepresentations_InvalidList()
    {
        var schema = CreateSchema<TestObject>("id numbers");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "numbers", 1 } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'numbers' is a list graph type but the value is not a list");
    }

    [Fact]
    public void ConvertRepresentations_NullWhenRequired()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new Dictionary<string, object?>() { { "__typename", "TestObject" }, { "id", null } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The non-null field 'id' has a null value.");
    }

    [Fact]
    public void ConvertRepresentations_NotDictionary()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new { __typename = "TestObject", id = "1" } };
        Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations))
            .Message.ShouldBe("Representation must be a dictionary.");
    }

    [Fact]
    public void ConvertRepresentations_NotResolvable()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestChildObject" }, { "id", "1" } } };
        Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations))
            .Message.ShouldBe("The type 'TestChildObject' has not been configured for GraphQL Federation.");
    }

    [Fact]
    public void ConvertRepresentations_InvalidField()
    {
        var schema = CreateSchema<TestObject>("id name");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "dummy", "abc" } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Field 'dummy' not found in graph type 'TestObject'.");
    }

    [Fact]
    public void ConvertRepresentations_InvalidClrField()
    {
        var schema = CreateSchema<TestSimpleObject>("id name");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", "1" }, { "name", "abc" } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Property 'name' not found in type 'TestSimpleObject'.");
    }

    [Fact]
    public void ConvertRepresentations_CannotFindType()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "DummyType" }, { "id", "1" } } };
        Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations))
            .Message.ShouldBe("The type 'DummyType' could not be found.");
    }

    [Fact]
    public void ConvertRepresentations_NotObjectType()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestInput" }, { "id", "1" } } };
        Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations))
            .Message.ShouldBe("The type 'TestInput' is not an object graph type.");
    }

    [Fact]
    public void ConvertRepresentations_WrongValueType()
    {
        var schema = CreateSchema<TestObject>("name");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "name", 1 } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Unable to convert '1' value of type 'Int32' to the scalar type 'String'");
    }

    [Fact]
    public void ConvertRepresentations_ValueConverterFailure()
    {
        var schema = CreateSchema<TestSimpleObject3>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "__typename", "TestObject" }, { "id", 1 } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Could not find conversion from 'System.Int32' to 'System.Guid'");
    }

    [Fact]
    public void ConvertRepresentations_MissingTypename()
    {
        var schema = CreateSchema<TestObject>("id");
        var representations = new List<object> { new Dictionary<string, object>() { { "id", "1" } } };
        Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations))
            .Message.ShouldBe("Representation must contain a __typename field.");
    }

    [Fact]
    public void ConvertRepresentations_FieldNotObjectOrScalar()
    {
        var schema = CreateSchema<TestObjectWithUnion>("id union");
        var representations = new List<object> { new Dictionary<string, object?>() { { "__typename", "TestObject" }, { "id", "1" }, { "union", new Dictionary<string, object?> { { "id", "2" } } } } };
        var err = Should.Throw<InvalidOperationException>(() => _resolver.ConvertRepresentations(schema, representations));
        err.Message.ShouldBe("Error converting representation for type 'TestObject'. Please verify your supergraph is up to date.");
        err.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'union' is not a scalar or object graph type.");
    }

    private class TestSimpleObject
    {
        public string Id { get; set; }
    }

    private class TestSimpleObject2
    {
        public int Id { get; set; }
    }

    private class TestSimpleObject3
    {
        public Guid Id { get; set; }
    }

    private class TestObject
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public TestChildObject? Child { get; set; }
        public List<int>? Numbers { get; set; }
    }

    private class TestChildObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    private class TestObjectWithUnion
    {
        public string Id { get; set; }
        public TestChildObject Union { get; set; }
    }

    private static ISchema CreateSchema<TObjectType>(string fields)
    {
        var schema = Schema.For(
            $$"""
            type Query {
              test: TestObject
            }

            type TestObject @key(fields: "{{fields}}") {
              id: ID!
              name: String
              age: Int!
              child: TestChildObject
              numbers: [Int]
              union: TestUnion
            }

            type TestChildObject {
              id: ID!
              name: String!
            }

            input TestInput {
              id: ID!
              name: String!
            }

            union TestUnion = TestChildObject

            directive @key(fields: String!, resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            """,
            b =>
            {
                b.Types.For("TestObject").ResolveReference<TObjectType>((ctx, source) => source);
                b.Types.For("TestUnion").ResolveType = _ => new GraphQLTypeReference("TestChildObject");
            });

        return schema;
    }
}
