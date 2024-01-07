using System.Collections.ObjectModel;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.Tests.Builders;

public class FieldBuilderTests
{
    [Fact]
    public void should_have_name()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("TheName");

        var fields = objectType.Fields.ToList();
        fields.Count.ShouldBe(1);
        fields[0].Name.ShouldBe("TheName");
        fields[0].Description.ShouldBe(null);
        fields[0].Type.ShouldBe(typeof(StringGraphType));
    }

    [Fact]
    public void should_have_name_and_description()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("TheName")
            .Description("TheDescription");

        var fields = objectType.Fields.ToList();
        fields.Count.ShouldBe(1);
        fields[0].Name.ShouldBe("TheName");
        fields[0].Description.ShouldBe("TheDescription");
        fields[0].Type.ShouldBe(typeof(StringGraphType));
    }

    [Fact]
    public void should_have_deprecation_reason()
    {
        var objectType = new ObjectGraphType();

        objectType.Field<StringGraphType>("_")
            .DeprecationReason("Old field");

        objectType.Fields
            .First().DeprecationReason.ShouldBe("Old field");
    }

    [Fact]
    public async Task should_return_the_right_type()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("TheName")
            .Returns<string>()
            .Resolve(_ => "SomeString");

        var fields = objectType.Fields.ToList();
        fields.Count.ShouldBe(1);
        fields[0].Name.ShouldBe("TheName");
        fields[0].Type.ShouldBe(typeof(StringGraphType));

        var context = new ResolveFieldContext();
        object? result = await fields[0].Resolver.ShouldNotBeNull().ResolveAsync(context);
        result.ShouldNotBeNull().GetType().ShouldBe(typeof(string));
        result.ShouldBe("SomeString");
    }

    [Fact]
    public void can_be_defined_of_graphtype()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<IntGraphType>("Field1");
        objectType.Field<FloatGraphType>("Field2");
        objectType.Field<DateGraphType>("Field3");

        var fields = objectType.Fields.ToList();
        fields.Count.ShouldBe(3);
        fields[0].Name.ShouldBe("Field1");
        fields[0].Type.ShouldBe(typeof(IntGraphType));
        fields[1].Name.ShouldBe("Field2");
        fields[1].Type.ShouldBe(typeof(FloatGraphType));
        fields[2].Name.ShouldBe("Field3");
        fields[2].Type.ShouldBe(typeof(DateGraphType));
    }

    [Fact]
    public void can_have_a_default_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<IntGraphType>("_")
            .Returns<int>()
            .DefaultValue(15);

        objectType.Fields.First().DefaultValue.ShouldBe(15);
    }

    [Fact]
    public void can_have_arguments_with_and_without_default_values_and_with_metadata()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<IntGraphType>("_")
            .Argument<StringGraphType>("arg1", "desc1", arg => arg.DefaultValue = "12345")
            .Argument<IntGraphType>("arg2", "desc2", arg => arg.DefaultValue = 9)
            .Argument<IntGraphType>("arg3", "desc3", cfg => cfg.WithMetadata("secure", true))
            .Argument<BooleanGraphType>("arg4", cfg => cfg.WithMetadata("useBefore", new DateTime(2030, 1, 2)).DefaultValue = true);

        var field = objectType.Fields.First();
        field.Arguments.ShouldNotBeNull().Count.ShouldBe(4);

        field.Arguments[0].Name.ShouldBe("arg1");
        field.Arguments[0].Description.ShouldBe("desc1");
        field.Arguments[0].Type.ShouldBe(typeof(StringGraphType));
        field.Arguments[0].DefaultValue.ShouldBe("12345");

        field.Arguments[1].Name.ShouldBe("arg2");
        field.Arguments[1].Description.ShouldBe("desc2");
        field.Arguments[1].Type.ShouldBe(typeof(IntGraphType));
        field.Arguments[1].DefaultValue.ShouldBe(9);

        field.Arguments[2].Name.ShouldBe("arg3");
        field.Arguments[2].Description.ShouldBe("desc3");
        field.Arguments[2].Type.ShouldBe(typeof(IntGraphType));
        field.Arguments[2].DefaultValue.ShouldBe(null);
        field.Arguments[2].Metadata["secure"].ShouldBe(true);

        field.Arguments[3].Name.ShouldBe("arg4");
        field.Arguments[3].Description.ShouldBeNull();
        field.Arguments[3].Type.ShouldBe(typeof(BooleanGraphType));
        field.Arguments[3].DefaultValue.ShouldBe(true);
        field.Arguments[3].Metadata["useBefore"].ShouldBe(new DateTime(2030, 1, 2));
    }

    [Fact]
    public async Task can_determine_whether_argument_exists_in_resolver()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<IntGraphType>("_")
            .Argument<StringGraphType>("arg1", "desc1", arg => arg.DefaultValue = "12345")
            .Returns<int>()
            .Resolve(context =>
            {
                context.HasArgument("arg1").ShouldBe(true);
                context.HasArgument("arg0").ShouldBe(false);
                return 0;
            });

        var field = objectType.Fields.First();
        field.Arguments.ShouldNotBeNull().Count.ShouldBe(1);

        field.Arguments[0].Name.ShouldBe("arg1");
        field.Arguments[0].Description.ShouldBe("desc1");
        field.Arguments[0].Type.ShouldBe(typeof(StringGraphType));
        field.Arguments[0].DefaultValue.ShouldBe("12345");
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "arg1", new ArgumentValue("abc", ArgumentSource.Literal) }
            }
        });
    }

    [Fact]
    public async Task getting_unspecified_argument_in_resolver_yields_null()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<StringGraphType>("arg1", arg => arg.DefaultValue = "desc1")
            .Resolve(context =>
            {
                context.GetArgument<string>("arg1").ShouldBe(null);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task getting_unspecified_argument_in_resolver_yields_overridden_default_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<StringGraphType>("arg1", "desc1", arg => arg.DefaultValue = "default")
            .Resolve(context =>
            {
                context.GetArgument("arg1", "default2").ShouldBe("default2");
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_get_nullable_argument_with_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<IntGraphType>("skip", "desc1", arg => arg.DefaultValue = 1)
            .Resolve(context =>
            {
                context.GetArgument<int?>("skip").ShouldBe(1);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "skip", new ArgumentValue(1, ArgumentSource.Literal) }
            },
            FieldDefinition = field
        });
    }

    [Fact]
    public void argument_with_clr_type_works()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<string>("test")
            .Argument<int>("intRequired")
            .Argument<int?>("intOptional", true)
            .Argument<string>("strRequired")
            .Argument<string>("strOptional", true);
        var testField = objectType.Fields.Find("test").ShouldNotBeNull();
        testField.Type.ShouldBe(typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>));
        testField.Arguments.ShouldNotBeNull();
        var arg1 = testField.Arguments.Find("intRequired").ShouldNotBeNull();
        arg1.Type.ShouldBe(typeof(NonNullGraphType<GraphQLClrInputTypeReference<int>>));
        var arg2 = testField.Arguments.Find("intOptional").ShouldNotBeNull();
        arg2.Type.ShouldBe(typeof(GraphQLClrInputTypeReference<int>));
        var arg3 = testField.Arguments.Find("strRequired").ShouldNotBeNull();
        arg3.Type.ShouldBe(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>));
        var arg4 = testField.Arguments.Find("strOptional").ShouldNotBeNull();
        arg4.Type.ShouldBe(typeof(GraphQLClrInputTypeReference<string>));
    }

    [Fact]
    public async Task can_get_nullable_argument_with_null_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<IntGraphType, int?>("skip", "desc1")
            .Resolve(context =>
            {
                context.GetArgument<int?>("skip").ShouldBe(null);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "skip", ArgumentValue.NullLiteral }
            },
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_get_nullable_argument_missing_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<IntGraphType, int?>("skip", "desc1")
            .Resolve(context =>
            {
                context.GetArgument<int?>("skip").ShouldBe(null);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_get_enum_argument()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<EpisodeEnum, Episodes>("episode", "episodes")
            .Resolve(context =>
            {
                context.GetArgument<Episodes>("episode").ShouldBe(Episodes.JEDI);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                {"episode", new ArgumentValue("JEDI", ArgumentSource.Literal) }
            },
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_get_enum_argument_with_overriden_default_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<EpisodeEnum, Episodes>("episode", "episodes")
            .Resolve(context =>
            {
                context.GetArgument("episode", Episodes.EMPIRE).ShouldBe(Episodes.EMPIRE);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_get_list_argument()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("episodes", "episodes")
            .Resolve(context =>
            {
                context.GetArgument<IEnumerable<string>>("episodes").ShouldBe(new List<string> { "JEDI", "EMPIRE" });
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                {"episodes", new ArgumentValue(new object[] {"JEDI", "EMPIRE" }, ArgumentSource.Literal) }
            },
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_get_collection_argument()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("episodes", "episodes")
            .Resolve(context =>
            {
                context.GetArgument<Collection<string>>("episodes").ShouldBe(new Collection<string> { "JEDI", "EMPIRE" });
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                {"episodes", new ArgumentValue(new object[] {"JEDI", "EMPIRE" }, ArgumentSource.Literal) }
            },
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task getting_specified_argument_in_resolver_overrides_default_value()
    {
        var objectType = new ObjectGraphType();
        objectType.Field<StringGraphType>("_")
            .Argument<StringGraphType>("arg1", "desc1", arg => arg.DefaultValue = "default")
            .Resolve(context =>
            {
                context.GetArgument("arg1", "default2").ShouldBe("arg1value");
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "arg1", new ArgumentValue("arg1value", ArgumentSource.Literal) }
            },
            FieldDefinition = field
        });
    }

    [Fact]
    public async Task can_access_object()
    {
        var objectType = new ObjectGraphType<int>();

        objectType.Field<StringGraphType>("_")
            .Resolve(context =>
            {
                context.Source.ShouldBe(12345);
                return null;
            });

        var field = objectType.Fields.First();
        await field.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext
        {
            Source = 12345
        });
    }

    [Fact]
    public async Task microsoft_di_resolve_works_with_dataloader()
    {
        var objectType = new ObjectGraphType<Tuple<string>>();
        var fieldType = objectType.Field(x => x.Item1)
            .Resolve()
            .ResolveAsync(_ => new DataLoaderResult<string>(Task.FromResult("abc")))
            .FieldType;

        var result = await fieldType.Resolver.ShouldNotBeNull().ResolveAsync(new ResolveFieldContext());
        var loadedResult = await result.ShouldBeAssignableTo<IDataLoaderResult<string>>()!.GetResultAsync();
        loadedResult.ShouldBe("abc");
    }
}
