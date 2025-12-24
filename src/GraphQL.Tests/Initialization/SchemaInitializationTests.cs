using GraphQL.MicrosoftDI;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Tests.Initialization;

public class SchemaInitializationTests : SchemaInitializationTestBase
{
    [Fact]
    public void EmptyQuerySchema_Should_Throw()
    {
        ShouldThrow<EmptyQuerySchema, InvalidOperationException>("An Object type 'Empty' must define one or more fields.");
    }

    [Fact]
    public void SchemaWithDuplicateFields_Should_Throw()
    {
        ShouldThrow<SchemaWithDuplicateFields, InvalidOperationException>("The field 'field' must have a unique name within Object type 'Dup'; no two fields may share the same name.");
    }

    [Fact]
    public void SchemaWithDuplicateArguments_Should_Throw()
    {
        ShouldThrow<SchemaWithDuplicateArguments, InvalidOperationException>("The argument 'arg' must have a unique name within field 'Dup.field'; no two field arguments may share the same name.");
    }

    [Fact]
    public void SchemaWithDuplicateArgumentsInDirective_Should_Throw()
    {
        ShouldThrow<SchemaWithDuplicateArgumentsInDirective, InvalidOperationException>("The argument 'arg' must have a unique name within directive 'my'; no two directive arguments may share the same name.");
    }

    [Fact]
    public void EmptyInterfaceSchema_Should_Throw()
    {
        ShouldThrow<EmptyInterfaceSchema, InvalidOperationException>("An Interface type 'Empty' must define one or more fields.");
    }

    [Fact]
    public void SchemaWithDuplicateInterfaceFields_Should_Throw()
    {
        var exceptions = Should.Throw<AggregateException>(() =>
        {
            var schema = new SchemaWithDuplicateInterfaceFields();
            schema.Initialize();
        }).InnerExceptions;
        exceptions.Count.ShouldBe(2);
        exceptions[0].ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'field' must have a unique name within Interface type 'Dup'; no two fields may share the same name.");
        exceptions[1].ShouldBeOfType<ArgumentException>().Message.ShouldBe("Type ObjectGraphType with name 'Query' does not implement interface InterfaceGraphType with name 'Dup'. Field 'field' must be of type 'String' or covariant from it, but in fact it is of type 'String'.");
    }

    [Fact]
    public void SchemaWithDeprecatedAppliedDirective_Should_Not_Throw()
    {
        ShouldNotThrow<SchemaWithDeprecatedAppliedDirective>();
    }

    [Fact]
    public void SchemaWithNullDirectiveArgumentWhenShouldBeNonNull_Should_Throw()
    {
        ShouldThrow<SchemaWithNullDirectiveArgumentWhenShouldBeNonNull, InvalidOperationException>("Directive 'test' applied to field 'MyQuery.field' explicitly specifies 'null' value for required argument 'arg'. The value must be non-null.");
    }

