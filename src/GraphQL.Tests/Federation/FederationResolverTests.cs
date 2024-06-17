using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Federation;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser;
using Moq;

namespace GraphQL.Tests.Federation;

public class FederationResolverTests
{
    [Fact]
    public void CodeFirst_ResolveReference1()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        var mockResolver = new Mock<IFederationResolver>(MockBehavior.Strict);
        mockResolver.Setup(x => x.MatchKeys(It.IsAny<IDictionary<string, object?>>()))
            .Returns<IDictionary<string, object?>>(args => args.ContainsKey("id2"));
        mockResolver.Setup(x => x.ParseRepresentation(objectGraphType, It.IsAny<IDictionary<string, object?>>()))
            .Returns<IObjectGraphType, IDictionary<string, object?>>((_, args) => args["id2"].ShouldBeOfType<int>());
        mockResolver.Setup(x => x.ResolveAsync(It.IsAny<IResolveFieldContext>(), objectGraphType, 1))
            .Returns(new ValueTask<object?>(new Class1 { Id = 11 }));
        objectGraphType.ResolveReference(mockResolver.Object);

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "id2": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference2()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference((ctx, obj) => new Class1() { Id = obj.Id + 10 });

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference3()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field(x => x.Id, type: typeof(IntGraphType)).FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference((ctx, obj) => new SimpleDataLoader<Class1?>(_ => Task.FromResult<Class1?>(new Class1() { Id = obj.Id + 10 })));

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference4()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference((ctx, obj) => Task.FromResult<Class1?>(new Class1() { Id = obj.Id + 10 }));

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference5()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference<Class2, Class1>((ctx, obj) => new Class1() { Id = obj.ShouldBeOfType<Class2>().Id + 10 });

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference6()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference<Class2, Class1>((ctx, obj) => new SimpleDataLoader<Class1?>(_ => Task.FromResult<Class1?>(new Class1() { Id = obj.ShouldBeOfType<Class2>().Id + 10 })));

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference7()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference<Class2, Class1>((ctx, obj) => Task.FromResult<Class1?>(new Class1() { Id = obj.ShouldBeOfType<Class2>().Id + 10 }));

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference1()
    {
        var mockResolver = new Mock<IFederationResolver>(MockBehavior.Strict);
        mockResolver.Setup(x => x.MatchKeys(It.IsAny<IDictionary<string, object?>>()))
            .Returns<IDictionary<string, object?>>(args => args.ContainsKey("id2"));
        mockResolver.Setup(x => x.ParseRepresentation(It.IsAny<IObjectGraphType>(), It.IsAny<IDictionary<string, object?>>()))
            .Returns<IObjectGraphType, IDictionary<string, object?>>((_, args) => args["id2"].ShouldBeOfType<int>());
        mockResolver.Setup(x => x.ResolveAsync(It.IsAny<IResolveFieldContext>(), It.IsAny<IObjectGraphType>(), 1))
            .Returns(new ValueTask<object?>(new Class1 { Id = 11 }));

        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference(mockResolver.Object));
        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id2": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference2()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class1>((ctx, obj) => new Class1() { Id = obj.Id + 10 }));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference3()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class1>((ctx, obj) => new SimpleDataLoader<Class1?>(_ => Task.FromResult<Class1?>(new Class1() { Id = obj.Id + 10 }))));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference4()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class1>((ctx, obj) => Task.FromResult<Class1?>(new Class1() { Id = obj.Id + 10 })));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference5()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class2, Class1>((ctx, obj) => new Class1() { Id = obj.ShouldBeOfType<Class2>().Id + 10 }));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference6()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class2, Class1>((ctx, obj) => new SimpleDataLoader<Class1?>(_ => Task.FromResult<Class1?>(new Class1() { Id = obj.ShouldBeOfType<Class2>().Id + 10 }))));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference7()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class2, Class1>((ctx, obj) => Task.FromResult<Class1?>(new Class1() { Id = obj.ShouldBeOfType<Class2>().Id + 10 })));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void TypeFirst_ResolveReference1()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest1>();

