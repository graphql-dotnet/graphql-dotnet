using System.Globalization;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Federation;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;
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
    public void CodeFirst_ResolveReference8()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.Field<IntGraphType>("Id2").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference((ctx, obj) => new Class1() { Id = obj.Id + obj.Id2 });

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1, "Id2": 10 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference9()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<IntGraphType>("Id").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.Field<IntGraphType>("Id2").FieldType.ResolvedType = new IntGraphType();
        objectGraphType.ResolveReference<Class3, Class1>((ctx, obj) => new Class1() { Id = obj.Id + obj.Id2 });

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1, "Id2": 10 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void CodeFirst_ResolveReference10_CustomScalar()
    {
        var objectGraphType = new ObjectGraphType<Class1>() { Name = "Class1" };
        objectGraphType.Field<MyCustomScalar>("Id").FieldType.ResolvedType = new MyCustomScalar();
        objectGraphType.ResolveReference((ctx, obj) => new Class1() { Id = obj.Id + 100 });

        var ret = TestResolver(objectGraphType, """{ "__typename": "Class1", "Id": 1 }""");
        ret.Id.ShouldBe(110);
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
    public void SchemaFirst_ResolveReference8()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class1>((ctx, obj) => new Class1() { Id = obj.Id + obj.Id2 }));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1, "id2": 10 }""");
        ret.Id.ShouldBe(11);
    }

    [Fact]
    public void SchemaFirst_ResolveReference9()
    {
        var objectGraphType = SchemaFirstSetup(t => t.ResolveReference<Class3, Class1>((ctx, obj) => new Class1() { Id = obj.Id + obj.Id2 }));

        var ret = TestResolver<Class1>(objectGraphType, """{ "__typename": "Class1", "id": 1, "id2": 10 }""");
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
        public static TypeFirstTest4 Resolve(int id) => new() { Id = id + 10 };

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

    [Fact]
    public void TypeFirst_ResolveReference7()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest7>();

        var ret = TestResolver<TypeFirstTest7>(objectGraphType, """{ "__typename": "TypeFirstTest7", "id": 1, "id2": 10 }""");
        ret.Id.ShouldBe(11);
    }

    private class TypeFirstTest7
    {
        public int Id { get; set; }
        public int Id2 { get; set; }
        public string? Name { get; set; }

        [FederationResolver]
        public static TypeFirstTest7 Resolve(int id, int id2) => new() { Id = id + id2 };
    }

    [Fact]
    public void TypeFirst_ResolveReference8()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest8>();

        var ret = TestResolver<TypeFirstTest8>(objectGraphType, """{ "__typename": "TypeFirstTest8", "id": 1, "id2": 10 }""");
        ret.Id.ShouldBe(11);
    }

    private class TypeFirstTest8
    {
        public int Id { get; set; }
        public int Id2 { get; set; }
        public string? Name { get; set; }

        [FederationResolver]
        public TypeFirstTest8 Resolve() => new() { Id = Id + Id2 };
    }

    [Fact]
    public void TypeFirst_ResolveReference9_CustomScalar()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest9>();

        var ret = TestResolver<TypeFirstTest9>(objectGraphType, """{ "__typename": "TypeFirstTest9", "id": 1 }""");
        ret.Id.ShouldBe(110);
    }

    private class TypeFirstTest9
    {
        [OutputType(typeof(MyCustomScalar))]
        public int Id { get; set; }
        public string? Name { get; set; }

        [FederationResolver]
        public TypeFirstTest9 Resolve() => new() { Id = Id + 100 };
    }

    [Fact]
    public void TypeFirst_ResolveReference10_CustomScalar2()
    {
        var objectGraphType = TypeFirstSetup<TypeFirstTest10>();

        var ret = TestResolver<TypeFirstTest10>(objectGraphType, """{ "__typename": "TypeFirstTest10", "id": 1 }""");
        ret.Id.ShouldBe(110);
    }

    private class TypeFirstTest10
    {
        [OutputType(typeof(MyCustomScalar))]
        public int Id { get; set; }
        public string? Name { get; set; }

        [FederationResolver]
        public static TypeFirstTest10 Resolve([InputType(typeof(MyCustomScalar))] int id) => new() { Id = id + 100 };
    }

    private static IObjectGraphType TypeFirstSetup<T>()
    {
        var objectGraphType = new AutoRegisteringObjectGraphType<T>();
        objectGraphType.Fields.Find("Resolve").ShouldNotBeNull().IsPrivate.ShouldBeTrue();
        var schema = new Schema { Query = objectGraphType };
        schema.RegisterTypeMapping<T, AutoRegisteringObjectGraphType<T>>();
        schema.Initialize();
        objectGraphType.Fields.Find("Resolve").ShouldBeNull();
        objectGraphType.Fields.Find("resolve").ShouldBeNull();
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
                id2: Int!
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
        public MySchemaTypes(IEnumerable<IGraphType> types)
        {
            foreach (var type in types)
            {
                Dictionary.Add(type.Name, type);
            }
        }
    }

    private class Class2
    {
        public int Id { get; set; }
    }

    private class Class1
    {
        public int Id { get; set; }
        public int Id2 { get; set; }
        public string? Name { get; set; }
    }

    public class Class3
    {
        public int Id { get; set; }
        public int Id2 { get; set; }
    }

    public class MyCustomScalar : ScalarGraphType
    {
        public MyCustomScalar()
        {
            Name = "MyCustom";
        }
        public override object? ParseLiteral(GraphQLValue value) => throw new NotImplementedException();
        public override object? ParseValue(object? value) => value == null ? null : int.Parse(value.ToString()!, CultureInfo.InvariantCulture) * 10;
        public override object? Serialize(object? value) => throw new NotImplementedException();
    }
}