    [Fact]
    public void SchemaWithArgumentsOnInputField_Should_Throw()
    {
        ShouldThrow<SchemaWithArgumentsOnInputField, InvalidOperationException>("The field 'id' of an Input Object type 'MyInput' must not have any arguments specified.");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/pull/2707/files#r757949833
    [Fact]
    public void SchemaWithInvalidDefault_Should_Throw()
    {
        ShouldThrow<SchemaWithInvalidDefault1, InvalidOperationException>("The default value of argument 'argOne' of field 'Object.field' is invalid.");
        ShouldThrow<SchemaWithInvalidDefault2, InvalidOperationException>("The default value of argument 'argOne' of field 'Object.field' is invalid.");
    }

    [Fact]
    public void SchemaWithEnumWithoutValues_Should_Throw()
    {
        ShouldThrow<SchemaWithEnumWithoutValues1, InvalidOperationException>("An Enum type 'EnumWithoutValues' must define one or more unique enum values.");
        ShouldThrow<SchemaWithEnumWithoutValues2, InvalidOperationException>("An Enum type 'Enumeration' must define one or more unique enum values.");
    }

    [Fact]
    public void SchemaWithDirective_Should_Not_Throw()
    {
        ShouldNotThrow<SchemaWithDirective>();
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3507
    [Fact]
    public void Passing_GraphType_InsteadOf_ClrType_Should_Produce_Friendly_Error()
    {
        Should.Throw<ArgumentException>(() => new Bug3507Schema()).Message.ShouldStartWith("The GraphQL type for argument 'updateDate.newDate' could not be derived implicitly from type 'DateGraphType'. The graph type 'DateGraphType' cannot be used as a CLR type.");
    }

    [Fact]
    public void SchemaWithoutQuery_Should_Throw()
    {
        ShouldThrow<Schema, InvalidOperationException>("Query root type must be provided. See https://spec.graphql.org/October2021/#sec-Schema-Introspection");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/pull/3571
    [Fact]
    public void Deprecate_Required_Arguments_And_Input_Fields_Should_Produce_Friendly_Error()
    {
        ShouldThrow<Issue3571Schema1, InvalidOperationException>("The required argument 'flag' of field 'MyQuery.str' has no default value so `@deprecated` directive must not be applied to this argument. To deprecate a required argument, it must first be made optional by either changing the type to nullable or adding a default value.");
        ShouldThrow<Issue3571Schema2, InvalidOperationException>("The required input field 'age' of an Input Object 'PersonInput' has no default value so `@deprecated` directive must not be applied to this input field. To deprecate an input field, it must first be made optional by either changing the type to nullable or adding a default value.");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2994
    [Fact]
    public void StreamResolver_On_Wrong_Fields_Should_Produce_Friendly_Error()
    {
        ShouldThrow<SchemaWithFieldStreamResolverOnNonRootSubscriptionField, InvalidOperationException>("The field 'str' of an Object type 'MyQuery' must not have StreamResolver set. You should set StreamResolver only for the root fields of subscriptions.");
        var ex1 = ShouldThrowMultiple<SchemaWithFieldStreamResolverOnFieldOfInterface>();
        ex1.InnerExceptions.Count.ShouldBe(2);
        ex1.InnerExceptions[0].ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'id' of an Interface type 'My' must not have StreamResolver set. You should set StreamResolver only for the root fields of subscriptions.");
        ex1.InnerExceptions[1].ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'id' of an Interface type 'My' must not have Resolver set. Each interface is translated to a concrete type during request execution. You should set Resolver only for fields of object output types.");
        var ex2 = ShouldThrowMultiple<SchemaWithFieldStreamResolverOnFieldOfInputObject>();
        ex2.InnerExceptions.Count.ShouldBe(2);
        ex2.InnerExceptions[0].ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'name' of an Input Object type 'PersonInput' must not have StreamResolver set. You should set StreamResolver only for the root fields of subscriptions.");
        ex2.InnerExceptions[1].ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The field 'name' of an Input Object type 'PersonInput' must not have Resolver set. You should set Resolver only for fields of object output types.");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1176
    [Fact]
    public void Resolver_On_InputField_Should_Produce_Friendly_Error()
    {
        ShouldThrow<SchemaWithInputFieldResolver, InvalidOperationException>("The field 'name' of an Input Object type 'PersonInput' must not have Resolver set. You should set Resolver only for fields of object output types.");
    }

    [Fact]
    public void Resolver_On_InterfaceField_Should_Produce_Friendly_Error()
    {
        ShouldThrow<SchemaWithFieldResolverOnFieldOfInterface, InvalidOperationException>("The field 'id' of an Interface type 'My' must not have Resolver set. Each interface is translated to a concrete type during request execution. You should set Resolver only for fields of object output types.");
    }

    [Fact]
    public void SchemaWithTheSameRootOperationTypes_Should_Throw()
    {
        ShouldThrow<SchemaWithTheSameRootOperationTypes, InvalidOperationException>("The query, mutation, and subscription root types must all be different types if provided.");
    }
}

public class EmptyQuerySchema : Schema
{
    public EmptyQuerySchema()
    {
        Query = new ObjectGraphType { Name = "Empty" };
    }
}

public class SchemaWithDuplicateArgumentsInDirective : Schema
{
    public SchemaWithDuplicateArgumentsInDirective()
    {
        Query = new ObjectGraphType { Name = "q" };
        Query.Fields.Add(new FieldType { Name = "f", ResolvedType = new StringGraphType() });

        Directives.Register(new MyDirective());
    }

    public class MyDirective : Directive
    {
        public MyDirective()
            : base("my", DirectiveLocation.Field)
        {
            Arguments = new QueryArguments(
                new QueryArgument<BooleanGraphType> { Name = "arg" },
                new QueryArgument<BooleanGraphType> { Name = "arg" }
            );
        }
    }
}

public class SchemaWithDuplicateFields : Schema
{
    public SchemaWithDuplicateFields()
    {
        Query = new ObjectGraphType { Name = "Dup" };
        Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });
        Query.AddField(new FieldType { Name = "field_2", ResolvedType = new StringGraphType() }); // bypass HasField check
        Query.Fields.List[1].Name = "field";
    }
}

public class SchemaWithDuplicateArguments : Schema
{
    public SchemaWithDuplicateArguments()
    {
        var query = new ObjectGraphType { Name = "Dup" };
        query.Field("field", new StringGraphType())
            .Argument<StringGraphType>("arg")
            .Argument<StringGraphType>("arg");
        Query = query;
    }
}

public class EmptyInterfaceSchema : Schema
{
    public EmptyInterfaceSchema()
    {
        Query = new ObjectGraphType { Name = "Query" };
        Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });

        var iface = new InterfaceGraphType { Name = "Empty", ResolveType = _ => null };
        RegisterType(iface);
        Query.ResolvedInterfaces.Add(iface);
    }
}

public class SchemaWithDuplicateInterfaceFields : Schema
{
    public SchemaWithDuplicateInterfaceFields()
    {
        Query = new ObjectGraphType { Name = "Query" };

        var iface = new InterfaceGraphType { Name = "Dup", ResolveType = _ => null };
        iface.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });
        iface.AddField(new FieldType { Name = "field_2", ResolvedType = new StringGraphType() }); // bypass HasField check
        iface.Fields.List[1].Name = "field";

        Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });
        RegisterType(iface);
        Query.ResolvedInterfaces.Add(iface);
    }
}

