#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Reflection;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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
        var fieldType = graphType.Fields.Find("TaskIntField").ShouldNotBeNull();
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
    [InlineData(nameof(ArgumentTestsInterface.WithNonNullString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), "arg1", null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithNullableString), null, null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), "arg1", null, null, null)]
    [InlineData(nameof(ArgumentTestsInterface.WithDefaultString), null, null, null, "test")]
    [InlineData(nameof(ArgumentTestsInterface.WithCancellationToken), null, null, null, true)]
    [InlineData(nameof(ArgumentTestsInterface.WithFromServices), null, null, null, "testService")]
    [InlineData(nameof(ArgumentTestsInterface.NamedArg), "arg1rename", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTestsInterface.IdArg), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTestsInterface.IdIntArg), "arg1", "123", null, 123)]
    [InlineData(nameof(ArgumentTestsInterface.IdIntArg), "arg1", 123, null, 123)]
    [InlineData(nameof(ArgumentTestsInterface.TypedArg), "arg1", "123", null, 123)]
    [InlineData(nameof(ArgumentTestsInterface.MultipleArgs), "arg1", "hello", 123, "hello123")]
    public async Task Argument_ResolverTests_WithNonNullString(string fieldName, string arg1Name, object? arg1Value, int? arg2Value, object? expected)
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
        fieldType.Resolver.ShouldNotBeNull();
        (await fieldType.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(expected);
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
    [InlineData("Field1", 1)]
    [InlineData("Field2", 2)]
    [InlineData("Field4", 4)]
    [InlineData("Field6AltName", 6)]
    [InlineData("Field7", 7)]
    public async Task FieldResolversWork(string fieldName, object expected)
    {
        var graph = new AutoRegisteringInterfaceGraphType<TestInterface>();
        var field = graph.Fields.Find(fieldName).ShouldNotBeNull();
        var resolver = field.Resolver.ShouldNotBeNull();
        var obj = new TestClass();
        var actual = await resolver.ResolveAsync(new ResolveFieldContext
        {
            Source = obj,
            FieldDefinition = new FieldType { Name = fieldName },
        }).ConfigureAwait(false);
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task WorksWithNoDefaultConstructor()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<NoDefaultConstructorTestInterface>();
        var context = new ResolveFieldContext
        {
            Source = new NoDefaultConstructorTestClass(true)
        };
        (await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(true);
        (await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe("test");
    }

    [Fact]
    public async Task ThrowsWhenSourceNull()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<NullSourceFailureTest>();
        var context = new ResolveFieldContext();
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringInterfaceGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringInterfaceGraphType as a root graph type or provide a root value.");
    }

#if !NET48 // .NET Framework 4.8 does not support default interface implementation, so would not support static methods on an interface
    [Fact]
    public async Task WorksWithNullSource()
    {
        var graphType = new TestFieldSupport<NullSourceTest>();
        var context = new ResolveFieldContext();
        (await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(true);
        (await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe("test");
        (await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(3);
    }

    private class TestFieldSupport<T> : AutoRegisteringInterfaceGraphType<T>
    {
        protected override IEnumerable<MemberInfo> GetRegisteredMembers()
            => base.GetRegisteredMembers().Concat(typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public));
    }

    private interface NullSourceTest
    {
        public static bool Example1 { get; set; } = true;
        public static string Example2() => "test";
        public static int Example3 = 3;
    }
#endif

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
    public async Task CustomHardcodedArgumentAttributesWork()
    {
        var graphType = new AutoRegisteringInterfaceGraphType<CustomHardcodedArgumentAttributeTestInterface>();
        var fieldType = graphType.Fields.Find(nameof(CustomHardcodedArgumentAttributeTestInterface.FieldWithHardcodedValue))!;
        var resolver = fieldType.Resolver!;
        (await resolver.ResolveAsync(new ResolveFieldContext
        {
            Source = new CustomHardcodedArgumentAttributeTestClass(),
        }).ConfigureAwait(false)).ShouldBe("85");
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
        string GetName([Id] int id);
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
    [InlineData("{find(type:CAT){id name}}", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy""}}}")]
    [InlineData("{find(type:DOG){id name}}", @"{""data"":{""find"":{""id"":""20"",""name"":""Shadow""}}}")]
    [InlineData("{find(type:CAT){id name ... on Cat { lives } ... on Dog { isLarge }}}", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy"",""lives"":9}}}")]
    [InlineData("{find(type:DOG){id name ... on Cat { lives } ... on Dog { isLarge }}}", @"{""data"":{""find"":{""id"":""20"",""name"":""Shadow"",""isLarge"":true}}}")]
    [InlineData("{cat{id name}}", @"{""data"":{""cat"":{""id"":""10"",""name"":""Fluffy""}}}")]
    [InlineData("{dog{id name}}", @"{""data"":{""dog"":{""id"":""20"",""name"":""Shadow""}}}")]
    [InlineData("{cat{...frag}} fragment frag on IAnimal { id name }", @"{""data"":{""cat"":{""id"":""10"",""name"":""Fluffy""}}}")]
    [InlineData("{dog{...frag}} fragment frag on IAnimal { id name }", @"{""data"":{""dog"":{""id"":""20"",""name"":""Shadow""}}}")]
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
        }).ConfigureAwait(false);
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var actual = serializer.Serialize(result);
        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [InlineData("{find(type:CAT){id name}}", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy""}}}")]
    [InlineData("{find(type:CAT){ ...frag }} fragment frag on IAnimal {id name}", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy""}}}")]
    public async Task ExecutesQueryWithInterfaceOnly(string query, string expected)
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
        var result = await executer.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = provider,
        }).ConfigureAwait(false);
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var actual = serializer.Serialize(result);
        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [InlineData("{find(type:CAT){id name}}", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy""}}}")]
    [InlineData("{find(type:CAT){ ...frag }} fragment frag on IAnimal { id name }", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy""}}}")]
    [InlineData("{find(type:CAT){ ...frag }} fragment frag on Dog { isLarge }", @"{""data"":{""find"":{}}}")]
    [InlineData("{find(type:CAT){ ... on Dog { isLarge } }}", @"{""data"":{""find"":{}}}")]
    [InlineData("{find(type:CAT){ id name ... on Dog { isLarge } }}", @"{""data"":{""find"":{""id"":""10"",""name"":""Fluffy""}}}")]
    public async Task ExecutesQueryWithInterfaceOnly2(string query, string expected)
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
        var result = await executer.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = provider,
        }).ConfigureAwait(false);
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var actual = serializer.Serialize(result);
        actual.ShouldBeCrossPlatJson(expected);
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
        [Id] int Id { get; }
    }

    public interface IAnimal : IObject
    {
        AnimalType Type { get; }
        string Name { get; }
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

    public class CustomHardcodedArgumentAttributeTestClass : CustomHardcodedArgumentAttributeTestInterface
    {
        public string FieldWithHardcodedValue(int value) => value.ToString();
    }

    public interface CustomHardcodedArgumentAttributeTestInterface
    {
        string FieldWithHardcodedValue([HardcodedValue] int value);
    }

    private class HardcodedValueAttribute : GraphQLAttribute
    {
        public override void Modify(ArgumentInformation argumentInformation)
            => argumentInformation.SetDelegate(context => 85);
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
        bool Example1 { get; set; }
        string Example2();
    }

    private interface NullSourceFailureTest
    {
        bool Example1 { get; set; }
        string Example2();
    }

    private interface FieldTests
    {
        [Name("Test1")]
        string? Field1 { get; set; }
        [Description("Test description")]
        string? Field2 { get; set; }
        [Obsolete("Test deprecation reason")]
        string? Field3 { get; set; }
        [CustomDescription]
        string? Field4 { get; set; }
        [Description("Test description")]
        [Metadata("key1", "value1")]
        [Metadata("key2", "value2")]
        string? Field5 { get; set; }
        [InputType(typeof(IdGraphType))]
        int? Field6 { get; set; }
        [OutputType(typeof(IdGraphType))]
        int? Field7 { get; set; }
        [DefaultValue("hello")]
        string? Field8 { get; set; }
        [InputName("InputField9")]
        string? Field9 { get; set; }
        [OutputName("OutputField10")]
        string? Field10 { get; set; }
        [Ignore]
        string? Field11 { get; set; }
        int NotNullIntField { get; set; }
        int? NullableIntField { get; set; }
        string NotNullStringField { get; set; }
        string? NullableStringField { get; set; }
        string NotNullStringGetOnlyField { get; }
        string? NullableStringGetOnlyField { get; }
        List<string?> NotNullListNullableStringField { get; set; }
        List<string> NotNullListNotNullStringField { get; set; }
        List<string?>? NullableListNullableStringField { get; set; }
        List<string>? NullableListNotNullStringField { get; set; }
        IEnumerable<int?> NotNullEnumerableNullableIntField { get; set; }
        IEnumerable<int> NotNullEnumerableNotNullIntField { get; set; }
        IEnumerable<int?>? NullableEnumerableNullableIntField { get; set; }
        IEnumerable<int>? NullableEnumerableNotNullIntField { get; set; }
        Tuple<int, string>?[] NotNullArrayNullableTupleField { get; set; }
        Tuple<int, string>[] NotNullArrayNotNullTupleField { get; set; }
        Tuple<int, string>?[]? NullableArrayNullableTupleField { get; set; }
        Tuple<int, string>[]? NullableArrayNotNullTupleField { get; set; }
        [Id]
        int IdField { get; set; }
        [Id]
        int? NullableIdField { get; set; }
        IEnumerable EnumerableField { get; set; }
        ICollection CollectionField { get; set; }
        IEnumerable? NullableEnumerableField { get; set; }
        ICollection? NullableCollectionField { get; set; }
        int?[]?[]? ListOfListOfIntsField { get; set; }
        Task<string> TaskStringField();
        Task<int> TaskIntFieldAsync();
        IDataLoaderResult<string?> DataLoaderNullableStringField();
        IDataLoaderResult<string>? NullableDataLoaderStringField();
        Task<IDataLoaderResult<string?[]>> TaskDataLoaderStringArrayField();
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
        public int TypedArg([InputType(typeof(StringGraphType))] int arg1) => arg1;
        public string MultipleArgs(string arg1, int arg2) => arg1 + arg2.ToString();
    }

    private interface ArgumentTestsInterface
    {
        string? WithNonNullString(string arg1);
        string? WithNullableString(string? arg1);
        string? WithDefaultString(string arg1 = "test");
        string? WithNullableDefaultString(string? arg1 = "test");
        bool WithCancellationToken(CancellationToken cancellationToken);
        string? WithResolveFieldContext(IResolveFieldContext context);
        string WithFromServices([FromServices] string arg1);
        string NamedArg([Name("arg1rename")] string arg1);
        string IdArg([Id] string arg1);
        int IdIntArg([Id] int arg1);
        int TypedArg([InputType(typeof(StringGraphType))] int arg1);
        string MultipleArgs(string arg1, int arg2);
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

    private class TestClass : TestInterface
    {
        public int Field1 { get; set; } = 1;
        public int Field2 => 2;
        public int Field3 { set { } }
        public int Field4() => 4;
        [Name("Field6AltName")]
        public int Field6 => 6;
        public Task<int> Field7 => Task.FromResult(7);
    }

    private interface TestInterface
    {
        int Field1 { get; set; }
        int Field2 { get; }
        int Field3 { set; }
        int Field4();
        [Name("Field6AltName")]
        int Field6 { get; }
        Task<int> Field7 { get; }
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
        string? Field1 { get; set; }
    }
    private interface DerivedInterface : ParentInterface
    {
    }

    private interface TestExceptionBubblingInterface
    {
        string Test([TestExceptionBubbling] string arg);
    }

    private class TestExceptionBubblingAttribute : GraphQLAttribute
    {
        public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        {
            throw new Exception("Test");
        }
    }

    private interface TestBasicInterface
    {
        int Id { get; set; }
        string Name { get; set; }
    }

    public interface InheritanceTestsGrandparent
    {
        int Field1 { get; }
        int Field2();
    }

    public interface InheritanceTestsParent : InheritanceTestsGrandparent
    {
        int Field3();
    }

    public interface InheritanceTests : InheritanceTestsParent
    {
        int Field4 { get; }
        int Field5();
    }
}