        var ret = TestResolver<TypeFirstTest1>(objectGraphType, """{ "__typename": "TypeFirstTest1", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    private class TypeFirstTest1
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        [FederationResolver]
        public static TypeFirstTest1 Resolve(int id) => new() { Id = id + 10 };
    }

    [Fact]
    public void TypeFirst_ResolveReference2()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest2>();

        var ret = TestResolver<TypeFirstTest2>(objectGraphType, """{ "__typename": "TypeFirstTest2", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    private class TypeFirstTest2
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        [FederationResolver]
        public static Task<TypeFirstTest2> Resolve(int id) => Task.FromResult(new TypeFirstTest2() { Id = id + 10 });
    }

    [Fact]
    public void TypeFirst_ResolveReference3()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest3>();

        var ret = TestResolver<TypeFirstTest3>(objectGraphType, """{ "__typename": "TypeFirstTest3", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    private class TypeFirstTest3
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        [FederationResolver]
        public static IDataLoaderResult<TypeFirstTest3> Resolve(int id) => new SimpleDataLoader<TypeFirstTest3>(_ => Task.FromResult(new TypeFirstTest3() { Id = id + 10 }));
    }

    [Fact]
    public void TypeFirst_ResolveReference4()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest4>();

        var ret = TestResolver<TypeFirstTest4>(objectGraphType, """{ "__typename": "TypeFirstTest4", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void TypeFirst_ResolveReference5()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest4>();

        var ret = TestResolver<TypeFirstTest4>(objectGraphType, """{ "__typename": "TypeFirstTest4", "id2": 1 }""");
        ret.Id.ShouldBe(21);
    }

    private class TypeFirstTest4
    {
        public int Id { get; set; }

        public int Id2 { get; set; }

        public string? Name { get; set; }

        [FederationResolver]
        public static TypeFirstTest4 Resolve1(int id) => new() { Id = id + 10 };

        [FederationResolver]
        public static TypeFirstTest4 Resolve2(int id2) => new() { Id = id2 + 20 };
    }

    [Fact]
    public void TypeFirst_ResolveReference6()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest6>();

        var ret = TestResolver<TypeFirstTest6>(objectGraphType, """{ "__typename": "TypeFirstTest6", "id": 1 }""");
        ret.Id.ShouldBe(11);
    }

    private class TypeFirstTest6
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        [FederationResolver]
        public TypeFirstTest6 Resolve() => new() { Id = Id + 10 };
    }

    private static IObjectGraphType TypeFirstSetup<T>()
    {
        var objectGraphType = new AutoRegisteringObjectGraphType<T>();
        var schema = new Schema { Query = objectGraphType };
        schema.RegisterTypeMapping<T, AutoRegisteringObjectGraphType<T>>();
        schema.Initialize();
        return objectGraphType;
    }

    private static IObjectGraphType SchemaFirstSetup(Action<TypeConfig> class1Config)
    {
        var schema = Schema.For(
            """
            type Query {
                dummy: String
            }

            type Class1 {
                id: Int!
                name: String
            }
            """,
            builder => class1Config(builder.Types.For("Class1")));
        return (IObjectGraphType)schema.AllTypes["Class1"]!;
    }

    private static T TestResolver<T>(ObjectGraphType<T> objectGraphType, string representationJson)
        => TestResolver<T>((IObjectGraphType)objectGraphType, representationJson);

    private static T TestResolver<T>(IObjectGraphType objectGraphType, string representationJson)
    {
        // mock calling _entities for a single representation on a schema with a single configured type
        var data = new SystemTextJson.GraphQLSerializer().Deserialize<List<Inputs>>("[" + representationJson + "]")!;
        var schemaTypes = new MySchemaTypes([objectGraphType]);
        var mockSchema = new Mock<ISchema>(MockBehavior.Strict);
        mockSchema.Setup(x => x.AllTypes).Returns(schemaTypes);

        // first simulate parsing
        var parsedData = EntityResolver.Instance.ConvertRepresentations(mockSchema.Object, data);
        var context = new ResolveFieldContext()
        {
            Arguments = new Dictionary<string, ArgumentValue>()
            {
                { FederationHelper.REPRESENTATIONS_ARGUMENT, new(parsedData, ArgumentSource.Literal) }
            }
        };

        // then simulate resolving
        var ret = EntityResolver.Instance.ResolveAsync(context).AsTask().GetAwaiter().GetResult()
            .ShouldBeAssignableTo<IEnumerable<object>>().ShouldHaveSingleItem();
        while (ret is IDataLoaderResult dataLoaderResult)
        {
            ret = dataLoaderResult.GetResultAsync().GetAwaiter().GetResult();
        }
        return ret.ShouldNotBeNull().ShouldBeAssignableTo<T>()!;
    }

    private class MySchemaTypes : SchemaTypes
    {
        private readonly Dictionary<ROM, IGraphType> _types = new();

        public MySchemaTypes(IEnumerable<IGraphType> types)
        {
            foreach (var type in types)
            {
                _types.Add(type.Name, type);
            }
        }

        protected internal override Dictionary<ROM, IGraphType> Dictionary => _types;
    }

    private IFederationResolver GetResolver(IGraphType objectGraphType)
    {
        return GetResolvers(objectGraphType).ShouldHaveSingleItem();
    }

    private IEnumerable<IFederationResolver> GetResolvers(IGraphType objectGraphType)
    {
        var ret = objectGraphType.GetMetadata<object>(FederationHelper.RESOLVER_METADATA);
        if (ret is IFederationResolver resolver)
            return [resolver];
        else if (ret is IEnumerable<IFederationResolver> resolvers)
            return resolvers;
        else
            throw new InvalidOperationException("Could not find resolver");
    }

    private class Class2
    {
        public int Id { get; set; }
    }

    private class Class1
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