public class SchemaWithDeprecatedAppliedDirective : Schema
{
    public SchemaWithDeprecatedAppliedDirective()
    {
        Query = new ObjectGraphType { Name = "Query" };

        var f = Query.AddField(new FieldType { Name = "field1", ResolvedType = new StringGraphType() }).ApplyDirective("deprecated", "reason", "aaa");
        f.DeprecationReason.ShouldBe("aaa");
        f.DeprecationReason = "bbb";
        f.FindAppliedDirective("deprecated").ShouldNotBeNull().FindArgument("reason").ShouldNotBeNull().Value.ShouldBe("bbb");
    }
}

public class SchemaWithNullDirectiveArgumentWhenShouldBeNonNull : Schema
{
    public class TestDirective : Directive
    {
        public TestDirective()
            : base("test", DirectiveLocation.Schema, DirectiveLocation.FieldDefinition)
        {
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
            {
                Name = "arg"
            });
        }
    }

    public SchemaWithNullDirectiveArgumentWhenShouldBeNonNull()
    {
        Query = new ObjectGraphType { Name = "MyQuery" };
        Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() }).ApplyDirective("test", "arg", null);

        Directives.Register(new TestDirective());
    }
}

public class SchemaWithArgumentsOnInputField : Schema
{
    public class MyInputGraphType : InputObjectGraphType
    {
        public MyInputGraphType()
        {
            Field<NonNullGraphType<StringGraphType>>("id").Argument<StringGraphType>("x");
        }
    }

