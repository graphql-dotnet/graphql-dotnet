#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Reflection;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class AutoRegisteringObjectGraphTypeTests
{
    [Fact]
    public void Class_RecognizesNameAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Class_IgnoresInputNameAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomInputName>();
        graphType.Name.ShouldBe("TestClass_WithCustomInputName");
    }

    [Fact]
    public void Class_RecognizesOutputNameAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomOutputName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Class_RecognizesDescriptionAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomDescription>();
        graphType.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Class_RecognizesObsoleteAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomDeprecationReason>();
        graphType.DeprecationReason.ShouldBe("Test deprecation reason");
    }

    [Fact]
    public void Class_RecognizesCustomGraphQLAttributes()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomAttributes>();
        graphType.Description.ShouldBe("Test custom description");
    }

    [Fact]
    public void Class_RecognizesMultipleAttributes()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestClass_WithMultipleAttributes>();
        graphType.Description.ShouldBe("Test description");
        graphType.GetMetadata<string>("key1").ShouldBe("value1");
        graphType.GetMetadata<string>("key2").ShouldBe("value2");
    }

    [Fact]
    public void Class_CanOverrideDefaultName()
    {
        var graphType = new TestOverrideDefaultName<TestClass>();
        graphType.Name.ShouldBe("TestClassInput");
    }

    [Fact]
    public void Class_AttributesApplyOverOverriddenDefaultName()
    {
        var graphType = new TestOverrideDefaultName<TestClass_WithCustomName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Class_RecognizesInheritedAttributes()
    {
        var graphType = new AutoRegisteringObjectGraphType<DerivedClass>();
        graphType.Fields.Find("Field1CustomName").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesNameAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        graphType.Fields.Find("Test1").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesDescriptionAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field2").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Field_RecognizesObsoleteAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field3").ShouldNotBeNull();
        fieldType.DeprecationReason.ShouldBe("Test deprecation reason");
    }

    [Fact]
    public void Field_RecognizesCustomGraphQLAttributes()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field4").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test custom description for field");
    }

    [Fact]
    public void Field_RecognizesMultipleAttributes()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field5").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test description");
        fieldType.GetMetadata<string>("key1").ShouldBe("value1");
        fieldType.GetMetadata<string>("key2").ShouldBe("value2");
    }

    [Fact]
    public void Field_IgnoresInputTypeAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field6").ShouldNotBeNull();
        fieldType.Type.ShouldBe(typeof(GraphQLClrOutputTypeReference<int>));
    }

    [Fact]
    public void Field_RecognizesOutputTypeAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field7").ShouldNotBeNull();
        fieldType.Type.ShouldBe(typeof(IdGraphType));
    }

    [Fact]
    public void Field_IgnoresDefaultValueAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field8").ShouldNotBeNull();
        fieldType.DefaultValue.ShouldBeNull();
    }

    [Fact]
    public void Field_IgnoresInputNameAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        graphType.Fields.Find("Field9").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesOutputNameAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        graphType.Fields.Find("OutputField10").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesIgnoreAttribute()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
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
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
        fieldType.Type.ShouldBe(expectedGraphType);
    }

    [Fact]
    public void Field_RemovesAsyncSuffix()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("TaskIntField").ShouldNotBeNull();
    }

    [Fact]
    public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
    {
        var provider = new DefaultServiceProvider();
        provider.GetService(typeof(AutoRegisteringObjectGraphType<TestClass>)).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(ArgumentTests.WithNonNullString), "arg1", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTests.WithNullableString), "arg1", typeof(GraphQLClrInputTypeReference<string>))]
    [InlineData(nameof(ArgumentTests.WithDefaultString), "arg1", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTests.WithNullableDefaultString), "arg1", typeof(GraphQLClrInputTypeReference<string>))]
    [InlineData(nameof(ArgumentTests.WithCancellationToken), "cancellationToken", null)]
    [InlineData(nameof(ArgumentTests.WithResolveFieldContext), "context", null)]
    [InlineData(nameof(ArgumentTests.WithFromServices), "arg1", null)]
    [InlineData(nameof(ArgumentTests.NamedArg), "arg1", null)]
    [InlineData(nameof(ArgumentTests.NamedArg), "arg1rename", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTests.IdArg), "arg1", typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(ArgumentTests.TypedArg), "arg1", typeof(StringGraphType))]
    [InlineData(nameof(ArgumentTests.MultipleArgs), "arg1", typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(ArgumentTests.MultipleArgs), "arg2", typeof(NonNullGraphType<GraphQLClrInputTypeReference<int>>))]
    public void Argument_VerifyType(string fieldName, string argumentName, Type? argumentGraphType)
    {
        var graphType = new AutoRegisteringObjectGraphType<ArgumentTests>();
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
    [InlineData(nameof(ArgumentTests.WithNonNullString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.WithNullableString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.WithNullableString), "arg1", null, null, null)]
    [InlineData(nameof(ArgumentTests.WithNullableString), null, null, null, null)]
    [InlineData(nameof(ArgumentTests.WithDefaultString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.WithDefaultString), "arg1", null, null, null)]
    [InlineData(nameof(ArgumentTests.WithDefaultString), null, null, null, "test")]
    [InlineData(nameof(ArgumentTests.WithCancellationToken), null, null, null, true)]
    [InlineData(nameof(ArgumentTests.WithFromServices), null, null, null, "testService")]
    [InlineData(nameof(ArgumentTests.NamedArg), "arg1rename", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.IdArg), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.IdIntArg), "arg1", "123", null, 123)]
    [InlineData(nameof(ArgumentTests.IdIntArg), "arg1", 123, null, 123)]
    [InlineData(nameof(ArgumentTests.TypedArg), "arg1", "123", null, 123)]
    [InlineData(nameof(ArgumentTests.MultipleArgs), "arg1", "hello", 123, "hello123")]
    public async Task Argument_ResolverTests_WithNonNullString(string fieldName, string arg1Name, object? arg1Value, int? arg2Value, object? expected)
    {
        var graphType = new AutoRegisteringObjectGraphType<ArgumentTests>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            CancellationToken = cts.Token,
            Source = new ArgumentTests(),
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
        var outputType = new AutoRegisteringObjectGraphType<TestClass>();
        outputType.Fields.Find("Field1").ShouldNotBeNull();
        outputType.Fields.Find("Field2").ShouldNotBeNull();
        outputType.Fields.Find("Field3").ShouldBeNull();
        outputType.Fields.Find("Field4").ShouldNotBeNull();
        outputType.Fields.Find("Field5").ShouldBeNull();
    }

    [Fact]
    public void SkipsSpecifiedProperties()
    {
        var outputType = new AutoRegisteringObjectGraphType<TestClass>(x => x.Field1);
        outputType.Fields.Find("Field1").ShouldBeNull();
        outputType.Fields.Find("Field2").ShouldNotBeNull();
        outputType.Fields.Find("Field3").ShouldBeNull();
        outputType.Fields.Find("Field4").ShouldNotBeNull();
        outputType.Fields.Find("Field5").ShouldBeNull();
    }

    [Fact]
    public void CanOverrideFieldGeneration()
    {
        var graph = new TestChangingName<TestClass>();
        graph.Fields.Find("Field1Prop").ShouldNotBeNull();
    }

    [Fact]
    public void CanOverrideFieldList()
    {
        var graph = new TestChangingFieldList<TestClass>();
        graph.Fields.Count.ShouldBe(1);
        graph.Fields.Find("Field1").ShouldNotBeNull();
    }

    [Fact]
    public void CanAddFieldInfos()
    {
        var graph = new TestFieldSupport<TestClass>();
        graph.Fields.Find("Field5").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Field1", 1)]
    [InlineData("Field2", 2)]
    [InlineData("Field4", 4)]
    [InlineData("Field5", 5)]
    [InlineData("Field6AltName", 6)]
    [InlineData("Field7", 7)]
    public async Task FieldResolversWork(string fieldName, object expected)
    {
        var graph = new TestFieldSupport<TestClass>();
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
        var graphType = new TestFieldSupport<NoDefaultConstructorTest>();
        var context = new ResolveFieldContext
        {
            Source = new NoDefaultConstructorTest(true)
        };
        (await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(true);
        (await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe("test");
        (await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(1);
    }

    [Fact]
    public async Task ThrowsWhenSourceNull()
    {
        var graphType = new TestFieldSupport<NullSourceFailureTest>();
        var context = new ResolveFieldContext();
        (await Should.ThrowAsync<NullReferenceException>(async () => await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<NullReferenceException>(async () => await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<NullReferenceException>(async () => await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
    }

    [Fact]
    public async Task ThrowsWhenSourceNull_Struct()
    {
        var graphType = new TestFieldSupport<NullSourceStructFailureTest>();
        var context = new ResolveFieldContext();
        (await Should.ThrowAsync<NullReferenceException>(async () => await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<NullReferenceException>(async () => await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<NullReferenceException>(async () => await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ConfigureAwait(false))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
    }

    [Fact]
    public async Task WorksWithNullSource()
    {
        var graphType = new TestFieldSupport<NullSourceTest>();
        var context = new ResolveFieldContext();
        (await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(true);
        (await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe("test");
        (await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(3);
    }

    [Fact]
    public void TestExceptionBubbling()
    {
        Should.Throw<Exception>(() => new AutoRegisteringObjectGraphType<TestExceptionBubblingClass>()).Message.ShouldBe("Test");
    }

    [Fact]
    public void TestBasicClassNoExtraFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestBasicClass>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void TestBasicRecordNoExtraFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestBasicRecord>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void TestBasicRecordStructNoExtraFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestBasicRecordStruct>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public async Task TestStruct()
    {
        var graphType = new TestFieldSupport<TestStructModel>();
        graphType.Name.ShouldBe("Test");
        graphType.Fields.Count.ShouldBe(3);
        var context = new ResolveFieldContext
        {
            Source = new TestStructModel(),
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "prefix", new ArgumentValue("test", ArgumentSource.Literal) },
            },
        };
        (await graphType.Fields.Find("Id").ShouldNotBeNull().Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe(1);
        graphType.Fields.Find("Id")!.Type.ShouldBe(typeof(NonNullGraphType<IdGraphType>));
        (await graphType.Fields.Find("Name").ShouldNotBeNull().Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe("Example");
        (await graphType.Fields.Find("IdAndName").ShouldNotBeNull().Resolver!.ResolveAsync(context).ConfigureAwait(false)).ShouldBe("testExample1");
    }

    [Fact]
    public async Task CustomHardcodedArgumentAttributesWork()
    {
        var graphType = new AutoRegisteringObjectGraphType<CustomHardcodedArgumentAttributeTestClass>();
        var fieldType = graphType.Fields.Find(nameof(CustomHardcodedArgumentAttributeTestClass.FieldWithHardcodedValue))!;
        var resolver = fieldType.Resolver!;
        (await resolver.ResolveAsync(new ResolveFieldContext
        {
            Source = new CustomHardcodedArgumentAttributeTestClass(),
        }).ConfigureAwait(false)).ShouldBe("85");
    }

    private class CustomHardcodedArgumentAttributeTestClass
    {
        public string FieldWithHardcodedValue([HardcodedValue] int value) => value.ToString();
    }

    private class HardcodedValueAttribute : GraphQLAttribute
    {
        public override void Modify(ArgumentInformation argumentInformation)
            => argumentInformation.SetDelegate(context => 85);
    }

    private class NoDefaultConstructorTest
    {
        public NoDefaultConstructorTest(bool value)
        {
            Example1 = value;
        }

        public bool Example1 { get; set; }
        public string Example2() => "test";
        public int Example3 = 1;
    }

    private class NullSourceTest
    {
        public static bool Example1 { get; set; } = true;
        public static string Example2() => "test";
        public static int Example3 = 3;
    }

    private class NullSourceFailureTest
    {
        public bool Example1 { get; set; } = true;
        public string Example2() => "test";
        public int Example3 = 3;
    }

    private struct NullSourceStructFailureTest
    {
        public NullSourceStructFailureTest() { }
        public bool Example1 { get; set; } = true;
        public string Example2() => "test";
        public int Example3 = 3;
    }

    private class FieldTests
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
        [InputType(typeof(IdGraphType))]
        public int? Field6 { get; set; }
        [OutputType(typeof(IdGraphType))]
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
        public string NotNullStringField { get; set; } = null!;
        public string? NullableStringField { get; set; }
        public string NotNullStringGetOnlyField => null!;
        public string? NullableStringGetOnlyField => null!;
        public List<string?> NotNullListNullableStringField { get; set; } = null!;
        public List<string> NotNullListNotNullStringField { get; set; } = null!;
        public List<string?>? NullableListNullableStringField { get; set; }
        public List<string>? NullableListNotNullStringField { get; set; }
        public IEnumerable<int?> NotNullEnumerableNullableIntField { get; set; } = null!;
        public IEnumerable<int> NotNullEnumerableNotNullIntField { get; set; } = null!;
        public IEnumerable<int?>? NullableEnumerableNullableIntField { get; set; }
        public IEnumerable<int>? NullableEnumerableNotNullIntField { get; set; }
        public Tuple<int, string>?[] NotNullArrayNullableTupleField { get; set; } = null!;
        public Tuple<int, string>[] NotNullArrayNotNullTupleField { get; set; } = null!;
        public Tuple<int, string>?[]? NullableArrayNullableTupleField { get; set; }
        public Tuple<int, string>[]? NullableArrayNotNullTupleField { get; set; }
        [Id]
        public int IdField { get; set; }
        [Id]
        public int? NullableIdField { get; set; }
        public IEnumerable EnumerableField { get; set; } = null!;
        public ICollection CollectionField { get; set; } = null!;
        public IEnumerable? NullableEnumerableField { get; set; }
        public ICollection? NullableCollectionField { get; set; }
        public int?[]?[]? ListOfListOfIntsField { get; set; }
        public Task<string> TaskStringField() => null!;
        public Task<int> TaskIntFieldAsync() => Task.FromResult(0);
        public IDataLoaderResult<string?> DataLoaderNullableStringField() => null!;
        public IDataLoaderResult<string>? NullableDataLoaderStringField() => null!;
        public Task<IDataLoaderResult<string?[]>> TaskDataLoaderStringArrayField() => null!;
    }

    private class ArgumentTests
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

    private class TestChangingFieldList<T> : AutoRegisteringObjectGraphType<T>
    {
        protected override IEnumerable<FieldType> ProvideFields()
        {
            yield return CreateField(GetRegisteredMembers().First(x => x.Name == "Field1"))!;
        }
    }

    private class TestFieldSupport<T> : AutoRegisteringObjectGraphType<T>
    {
        protected override IEnumerable<MemberInfo> GetRegisteredMembers()
            => base.GetRegisteredMembers().Concat(typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public));
    }

    private class TestChangingName<T> : AutoRegisteringObjectGraphType<T>
    {
        protected override FieldType CreateField(MemberInfo memberInfo)
        {
            var field = base.CreateField(memberInfo)!;
            field.Name += "Prop";
            return field;
        }
    }

    private class TestClass
    {
        public int Field1 { get; set; } = 1;
        public int Field2 => 2;
        public int Field3 { set { } }
        public int Field4() => 4;
        public int Field5 = 5;
        [Name("Field6AltName")]
        public int Field6 => 6;
        public Task<int> Field7 => Task.FromResult(7);
    }

    [Name("TestWithCustomName")]
    private class TestClass_WithCustomName { }

    [InputName("TestWithCustomName")]
    private class TestClass_WithCustomInputName { }

    [OutputName("TestWithCustomName")]
    private class TestClass_WithCustomOutputName { }

    [Description("Test description")]
    private class TestClass_WithCustomDescription { }

    [Obsolete("Test deprecation reason")]
    private class TestClass_WithCustomDeprecationReason { }

    [CustomDescription]
    private class TestClass_WithCustomAttributes { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
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
    private class TestClass_WithMultipleAttributes { }

    private class TestOverrideDefaultName<T> : AutoRegisteringInputObjectGraphType<T>
    {
        protected override void ConfigureGraph()
        {
            Name = typeof(T).Name + "Input";
            base.ConfigureGraph();
        }
    }

    private class ParentClass
    {
        [Name("Field1CustomName")]
        public virtual string? Field1 { get; set; }
    }
    private class DerivedClass : ParentClass
    {
        public override string? Field1 { get => base.Field1; set => base.Field1 = value; }
    }

    private class TestExceptionBubblingClass
    {
        public string Test([TestExceptionBubbling] string arg) => arg;
    }

    private class TestExceptionBubblingAttribute : GraphQLAttribute
    {
        public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        {
            throw new Exception("Test");
        }
    }

    private record TestBasicRecord(int Id, string Name);

    private record struct TestBasicRecordStruct(int Id, string Name);

    private class TestBasicClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    [Name("Test")]
    public struct TestStructModel
    {
        public TestStructModel() { }
        [Id]
        public int Id { get; set; } = 1;
        public static string Name = "Example";
        [Name("IdAndName")]
        public string IdName(string prefix) => prefix + Name + Id.ToString();
    }
}
