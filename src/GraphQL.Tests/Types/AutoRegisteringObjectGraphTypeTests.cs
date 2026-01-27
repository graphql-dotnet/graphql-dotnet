using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0414 // Field is assigned but its value is never used

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
        graphType.Name.ShouldBe("TestClassOutput");
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
    [InlineData("ValueTaskStringField", typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData("AsyncEnumerableIntField", typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
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
        graphType.Fields.Find("TaskIntField").ShouldNotBeNull();
        graphType.Fields.Find("ValueTaskStringField").ShouldNotBeNull();
        graphType.Fields.Find("AsyncEnumerableIntField").ShouldNotBeNull();
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

    [Fact]
    public async Task DefaultValueIsCoerced()
    {
        var schema = new Schema
        {
            Query = new AutoRegisteringObjectGraphType<ArgumentTests>()
        };
        schema.Initialize();
        var result = await schema.ExecuteAsync(o =>
        {
            o.Root = new ArgumentTests();
            o.Query = """
                query {
                    withDefaultString
                }
                """;
        });
        result.ShouldBeSimilarTo("""
            {"data":{"withDefaultString":"test"}}
            """);
    }

    [Theory]
    [InlineData(nameof(ArgumentTests.WithNonNullString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.WithNullableString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.WithNullableString), "arg1", null, null, null)]
    [InlineData(nameof(ArgumentTests.WithNullableString), null, null, null, null)]
    [InlineData(nameof(ArgumentTests.WithDefaultString), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.WithDefaultString), "arg1", null, null, null)]
    //[InlineData(nameof(ArgumentTests.WithDefaultString), null, null, null, "test")] //cannot occur -- TryGetArgumentExact returns true for args with default values
    [InlineData(nameof(ArgumentTests.WithCancellationToken), null, null, null, true)]
    [InlineData(nameof(ArgumentTests.WithFromServices), null, null, null, "testService")]
    [InlineData(nameof(ArgumentTests.NamedArg), "arg1rename", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.IdArg), "arg1", "hello", null, "hello")]
    [InlineData(nameof(ArgumentTests.IdIntArg), "arg1", "123", null, 123)]
    [InlineData(nameof(ArgumentTests.IdIntArg), "arg1", 123, null, 123)]
    [InlineData(nameof(ArgumentTests.TypedArg), "arg1", "123", null, 123)]
    [InlineData(nameof(ArgumentTests.MultipleArgs), "arg1", "hello", 123, "hello123")]
    public async Task Argument_ResolverTests(string fieldName, string? arg1Name, object? arg1Value, int? arg2Value, object? expected)
    {
        var graphType = new AutoRegisteringObjectGraphType<ArgumentTests>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();

        // initialize schema
        var services = new ServiceCollection();
        services
            .AddSingleton("testService")
            .AddGraphQL(b => b
                .AddAutoSchema<Class1>()
                .ConfigureSchema(b => b.RegisterType(graphType)));
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ISchema>().Initialize();

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            CancellationToken = cts.Token,
            Source = new ArgumentTests(),
            FieldDefinition = fieldType,
            RequestServices = provider,
            Schema = provider.GetRequiredService<ISchema>(),
        };
        if (arg1Name != null)
        {
            context.Arguments.Add(arg1Name, new ArgumentValue(arg1Value, ArgumentSource.Variable));
        }
        if (arg2Value.HasValue)
        {
            context.Arguments.Add("arg2", new ArgumentValue(arg2Value, ArgumentSource.Variable));
        }
        fieldType.Resolver.ShouldNotBeNull();
        (await fieldType.Resolver!.ResolveAsync(context)).ShouldBe(expected);
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
        object? actual = await resolver.ResolveAsync(new ResolveFieldContext
        {
            Source = obj,
            FieldDefinition = new FieldType { Name = fieldName },
        });
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
        (await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context)).ShouldBe(true);
        (await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context)).ShouldBe("test");
        (await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task ThrowsWhenSourceNull()
    {
        var graphType = new TestFieldSupport<NullSourceFailureTest>();
        var context = new ResolveFieldContext();
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context)))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context)))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context)))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
    }

    [Fact]
    public async Task ThrowsWhenSourceNull_Struct()
    {
        var graphType = new TestFieldSupport<NullSourceStructFailureTest>();
        var context = new ResolveFieldContext();
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context)))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context)))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
        (await Should.ThrowAsync<InvalidOperationException>(async () => await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context)))
            .Message.ShouldBe("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
    }

    [Fact]
    public async Task WorksWithNullSource()
    {
        var graphType = new TestFieldSupport<NullSourceTest>();
        var context = new ResolveFieldContext();
        (await graphType.Fields.Find("Example1")!.Resolver!.ResolveAsync(context)).ShouldBe(true);
        (await graphType.Fields.Find("Example2")!.Resolver!.ResolveAsync(context)).ShouldBe("test");
        (await graphType.Fields.Find("Example3")!.Resolver!.ResolveAsync(context)).ShouldBe(3);
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
    public void TestInheritedRecordWithOverridesNoExtraFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestInheritedRecordWithOverrides>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Find("Description").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(3);
    }

    [Fact]
    public void TestInheritedRecordNoExtraFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<TestInheritedRecord>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Find("Description").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(3);
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

        // initialize graph type
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Class1>()
            .ConfigureSchema(s => s.RegisterType(graphType)));
        services.BuildServiceProvider().GetRequiredService<ISchema>().Initialize();

        var context = new ResolveFieldContext
        {
            Source = new TestStructModel(),
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "prefix", new ArgumentValue("test", ArgumentSource.Literal) },
            },
            FieldDefinition = graphType.Fields.Find("id")!,
        };
        (await graphType.Fields.Find("id").ShouldNotBeNull().Resolver!.ResolveAsync(context)).ShouldBe(1);
        graphType.Fields.Find("id")!.Type.ShouldBe(typeof(NonNullGraphType<IdGraphType>));
        context.FieldDefinition = graphType.Fields.Find("name")!;
        (await graphType.Fields.Find("name").ShouldNotBeNull().Resolver!.ResolveAsync(context)).ShouldBe("Example");
        context.FieldDefinition = graphType.Fields.Find("idAndName")!;
        (await graphType.Fields.Find("idAndName").ShouldNotBeNull().Resolver!.ResolveAsync(context)).ShouldBe("testExample1");
    }

    public class Class1
    {
        public string? Test { get; set; }
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
        })).ShouldBe("85");
    }

    [Fact]
    public void TestInheritedFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<InheritanceTests>();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
        graphType.Fields.Find("Field3").ShouldNotBeNull();
        graphType.Fields.Find("Field4").ShouldNotBeNull();
        graphType.Fields.Find("Field5").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(5);
    }

    [Fact]
    public void CannotBuildWithoutMemberInstanceExpression()
    {
        Should.Throw<ArgumentNullException>(() => new NoMemberInstanceExpression<TestBasicClass>())
            .ParamName.ShouldBe("instanceExpression");
    }

    [Fact]
    public async Task ParserOnArgumentsSetProperly()
    {
        var queryType = new AutoRegisteringObjectGraphType<Class3>();
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        // verify that during input coercion, the value is converted to an integer
        var fieldType = queryType.Fields.First();
        var argument = fieldType.Arguments.ShouldNotBeNull().First();
        argument.ResolvedType.ShouldBeOfType<NonNullGraphType>().ResolvedType.ShouldBeOfType<IdGraphType>();
        argument.Parser.ShouldNotBeNull().Invoke("123", schema.ValueConverter).ShouldBe(123);
        // verify that during input coercion, parsing errors throw an exception
        Should.Throw<FormatException>(() => argument.Parser.ShouldNotBeNull().Invoke("abc", schema.ValueConverter));
        // perform end-to-end test for bad argument
        var result = await new DocumentExecuter().ExecuteAsync(o =>
        {
            o.Schema = schema;
            o.Query = """{ testMe(id: "abc") }""";
        });
        result.Executed.ShouldBeFalse();
        var resultJson = new SystemTextJson.GraphQLSerializer().Serialize(result);
#if NET7_0_OR_GREATER
        resultJson.ShouldBe("""{"errors":[{"message":"Invalid value for argument \u0027id\u0027 of field \u0027testMe\u0027. The input string \u0027abc\u0027 was not in a correct format.","locations":[{"line":1,"column":14}],"extensions":{"code":"INVALID_VALUE","codes":["INVALID_VALUE","FORMAT"],"number":"5.6"}}]}""");
#else
        resultJson.ShouldBe("""{"errors":[{"message":"Invalid value for argument \u0027id\u0027 of field \u0027testMe\u0027. Input string was not in a correct format.","locations":[{"line":1,"column":14}],"extensions":{"code":"INVALID_VALUE","codes":["INVALID_VALUE","FORMAT"],"number":"5.6"}}]}""");
#endif
    }

    private class Class3
    {
        public static int TestMe([Id] int id) => id;
    }

    [Fact]
    public async Task ValidatorOnArgumentsSetProperly()
    {
        var queryType = new AutoRegisteringObjectGraphType<Class4>();
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        // verify that during input coercion, the value is validated
        var fieldType = queryType.Fields.First();
        var argument = fieldType.Arguments.ShouldNotBeNull().First();
        argument.ResolvedType.ShouldBeOfType<NonNullGraphType>().ResolvedType.ShouldBeOfType<StringGraphType>();
        argument.Validator.ShouldNotBeNull().Invoke("abc");
        // verify that during input coercion, parsing errors throw an exception
        Should.Throw<ArgumentException>(() => argument.Validator("abcdef"));
        // perform end-to-end test for bad argument
        var result = await new DocumentExecuter().ExecuteAsync(o =>
        {
            o.Schema = schema;
            o.Query = """{ testMe(value: "abcdef") }""";
        });
        result.Executed.ShouldBeFalse();
        var resultJson = new SystemTextJson.GraphQLSerializer().Serialize(result);
        resultJson.ShouldBe("""{"errors":[{"message":"Invalid value for argument \u0027value\u0027 of field \u0027testMe\u0027. Value is too long. Max length is 5.","locations":[{"line":1,"column":17}],"extensions":{"code":"INVALID_VALUE","codes":["INVALID_VALUE","ARGUMENT"],"number":"5.6"}}]}""");
    }

    private class Class4
    {
        public static string TestMe([MyMaxLength(5)] string value) => value;
    }

    private class MyMaxLength : GraphQLAttribute
    {
        private readonly int _maxLength;
        public MyMaxLength(int maxLength)
        {
            _maxLength = maxLength;
        }

        public override void Modify(ArgumentInformation argumentInformation)
        {
            if (argumentInformation.TypeInformation.Type != typeof(string))
            {
                throw new InvalidOperationException("MyMaxLength can only be used on string arguments.");
            }
        }

        public override void Modify(QueryArgument queryArgument)
        {
            queryArgument.Validate(value =>
            {
                if (((string)value).Length > _maxLength)
                {
                    throw new ArgumentException($"Value is too long. Max length is {_maxLength}.");
                }
            });
        }
    }

    public class NoMemberInstanceExpression<T> : AutoRegisteringObjectGraphType<T>
    {
        protected override LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo) => null!;
    }

    private class CustomHardcodedArgumentAttributeTestClass
    {
        public string FieldWithHardcodedValue([HardcodedValue] int value) => value.ToString();
    }

    private class HardcodedValueAttribute : ParameterAttribute<int>
    {
        public override Func<IResolveFieldContext, int> GetResolver(ArgumentInformation argumentInformation)
            => context => 85;
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
        public ValueTask<string> ValueTaskStringFieldAsync() => default;
        public IAsyncEnumerable<int> AsyncEnumerableIntFieldAsync() => null!;
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
        public int TypedArg(
#if NET48
        [InputType(typeof(StringGraphType))]
#else
        [InputType<StringGraphType>()]
#endif
        int arg1) => arg1;
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
        public int Field3 { private get => 123; set { } }
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

    private class TestOverrideDefaultName<T> : AutoRegisteringObjectGraphType<T>
    {
        protected override void ConfigureGraph()
        {
            Name = typeof(T).Name + "Output";
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
        public override void Modify(ArgumentInformation argumentInformation)
        {
            throw new Exception("Test");
        }
    }

    private record TestBasicRecord(int Id, string Name);

    private record TestInheritedRecord : TestBasicRecord
    {
        public string Description { get; init; }
        public TestInheritedRecord(int Id, string Name, string Description) : base(Id, Name)
        {
            this.Description = Description;
        }
    }

    private record TestInheritedRecordWithOverrides : TestInheritedRecord
    {
        public TestInheritedRecordWithOverrides(int Id, string Name, string Description) : base(Id, Name, Description)
        {
        }
        protected override bool PrintMembers(StringBuilder builder) => base.PrintMembers(builder);
        public override string ToString() => base.ToString();
        public override int GetHashCode() => base.GetHashCode();
        protected override Type EqualityContract => base.EqualityContract;
    }

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

    public class InheritanceTestsParent
    {
        public int Field1 => 1;
        public int Field2() => 2;
        public virtual int Field3() => 3;
    }

    public class InheritanceTests : InheritanceTestsParent
    {
        public int Field4 => 4;
        public int Field5() => 5;
        public override int Field3() => 3;
        public override int GetHashCode() => 123;
    }

    [Fact]
    public async Task InstanceSourceAttribute_ContextSource_WorksLikeDefault()
    {
        var graphType = new AutoRegisteringObjectGraphType<ContextSourceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            Source = new ContextSourceClass { Value = "test" },
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("test");
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetServiceOrCreateInstance_GetsFromDI()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new GetServiceOrCreateInstanceClass { Value = "from-di" });
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetServiceOrCreateInstanceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("from-di");
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetServiceOrCreateInstance_CreatesInstanceWhenNotInDI()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetServiceOrCreateInstanceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("default");
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetServiceOrCreateInstance_UsesConstructorInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetServiceOrCreateInstanceWithDependencyClass>();
        var fieldType = graphType.Fields.Find("GetValue")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("injected");
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetRequiredService_GetsFromDI()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new GetRequiredServiceClass { Value = "from-di" });
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetRequiredServiceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("from-di");
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetRequiredService_ThrowsWhenNotInDI()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetRequiredServiceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await fieldType.Resolver!.ResolveAsync(context));
        ex.Message.ShouldContain("GetRequiredServiceClass");
    }

    [Fact]
    public async Task InstanceSourceAttribute_NewInstance_CreatesNewInstanceEachTime()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new NewInstanceClass { Value = "from-di" });
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<NewInstanceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        // Even though it's registered in DI, NewInstance should create a new instance
        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("default");
    }

    [Fact]
    public async Task InstanceSourceAttribute_NewInstance_UsesConstructorInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<NewInstanceWithDependencyClass>();
        var fieldType = graphType.Fields.Find("GetValue")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe("injected");
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetServiceOrCreateInstance_InjectsIServiceProvider()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetServiceOrCreateInstanceWithIServiceProviderClass>();
        var fieldType = graphType.Fields.Find("GetValue")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe(serviceProvider);
    }

    [Fact]
    public async Task InstanceSourceAttribute_GetServiceOrCreateInstance_InjectsIResolveFieldContext()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<GetServiceOrCreateInstanceWithIResolveFieldContextClass>();
        var fieldType = graphType.Fields.Find("GetValue")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe(context);
    }

    [Fact]
    public async Task InstanceSourceAttribute_NewInstance_InjectsIServiceProvider()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<NewInstanceWithIServiceProviderClass>();
        var fieldType = graphType.Fields.Find("GetValue")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task InstanceSourceAttribute_NewInstance_InjectsIResolveFieldContext()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var graphType = new AutoRegisteringObjectGraphType<NewInstanceWithIResolveFieldContextClass>();
        var fieldType = graphType.Fields.Find("GetValue")!;

        var context = new ResolveFieldContext
        {
            RequestServices = serviceProvider,
            FieldDefinition = fieldType,
        };

        var result = await fieldType.Resolver!.ResolveAsync(context);
        result.ShouldBe(context);
    }

    [Fact]
    public async Task InstanceSourceAttribute_ThrowsWhenRequestServicesNotAvailable()
    {
        var graphType = new AutoRegisteringObjectGraphType<GetServiceOrCreateInstanceClass>();
        var fieldType = graphType.Fields.Find("Value")!;

        var context = new ResolveFieldContext
        {
            FieldDefinition = fieldType,
        };

        await Should.ThrowAsync<MissingRequestServicesException>(
            async () => await fieldType.Resolver!.ResolveAsync(context));
    }

    [InstanceSource(InstanceSource.ContextSource)]
    private class ContextSourceClass
    {
        public string Value { get; set; } = "default";
    }

    [InstanceSource(InstanceSource.GetServiceOrCreateInstance)]
    private class GetServiceOrCreateInstanceClass
    {
        public string Value { get; set; } = "default";
    }

    [InstanceSource(InstanceSource.GetServiceOrCreateInstance)]
    private class GetServiceOrCreateInstanceWithDependencyClass
    {
        private readonly string _dependency;

        public GetServiceOrCreateInstanceWithDependencyClass(string dependency)
        {
            _dependency = dependency;
        }

        public string GetValue() => _dependency;
    }

    [InstanceSource(InstanceSource.GetRequiredService)]
    private class GetRequiredServiceClass
    {
        public string Value { get; set; } = "default";
    }

    [InstanceSource(InstanceSource.NewInstance)]
    private class NewInstanceClass
    {
        public string Value { get; set; } = "default";
    }

    [InstanceSource(InstanceSource.NewInstance)]
    private class NewInstanceWithDependencyClass
    {
        private readonly string _dependency;

        public NewInstanceWithDependencyClass(string dependency)
        {
            _dependency = dependency;
        }

        public string GetValue() => _dependency;
    }

    [InstanceSource(InstanceSource.GetServiceOrCreateInstance)]
    private class GetServiceOrCreateInstanceWithIServiceProviderClass
    {
        private readonly IServiceProvider _serviceProvider;

        public GetServiceOrCreateInstanceWithIServiceProviderClass(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceProvider GetValue() => _serviceProvider;
    }

    [InstanceSource(InstanceSource.GetServiceOrCreateInstance)]
    private class GetServiceOrCreateInstanceWithIResolveFieldContextClass
    {
        private readonly IResolveFieldContext _context;

        public GetServiceOrCreateInstanceWithIResolveFieldContextClass(IResolveFieldContext context)
        {
            _context = context;
        }

        public IResolveFieldContext GetValue() => _context;
    }

    [InstanceSource(InstanceSource.NewInstance)]
    private class NewInstanceWithIServiceProviderClass
    {
        private readonly IServiceProvider _serviceProvider;

        public NewInstanceWithIServiceProviderClass(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceProvider GetValue() => _serviceProvider;
    }

    [InstanceSource(InstanceSource.NewInstance)]
    private class NewInstanceWithIResolveFieldContextClass
    {
        private readonly IResolveFieldContext _context;

        public NewInstanceWithIResolveFieldContextClass(IResolveFieldContext context)
        {
            _context = context;
        }

        public IResolveFieldContext GetValue() => _context;
    }

    [Fact]
    public void MemberScanAttribute_PropertiesOnly()
    {
        var graphType = new AutoRegisteringObjectGraphType<PropertiesOnlyClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_FieldsOnly()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldsOnlyClass>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_MethodsOnly()
    {
        var graphType = new AutoRegisteringObjectGraphType<MethodsOnlyClass>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldNotBeNull();
        graphType.Fields.Find("Method2").ShouldNotBeNull();
    }

    [Fact]
    public void MemberScanAttribute_PropertiesAndFields()
    {
        var graphType = new AutoRegisteringObjectGraphType<PropertiesAndFieldsClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_None()
    {
        var graphType = new AutoRegisteringObjectGraphType<NoneClass>();
        graphType.Fields.Count.ShouldBe(0);
    }

    [Fact]
    public void MemberScanAttribute_AllCombined()
    {
        var graphType = new AutoRegisteringObjectGraphType<AllMembersClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Method1").ShouldNotBeNull();
    }

    [Fact]
    public void MemberScanAttribute_Inheritance()
    {
        var graphType = new AutoRegisteringObjectGraphType<DerivedPropertiesOnlyClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_Struct_PropertiesOnly()
    {
        var graphType = new AutoRegisteringObjectGraphType<PropertiesOnlyStruct>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_Struct_FieldsOnly()
    {
        var graphType = new AutoRegisteringObjectGraphType<FieldsOnlyStruct>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
        graphType.Fields.Find("Method1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_Struct_MethodsOnly()
    {
        var graphType = new AutoRegisteringObjectGraphType<MethodsOnlyStruct>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
        graphType.Fields.Find("Method1").ShouldNotBeNull();
        graphType.Fields.Find("Method2").ShouldNotBeNull();
    }

    [MemberScan(ScanMemberTypes.Properties)]
    private class PropertiesOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Property2 { get; set; } = "prop2";
        public string Field1 = "field1";
        public string Method1() => "method1";
    }

    [MemberScan(ScanMemberTypes.Fields)]
    private class FieldsOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Field2 = "field2";
        public string Method1() => "method1";
    }

    [MemberScan(ScanMemberTypes.Methods)]
    private class MethodsOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Method1() => "method1";
        public string Method2() => "method2";
    }

    [MemberScan(ScanMemberTypes.Properties | ScanMemberTypes.Fields)]
    private class PropertiesAndFieldsClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Method1() => "method1";
    }

    [MemberScan(0)]
    private class NoneClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Method1() => "method1";
    }

    [MemberScan(ScanMemberTypes.Properties | ScanMemberTypes.Fields | ScanMemberTypes.Methods)]
    private class AllMembersClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Method1() => "method1";
    }

    [MemberScan(ScanMemberTypes.Properties)]
    private class BasePropertiesOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Method1() => "method1";
    }

    private class DerivedPropertiesOnlyClass : BasePropertiesOnlyClass
    {
        public string Property2 { get; set; } = "prop2";
    }

    [MemberScan(ScanMemberTypes.Properties)]
    private struct PropertiesOnlyStruct
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        public string Field1;
        public string Method1() => "method1";

        public PropertiesOnlyStruct()
        {
            Property1 = "prop1";
            Property2 = "prop2";
            Field1 = "field1";
        }
    }

    [MemberScan(ScanMemberTypes.Fields)]
    private struct FieldsOnlyStruct
    {
        public string Property1 { get; set; }
        public string Field1;
        public string Field2;
        public string Method1() => "method1";

        public FieldsOnlyStruct()
        {
            Property1 = "prop1";
            Field1 = "field1";
            Field2 = "field2";
        }
    }

    [MemberScan(ScanMemberTypes.Methods)]
    private struct MethodsOnlyStruct
    {
        public string Property1 { get; set; }
        public string Field1;
        public string Method1() => "method1";
        public string Method2() => "method2";

        public MethodsOnlyStruct()
        {
            Property1 = "prop1";
            Field1 = "field1";
        }
    }
}