    public SchemaWithArgumentsOnInputField()
    {
        Query = new ObjectGraphType { Name = "MyQuery" };
        Query.AddField(new FieldType
        {
            Name = "field",
            ResolvedType = new StringGraphType(),
            Arguments = new QueryArguments(new QueryArgument<MyInputGraphType> { Name = "arg" })
        });
    }
}

public class SchemaWithInvalidDefault1 : Schema
{
    public SchemaWithInvalidDefault1()
    {
        var root = new ObjectGraphType();
        root.Field<NonNullGraphType<StringGraphType>>("field")
            .Argument<NonNullGraphType<SomeInputType>>("argOne", arg => arg.DefaultValue = new SomeInput { Names = null });
        Query = root;
    }

    public class SomeInputType : InputObjectGraphType<SomeInput>
    {
        public SomeInputType()
        {
            Name = "SomeInput";
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("names");
        }
    }

    public class SomeInput
    {
        public IList<string?>? Names { get; set; }
    }
}

public class SchemaWithInvalidDefault2 : Schema
{
    public SchemaWithInvalidDefault2()
    {
        var root = new ObjectGraphType();
        root.Field<NonNullGraphType<StringGraphType>>("field")
            .Argument<NonNullGraphType<SchemaWithInvalidDefault1.SomeInputType>>("argOne", arg => arg.DefaultValue = new SchemaWithInvalidDefault1.SomeInput { Names = ["a", null, "b"] });
        Query = root;
    }
}

public class SchemaWithEnumWithoutValues1 : Schema
{
    public enum EnumWithoutValues
    {
    }

    public SchemaWithEnumWithoutValues1()
    {
        var type = new EnumerationGraphType<EnumWithoutValues>();
        RegisterType(type);
        Query = new DummyType();
    }
}

public class SchemaWithEnumWithoutValues2 : Schema
{
    public SchemaWithEnumWithoutValues2()
    {
        var type = new EnumerationGraphType();
        RegisterType(type);
        Query = new DummyType();
    }
}

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3301
public class SchemaWithDirective : Schema
{
    public class MaxLength : Directive
    {
        public MaxLength()
            : base("maxLength", DirectiveLocation.Mutation, DirectiveLocation.InputFieldDefinition)
        {
            Description = "Used to specify the minimum and/or maximum length for an input field or argument.";
            Arguments = new QueryArguments(
                new QueryArgument<IntGraphType>
                {
                    Name = "min",
                    Description = "If specified, specifies the minimum length that the input field or argument must have."
                },
                new QueryArgument<IntGraphType>
                {
                    Name = "max",
                    Description = "If specified, specifies the maximum length that the input field or argument must have."
                }
            );
        }
    }

    public class MaxLengthDirectiveVisitor : BaseSchemaNodeVisitor
    {
        public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
        {
            var applied = field.FindAppliedDirective("maxLength");
            applied.ShouldBeNull();
        }

        public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema)
        {
            if (field.Name == "count")
            {
                var applied = field.FindAppliedDirective("maxLength");
                applied.ShouldNotBeNull();
                applied.ArgumentsCount.ShouldBe(2);
            }
        }
    }

    public class BookSummaryCreateArgInputType : InputObjectGraphType<BookSummaryCreateArg>
    {
        public BookSummaryCreateArgInputType()
        {
            Name = "BookSummaryCreateArg";
            Field(_ => _.Count).ApplyDirective("maxLength", x =>
                x.AddArgument(new DirectiveArgument("min") { Name = "min", Value = 1 })
                .AddArgument(new DirectiveArgument("max") { Name = "max", Value = 10 }));
        }
    }

    public class BookSummaryCreateArg
    {
        public int Count { get; set; }
    }

    public SchemaWithDirective()
    {
        var root = new ObjectGraphType();
        root.Field<StringGraphType>("field").Argument<BookSummaryCreateArgInputType>("arg");
        Query = root;

        Directives.Register(new MaxLength());
        this.RegisterVisitor<MaxLengthDirectiveVisitor>();
    }
}

