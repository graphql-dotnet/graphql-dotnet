using System.Collections;
using System.ComponentModel;
using System.Reflection;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class AutoRegisteringInterfaceGraphTypeTests
{
    [Fact]
    public void Interface_RecognizesNameAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithCustomName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Interface_IgnoresInputNameAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithCustomInputName>();
        graphType.Name.ShouldBe("TestInterface_WithCustomInputName");
    }

    [Fact]
    public void Interface_RecognizesOutputNameAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithCustomOutputName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Interface_RecognizesDescriptionAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithCustomDescription>();
        graphType.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Interface_RecognizesObsoleteAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithCustomDeprecationReason>();
        graphType.DeprecationReason.ShouldBe("Test deprecation reason");
    }

    [Fact]
    public void Interface_RecognizesCustomGraphQLAttributes()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithCustomAttributes>();
        graphType.Description.ShouldBe("Test custom description");
    }

    [Fact]
    public void Interface_RecognizesMultipleAttributes()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestInterface_WithMultipleAttributes>();
        graphType.Description.ShouldBe("Test description");
        graphType.GetMetadata<string>("key1").ShouldBe("value1");
        graphType.GetMetadata<string>("key2").ShouldBe("value2");
    }

    [Fact]
    public void Interface_CanOverrideDefaultName()
    {
        var graphType = new TestOverrideDefaultName<TestInterface>();
        graphType.Name.ShouldBe("TestInterfaceInterface");
    }

    [Fact]
    public void Interface_AttributesApplyOverOverriddenDefaultName()
    {
        var graphType = new TestOverrideDefaultName<TestInterface_WithCustomName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Interface_RecognizesInheritedAttributes()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<DerivedInterface>();
        graphType.Fields.Find("Field1CustomName").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesNameAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        graphType.Fields.Find("Test1").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesDescriptionAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field2").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Field_RecognizesObsoleteAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field3").ShouldNotBeNull();
        fieldType.DeprecationReason.ShouldBe("Test deprecation reason");
    }

    [Fact]
    public void Field_RecognizesCustomGraphQLAttributes()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field4").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test custom description for field");
    }

    [Fact]
    public void Field_RecognizesMultipleAttributes()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field5").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test description");
        fieldType.GetMetadata<string>("key1").ShouldBe("value1");
        fieldType.GetMetadata<string>("key2").ShouldBe("value2");
    }

    [Fact]
    public void Field_IgnoresInputTypeAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field6").ShouldNotBeNull();
        fieldType.Type.ShouldBe(typeof(GraphQLClrOutputTypeReference<int>));
    }

    [Fact]
    public void Field_RecognizesOutputTypeAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field7").ShouldNotBeNull();
        fieldType.Type.ShouldBe(typeof(IdGraphType));
    }

    [Fact]
    public void Field_IgnoresDefaultValueAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field8").ShouldNotBeNull();
        fieldType.DefaultValue.ShouldBeNull();
    }

    [Fact]
    public void Field_IgnoresInputNameAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        graphType.Fields.Find("Field9").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesOutputNameAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        graphType.Fields.Find("OutputField10").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesIgnoreAttribute()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        graphType.Fields.Find("Field11").ShouldBeNull();
    }

    [Theory]
    [InlineData(nameof(FieldTests.NotNullIntField), typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(nameof(FieldTests.NullableIntField), typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(nameof(FieldTests.NotNullStringField), typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(nameof(FieldTests.NotNullStringGetOnlyField), typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(nameof(FieldTests.NullableStringGetOnlyField), typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(nameof(FieldTests.NullableStringField), typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(nameof(FieldTests.NotNullListNullableStringField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<string>>>))]
    [InlineData(nameof(FieldTests.NotNullListNotNullStringField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<string>>>>))]
    [InlineData(nameof(FieldTests.NullableListNullableStringField), typeof(ListGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(nameof(FieldTests.NullableListNotNullStringField), typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<string>>>))]
    [InlineData(nameof(FieldTests.NotNullEnumerableNullableIntField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(nameof(FieldTests.NotNullEnumerableNotNullIntField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableNullableIntField), typeof(ListGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableNotNullIntField), typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(nameof(FieldTests.NotNullArrayNullableTupleField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>>))]
    [InlineData(nameof(FieldTests.NotNullArrayNotNullTupleField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>>>))]
    [InlineData(nameof(FieldTests.NullableArrayNullableTupleField), typeof(ListGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>))]
    [InlineData(nameof(FieldTests.NullableArrayNotNullTupleField), typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>>))]
    [InlineData(nameof(FieldTests.IdField), typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(FieldTests.NullableIdField), typeof(IdGraphType))]
    [InlineData(nameof(FieldTests.EnumerableField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<object>>>))]
    [InlineData(nameof(FieldTests.CollectionField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<object>>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableField), typeof(ListGraphType<GraphQLClrOutputTypeReference<object>>))]
    [InlineData(nameof(FieldTests.NullableCollectionField), typeof(ListGraphType<GraphQLClrOutputTypeReference<object>>))]
    [InlineData(nameof(FieldTests.ListOfListOfIntsField), typeof(ListGraphType<ListGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(nameof(FieldTests.TaskStringField), typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData("TaskIntField", typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData("ValueTaskStringField", typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData("AsyncEnumerableIntField", typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(nameof(FieldTests.DataLoaderNullableStringField), typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(nameof(FieldTests.NullableDataLoaderStringField), typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(nameof(FieldTests.TaskDataLoaderStringArrayField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<string>>>))]
    public void Field_DectectsProperType(string fieldName, Type expectedGraphType)
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
        fieldType.Type.ShouldBe(expectedGraphType);
    }

    [Fact]
    public void Field_RemovesAsyncSuffix()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<FieldTests>();
        graphType.Fields.Find("TaskIntField").ShouldNotBeNull();
        graphType.Fields.Find("ValueTaskStringField").ShouldNotBeNull();
        graphType.Fields.Find("AsyncEnumerableIntField").ShouldNotBeNull();
    }

    [Fact]
    public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
    {
        var provider = new DefaultServiceProvider();
        provider.GetService(typeof(AutoRegisteringInterfaceGraphType<TestInterface>)).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(ArgumentTestsInterface.WithNonNullString), "arg1", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), "arg1", typeof(GraphQLClrInputTypeReference<string>))]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), "arg1", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableDefaultString), "arg1", typeof(GraphQLClrInputTypeReference<string>))]
    [InlineData(nameof(ArgumentTestsInterface.WithCancellationToken), "cancellationToken", null)]
    [InlineData(nameof(ArgumentTestsInterface.WithResolveFieldContext), "context", null)]
    [InlineData(nameof(ArgumentTestsInterface.WithFromServices), "arg1", null)]
    [InlineData(nameof(ArgumentTestsInterface.NamedArg), "arg1", null)]
    [InlineData(nameof(ArgumentTestsInterface.NamedArg), "arg1rename", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTestsInterface.IdArg), "arg1", typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(ArgumentTestsInterface.TypedArg), "arg1", typeof(StringGraphType))]
    [InlineData(nameof(ArgumentTestsInterface.MultipleArgs), "arg1", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTestsInterface.MultipleArgs), "arg2", typeof(NonNullGraphType<GraphQLClrInputTypeReference<int>>))]
    public void Argument_VerifyType(string fieldName, string argumentName, Type? argumentGraphType)
    {
        var graphType = new AutoRegisteringInterfaceGraphType<ArgumentTestsInterface>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
        var argument = fieldType.Arguments?.Find(argumentName);
        if (argumentGraphType != null)
        {
            argument.ShouldNotBeNull($"Argument '{argumentName}' of field '{fieldName}' could not be found.");
            argument.Type.ShouldBe(argumentGraphType);
        }
        else
        {
            argument.ShouldBeNull($"Argument '{argumentName}' of field '{fieldName}' should not exist but does.");
        }
    }

    [Theory]
    [InlineData(nameof(ArgumentTestsInterface.WithNonNullString), "arg1", "hello", null)]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), "arg1", "hello", null)]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), "arg1", null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), "arg1", "hello", null)]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), "arg1", null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithCancellationToken), null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithFromServices), null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.NamedArg), "arg1rename", "hello", null)]
    [InlineData(nameof(ArgumentTestsInterface.IdArg), "arg1", "hello", null)]
    [InlineData(nameof(ArgumentTestsInterface.IdIntArg), "arg1", "123", null)]
    [InlineData(nameof(ArgumentTestsInterface.IdIntArg), "arg1", 123, null)]
    [InlineData(nameof(ArgumentTestsInterface.TypedArg), "arg1", "123", null)]
    [InlineData(nameof(ArgumentTestsInterface.MultipleArgs), "arg1", "hello", 123)]
    public void Argument_ResolverTests(string fieldName, string? arg1Name, object? arg1Value, int? arg2Value)
    {
        var graphType = new AutoRegisteringInterfaceGraphType<ArgumentTestsInterface>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            CancellationToken = cts.Token,
            Source = new ArgumentTestsClass(),
        };
        if (arg1Name != null)
        {
            context.Arguments.Add(arg1Name, new ArgumentValue(arg1Value, ArgumentSource.Variable));
        }
        if (arg2Value.HasValue)
        {
            context.Arguments.Add("arg2", new ArgumentValue(arg2Value, ArgumentSource.Variable));
        }
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<string>("testService");
        using var provider = serviceCollection.BuildServiceProvider();
        context.RequestServices = provider;
        fieldType.Resolver.ShouldBeNull();
    }

    [Fact]
    public void RegistersReadablePropertiesAndMethodsOnly()
    {
        var outputType = new AutoRegisteringInterfaceGraphType<TestInterface>();
        outputType.Fields.Find("Field1").ShouldNotBeNull();
        outputType.Fields.Find("Field2").ShouldNotBeNull();
        outputType.Fields.Find("Field3").ShouldBeNull();
        outputType.Fields.Find("Field4").ShouldNotBeNull();
        outputType.Fields.Find("Field5").ShouldBeNull();
    }

    [Fact]
    public void SkipsSpecifiedProperties()
    {
        var outputType = new AutoRegisteringInterfaceGraphType<TestInterface>(x => x.Field1);
        outputType.Fields.Find("Field1").ShouldBeNull();
        outputType.Fields.Find("Field2").ShouldNotBeNull();
        outputType.Fields.Find("Field3").ShouldBeNull();
        outputType.Fields.Find("Field4").ShouldNotBeNull();
        outputType.Fields.Find("Field5").ShouldBeNull();
    }

    [Fact]
    public void CanOverrideFieldGeneration()
    {
        var graph = new TestChangingName<TestInterface>();
        graph.Fields.Find("Field1Prop").ShouldNotBeNull();
    }

    [Fact]
    public void CanOverrideFieldList()
    {
        var graph = new TestChangingFieldList<TestInterface>();
        graph.Fields.Count.ShouldBe(1);
        graph.Fields.Find("Field1").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Field1")]
    [InlineData("Field2")]
    [InlineData("Field4")]
    [InlineData("Field6AltName")]
    [InlineData("Field7")]
    public void FieldResolvers_Of_InterfaceGraphType_ShouldBe_Null(string fieldName)
    {
        var graph = new AutoRegisteringInterfaceGraphType<TestInterface>();
        var field = graph.Fields.Find(fieldName).ShouldNotBeNull();
        field.Resolver.ShouldBeNull();
    }

    [Fact]
    public void TestExceptionBubbling()
    {
        Should.Throw<Exception>(() => new AutoRegisteringInterfaceGraphType<TestExceptionBubblingInterface>()).Message.ShouldBe("Test");
    }

    [Fact]
    public void TestBasicClassNoExtraFields()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<TestBasicInterface>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void CannotImplementWithMismatchedInterface_ReturnType()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b.AddAutoSchema<TestMismatchedObject1>());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        Should.Throw<ArgumentException>(() => schema.Initialize())
            .Message.ShouldBe("Type AutoRegisteringObjectGraphType<TestMismatchedObject1> with name 'TestMismatchedObject1' does not implement interface AutoRegisteringInterfaceGraphType<Interface1> with name 'Interface1'. Field 'id' must be of type 'ID!' or covariant from it, but in fact it is of type 'Int!'.");
    }

    [Implements(typeof(Interface1))]
    public class TestMismatchedObject1 : Interface1
    {
        public int Id { get; set; }
    }

    [Fact(Skip = "Check not implemented yet - see issue #3285")]
    public void CannotImplementWithMismatchedInterface_Argument()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b.AddAutoSchema<TestMismatchedObject2>());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        Should.Throw<ArgumentException>(() => schema.Initialize());
    }

    [Implements(typeof(Interface3))]
    public class TestMismatchedObject2 : Interface3
    {
        public string GetName(int id) => null!;
    }

    public interface Interface3
    {
        public string GetName([Id] int id);
    }

    [Fact]
    public void GraphTypeCanImplementMultipleInterfaces()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b.AddAutoSchema<TestObjectWithMultipleInterfaces>());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var testType = schema.AllTypes[nameof(TestObjectWithMultipleInterfaces)].ShouldBeAssignableTo<IObjectGraphType>().ShouldNotBeNull();
        var if1Type = schema.AllTypes[nameof(Interface1)].ShouldNotBeNull();
        var if2Type = schema.AllTypes[nameof(Interface2)].ShouldNotBeNull();
        testType.ResolvedInterfaces.ShouldContain(if1Type);
        testType.ResolvedInterfaces.ShouldContain(if2Type);
    }

    [Implements(typeof(Interface1))]
    [Implements(typeof(Interface2))]
    public class TestObjectWithMultipleInterfaces : Interface1, Interface2
    {
        [Id] public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public interface Interface1
    {
        [Id] public int Id { get; set; }
    }

    public interface Interface2 : Interface1
    {
        public string Name { get; set; }
    }

    [Fact]
    public void TestInheritedFields()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<InheritanceTests>();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
        graphType.Fields.Find("Field3").ShouldNotBeNull();
        graphType.Fields.Find("Field4").ShouldNotBeNull();
        graphType.Fields.Find("Field5").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(5);
    }

    [Theory]
    [InlineData("{find(type:CAT){id name}}", """{"data":{"find":{"id":"10","name":"Fluffy"}}}""")]
    [InlineData("{find(type:DOG){id name}}", """{"data":{"find":{"id":"20","name":"Shadow"}}}""")]
    [InlineData("{find(type:CAT){id name ... on Cat { lives } ... on Dog { isLarge }}}", """{"data":{"find":{"id":"10","name":"Fluffy","lives":9}}}""")]
    [InlineData("{find(type:DOG){id name ... on Cat { lives } ... on Dog { isLarge }}}", """{"data":{"find":{"id":"20","name":"Shadow","isLarge":true}}}""")]
    [InlineData("{cat{id name}}", """{"data":{"cat":{"id":"10","name":"Fluffy"}}}""")]
    [InlineData("{dog{id name}}", """{"data":{"dog":{"id":"20","name":"Shadow"}}}""")]
    [InlineData("{cat{...frag}} fragment frag on IAnimal { id name }", """{"data":{"cat":{"id":"10","name":"Fluffy"}}}""")]
    [InlineData("{dog{...frag}} fragment frag on IAnimal { id name }", """{"data":{"dog":{"id":"20","name":"Shadow"}}}""")]
    public async Task EndToEndTest(string query, string expected)
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<TestQuery>()
            .AddSystemTextJson());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.AllTypes.Select(x => x.Name).OrderBy(x => x).Where(x => !x.StartsWith("__"))
            .ShouldBe(new[] { "AnimalType", "Boolean", "Cat", "Dog", "IAnimal", "ID", "Int", "String", "TestQuery" });
        var executer = provider.GetRequiredService<IDocumentExecuter>();
        var result = await executer.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = provider,
        });
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        string actual = serializer.Serialize(result);
        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [InlineData("{find(type:CAT){id name}}")]
    [InlineData("{find(type:CAT){ ...frag }} fragment frag on IAnimal {id name}")]
    public async Task RejectQueryWithInterfaceOnly(string query)
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<TestQuery2>()
            .AddSystemTextJson());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.AllTypes.Select(x => x.Name).OrderBy(x => x).Where(x => !x.StartsWith("__"))
            .ShouldBe(new[] { "AnimalType", "Boolean", "IAnimal", "ID", "String", "TestQuery2" });
        var executer = provider.GetRequiredService<IDocumentExecuter>();
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => executer.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = provider,
            ThrowOnUnhandledException = true,
        }));
        ex.Message.ShouldBe("Abstract type IAnimal must resolve to an Object type at runtime for field TestQuery2.find with value 'GraphQL.Tests.Types.AutoRegisteringInterfaceGraphTypeTests+Cat', received 'null'.");
    }

    [Theory]
    [InlineData("{find(type:CAT){__typename}}")]
    [InlineData("{find(type:CAT){id name}}")]
    [InlineData("{find(type:CAT){ ...frag }} fragment frag on IAnimal { id name }")]
    [InlineData("{find(type:CAT){ ...frag }} fragment frag on Dog { isLarge }")]
    [InlineData("{find(type:CAT){ ... on Dog { isLarge } }}")]
    [InlineData("{find(type:CAT){ id name ... on Dog { isLarge } }}")]
    public async Task RejectsQueryWithInterfaceOnly2(string query)
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<TestQuery3>()
            .AddSystemTextJson());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.AllTypes.Select(x => x.Name).OrderBy(x => x).Where(x => !x.StartsWith("__"))
            .ShouldBe(new[] { "AnimalType", "Boolean", "Dog", "IAnimal", "ID", "String", "TestQuery3" });
        var executer = provider.GetRequiredService<IDocumentExecuter>();
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => executer.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = provider,
            ThrowOnUnhandledException = true,
        }));
        ex.Message.ShouldBe("Abstract type IAnimal must resolve to an Object type at runtime for field TestQuery3.find with value 'GraphQL.Tests.Types.AutoRegisteringInterfaceGraphTypeTests+Cat', received 'null'.");
    }

    [Fact]
    public void BuildFieldTypeChecks()
    {
        new TestProtectedMethods().Test();
    }

    public class TestProtectedMethods : AutoRegisteringInterfaceGraphType<IAnimal>
    {
        public void Test()
        {
            Should.Throw<ArgumentNullException>(() => BuildFieldType(null!, typeof(TestProtectedMethods).GetMethod(nameof(Test))!))
                .ParamName.ShouldBe("fieldType");
            Should.Throw<ArgumentNullException>(() => BuildFieldType(new FieldType(), null!))
                .ParamName.ShouldBe("memberInfo");
            Should.Throw<ArgumentOutOfRangeException>(() => BuildFieldType(new FieldType(), typeof(TestProtectedMethods)))
                .ParamName.ShouldBe("memberInfo");
        }
    }

    public class TestQuery
    {
        public static IAnimal Find(AnimalType type) => type switch
        {
            AnimalType.Cat => Cat(),
            AnimalType.Dog => Dog(),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        public static Cat Cat() => new Cat() { Name = "Fluffy", Lives = 9 };
        public static Dog Dog() => new Dog() { Name = "Shadow", IsLarge = true };
    }

    public class TestQuery2
    {
        public static IAnimal Find(AnimalType type) => type switch
        {
            AnimalType.Cat => new Cat() { Name = "Fluffy", Lives = 9 },
            AnimalType.Dog => new Dog() { Name = "Shadow", IsLarge = true },
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    public class TestQuery3
    {
        public static IAnimal Find(AnimalType type) => type switch
        {
            AnimalType.Cat => new Cat() { Name = "Fluffy", Lives = 9 },
            AnimalType.Dog => new Dog() { Name = "Shadow", IsLarge = true },
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        public static Dog Dog() => new Dog() { Name = "Shadow", IsLarge = true };
    }

    public interface IObject
    {
        [Id] public int Id { get; }
    }

    public interface IAnimal : IObject
    {
        public AnimalType Type { get; }
        public string Name { get; }
    }

    public enum AnimalType
    {
        Cat,
        Dog,
    }

    [Implements(typeof(IAnimal))]
    public class Cat : IAnimal
    {
        [Id] public int Id => 10;
        public AnimalType Type => AnimalType.Cat;
        public string Name { get; set; } = null!;
        public int Lives { get; set; }
    }

    [Implements(typeof(IAnimal))]
    public class Dog : IAnimal
    {
        [Id] public int Id => 20;
        public AnimalType Type => AnimalType.Dog;
        public string Name { get; set; } = null!;
        public bool IsLarge { get; set; }
    }

    private class NoDefaultConstructorTestClass : NoDefaultConstructorTestInterface
    {
        public NoDefaultConstructorTestClass(bool value)
        {
            Example1 = value;
        }

        public bool Example1 { get; set; }
        public string Example2() => "test";
    }

    private interface NoDefaultConstructorTestInterface
    {
        public bool Example1 { get; set; }
        public string Example2();
    }

    private interface FieldTests
    {
        [Name("Test1")]
        public string? Field1 { get; set; }
        [Description("Test description")]
        public string? Field2 { get; set; }
        [Obsolete("Test deprecation reason")]
        public string? Field3 { get; set; }
        [CustomDescription]
        public string? Field4 { get; set; }
        [Description("Test description")]
        [Metadata("key1", "value1")]
        [Metadata("key2", "value2")]
        public string? Field5 { get; set; }
#if NET48
        [InputType(typeof(IdGraphType))]
#else
        [InputType<IdGraphType>()]
#endif
        public int? Field6 { get; set; }
#if NET48
        [OutputType(typeof(IdGraphType))]
#else
        [OutputType<IdGraphType>()]
#endif
        public int? Field7 { get; set; }
        [DefaultValue("hello")]
        public string? Field8 { get; set; }
        [InputName("InputField9")]
        public string? Field9 { get; set; }
        [OutputName("OutputField10")]
        public string? Field10 { get; set; }
        [Ignore]
        public string? Field11 { get; set; }
        public int NotNullIntField { get; set; }
        public int? NullableIntField { get; set; }
        public string NotNullStringField { get; set; }
        public string? NullableStringField { get; set; }
        public string NotNullStringGetOnlyField { get; }
        public string? NullableStringGetOnlyField { get; }
        public List<string?> NotNullListNullableStringField { get; set; }
        public List<string> NotNullListNotNullStringField { get; set; }
        public List<string?>? NullableListNullableStringField { get; set; }
        public List<string>? NullableListNotNullStringField { get; set; }
        public IEnumerable<int?> NotNullEnumerableNullableIntField { get; set; }
        public IEnumerable<int> NotNullEnumerableNotNullIntField { get; set; }
        public IEnumerable<int?>? NullableEnumerableNullableIntField { get; set; }
        public IEnumerable<int>? NullableEnumerableNotNullIntField { get; set; }
        public Tuple<int, string>?[] NotNullArrayNullableTupleField { get; set; }
        public Tuple<int, string>[] NotNullArrayNotNullTupleField { get; set; }
        public Tuple<int, string>?[]? NullableArrayNullableTupleField { get; set; }
        public Tuple<int, string>[]? NullableArrayNotNullTupleField { get; set; }
        [Id]
        public int IdField { get; set; }
        [Id]
        public int? NullableIdField { get; set; }
        public IEnumerable EnumerableField { get; set; }
        public ICollection CollectionField { get; set; }
        public IEnumerable? NullableEnumerableField { get; set; }
        public ICollection? NullableCollectionField { get; set; }
        public int?[]?[]? ListOfListOfIntsField { get; set; }
        public Task<string> TaskStringField();
        public Task<int> TaskIntFieldAsync();
        public ValueTask<string> ValueTaskStringFieldAsync();
        public IAsyncEnumerable<int> AsyncEnumerableIntFieldAsync();
        public IDataLoaderResult<string?> DataLoaderNullableStringField();
        public IDataLoaderResult<string>? NullableDataLoaderStringField();
        public Task<IDataLoaderResult<string?[]>> TaskDataLoaderStringArrayField();
    }

    private class ArgumentTestsClass : ArgumentTestsInterface
    {
        public string? WithNonNullString(string arg1) => arg1;
        public string? WithNullableString(string? arg1) => arg1;
        public string? WithDefaultString(string arg1 = "test") => arg1;
        public string? WithNullableDefaultString(string? arg1 = "test") => arg1;
        public bool WithCancellationToken(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested;
        public string? WithResolveFieldContext(IResolveFieldContext context) => (string?)context.Source;
        public string WithFromServices([FromServices] string arg1) => arg1;
        public string NamedArg([Name("arg1rename")] string arg1) => arg1;
        public string IdArg([Id] string arg1) => arg1;
        public int IdIntArg([Id] int arg1) => arg1;
        public int TypedArg(
#if NET48
        [InputType(typeof(StringGraphType))]
#else
        [InputType<StringGraphType>()]
#endif
        int arg1) => arg1;
        public string MultipleArgs(string arg1, int arg2) => arg1 + arg2.ToString();
    }

    private interface ArgumentTestsInterface
    {
        public string? WithNonNullString(string arg1);
        public string? WithNullableString(string? arg1);
        public string? WithDefaultString(string arg1 = "test");
        public string? WithNullableDefaultString(string? arg1 = "test");
        public bool WithCancellationToken(CancellationToken cancellationToken);
        public string? WithResolveFieldContext(IResolveFieldContext context);
        public string WithFromServices([FromServices] string arg1);
        public string NamedArg([Name("arg1rename")] string arg1);
        public string IdArg([Id] string arg1);
        public int IdIntArg([Id] int arg1);
        public int TypedArg(
#if NET48
        [InputType(typeof(StringGraphType))]
#else
        [InputType<StringGraphType>()]
#endif
        int arg1);
        public string MultipleArgs(string arg1, int arg2);
    }

    private class TestChangingFieldList<T> : AutoRegisteringInterfaceGraphType<T>
    {
        protected override IEnumerable<FieldType> ProvideFields()
        {
            yield return CreateField(GetRegisteredMembers().First(x => x.Name == "Field1"))!;
        }
    }

    private class TestChangingName<T> : AutoRegisteringInterfaceGraphType<T>
    {
        protected override FieldType CreateField(MemberInfo memberInfo)
        {
            var field = base.CreateField(memberInfo)!;
            field.Name += "Prop";
            return field;
        }
    }

    private interface TestInterface
    {
        public int Field1 { get; set; }
        public int Field2 { get; }
        public int Field3 { set; }
        public int Field4();
        [Name("Field6AltName")]
        public int Field6 { get; }
        public Task<int> Field7 { get; }
    }

    [Name("TestWithCustomName")]
    private interface TestInterface_WithCustomName { }

    [InputName("TestWithCustomName")]
    private interface TestInterface_WithCustomInputName { }

    [OutputName("TestWithCustomName")]
    private interface TestInterface_WithCustomOutputName { }

    [Description("Test description")]
    private interface TestInterface_WithCustomDescription { }

    [Obsolete("Test deprecation reason")]
    private interface TestInterface_WithCustomDeprecationReason { }

    [CustomDescription]
    private interface TestInterface_WithCustomAttributes { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    private class CustomDescriptionAttribute : GraphQLAttribute
    {
        public override void Modify(IGraphType graphType)
        {
            graphType.Description = "Test custom description";
        }

        public override void Modify(FieldType fieldType, bool isInputType)
        {
            fieldType.Description = "Test custom description for field";
        }
    }

    [Metadata("key1", "value1")]
    [Metadata("key2", "value2")]
    [Description("Test description")]
    private interface TestInterface_WithMultipleAttributes { }

    private class TestOverrideDefaultName<T> : AutoRegisteringInterfaceGraphType<T>
    {
        protected override void ConfigureGraph()
        {
            Name = typeof(T).Name + "Interface";
            base.ConfigureGraph();
        }
    }

    private interface ParentInterface
    {
        [Name("Field1CustomName")]
        public string? Field1 { get; set; }
    }
    private interface DerivedInterface : ParentInterface
    {
    }

    private interface TestExceptionBubblingInterface
    {
        public string Test([TestExceptionBubbling] string arg);
    }

    private class TestExceptionBubblingAttribute : GraphQLAttribute
    {
        public override void Modify(ArgumentInformation argumentInformation)
        {
            throw new Exception("Test");
        }
    }

    private interface TestBasicInterface
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface InheritanceTestsGrandparent
    {
        public int Field1 { get; }
        public int Field2();
    }

    public interface InheritanceTestsParent : InheritanceTestsGrandparent
    {
        public int Field3();
    }

    public interface InheritanceTests : InheritanceTestsParent
    {
        public int Field4 { get; }
        public int Field5();
    }

    [Fact]
    public void MemberScanAttribute_Interface_PropertiesOnly()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<InterfacePropertiesOnlyClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_Interface_MethodsOnly()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<InterfaceMethodsOnlyClass>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldNotBeNull();
        graphType.Fields.Find("Method2").ShouldNotBeNull();
    }

    [Fact]
    public void MemberScanAttribute_Interface_MethodsOnlyDerivedClass()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<InterfaceMethodsOnlyDerivedClass>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Property2").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldNotBeNull();
        graphType.Fields.Find("Method2").ShouldNotBeNull();
        graphType.Fields.Find("Method3").ShouldNotBeNull();
    }

    [MemberScan(ScanMemberTypes.Properties)]
    private class InterfacePropertiesOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Property2 { get; set; } = "prop2";
        public string Method1() => "method1";
    }

    [MemberScan(ScanMemberTypes.Methods)]
    private class InterfaceMethodsOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Method1() => "method1";
        public string Method2() => "method2";
    }

    private class InterfaceMethodsOnlyDerivedClass : InterfaceMethodsOnlyClass
    {
        public string Property2 { get; set; } = "prop2";
        public string Method3() => "method3";
    }
}