public class Bug3507Schema : Schema
{
    public Bug3507Schema()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<BooleanGraphType>("updateDate")
            .Argument<DateGraphType>("newDate", true)
            .Resolve()
            .WithScope()
            .ResolveAsync(_ => Task.FromResult((object?)true));
        Query = type;
    }
}

public class Issue3571Schema1 : Schema
{
    public Issue3571Schema1()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<StringGraphType>("str")
            .Argument<NonNullGraphType<BooleanGraphType>>("flag", arg => arg.DeprecationReason = "Use some other argument.")
            .Resolve(_ => "abc");
        Query = type;
    }
}

public class Issue3571Schema2 : Schema
{
    public Issue3571Schema2()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<StringGraphType>("str")
            .Argument<PersonInput>("person")
            .Resolve(_ => "abc");
        Query = type;
    }

    private class PersonInput : InputObjectGraphType<Person>
    {
        public PersonInput()
        {
            Field(x => x.Name);
            Field(x => x.Age).DeprecationReason("Use some other input field.");
        }
    }

    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}

public class SchemaWithFieldStreamResolverOnNonRootSubscriptionField : Schema
{
    public SchemaWithFieldStreamResolverOnNonRootSubscriptionField()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<StringGraphType>("str")
            .ResolveStream(_ => new Subscription.SampleObservable<string>())
            .Resolve(_ => "abc");
        Query = type;
    }
}

public class SchemaWithFieldStreamResolverOnFieldOfInterface : Schema
{
    public SchemaWithFieldStreamResolverOnFieldOfInterface()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<MyInterface>("hero");
        Query = type;
    }

    private class MyInterface : InterfaceGraphType
    {
        public MyInterface()
        {
            Name = "My";

            Field<StringGraphType>("id").ResolveStream(_ => new Subscription.SampleObservable<string>());
        }
    }
}

public class SchemaWithFieldResolverOnFieldOfInterface : Schema
{
    public SchemaWithFieldResolverOnFieldOfInterface()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<MyInterface>("hero").Resolve(_ => null);
        Query = type;
    }

    private class MyInterface : InterfaceGraphType
    {
        public MyInterface()
        {
            Name = "My";

            Field<StringGraphType>("id").Resolve(_ => "abc");
        }
    }
}

public class SchemaWithFieldStreamResolverOnFieldOfInputObject : Schema
{
    public SchemaWithFieldStreamResolverOnFieldOfInputObject()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<StringGraphType>("str")
            .Argument<PersonInput>("person")
            .Resolve(_ => "abc");
        Query = type;
    }

    private class PersonInput : InputObjectGraphType<Person>
    {
        public PersonInput()
        {
            Field(x => x.Name).ResolveStream(_ => new Subscription.SampleObservable<string>());
            Field(x => x.Age);
        }
    }

    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}

public class SchemaWithInputFieldResolver : Schema
{
    public SchemaWithInputFieldResolver()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<StringGraphType>("str")
            .Argument<PersonInput>("person")
            .Resolve(_ => "abc");
        Query = type;
    }

    private class PersonInput : InputObjectGraphType<Person>
    {
        public PersonInput()
        {
            Field(x => x.Name).Resolve(_ => "abc");
            Field(x => x.Age);
        }
    }

    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}

public class SchemaWithTheSameRootOperationTypes : Schema
{
    public SchemaWithTheSameRootOperationTypes()
    {
        var type = new ObjectGraphType { Name = "MyQuery" };
        type.Field<StringGraphType>("str")
            .Resolve(_ => "abc");
        Query = type;
        Mutation = type;
    }
}
