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
        ShouldThrow<SchemaWithDuplicateInterfaceFields, InvalidOperationException>("The field 'field' must have a unique name within Interface type 'Dup'; no two fields may share the same name.");
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

    [Fact]
    public void SchemaWithNotFullSpecifiedResolvedType_Should_Throw()
    {
        ShouldThrow<SchemaWithNotFullSpecifiedResolvedType, InvalidOperationException>("The field 'in' of an Input Object type 'InputString' must have non-null 'ResolvedType' property for all types in the chain.");
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
        f.FindAppliedDirective("deprecated").FindArgument("reason").Value.ShouldBe("bbb");
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

// https://github.com/graphql-dotnet/graphql-dotnet/issues/2675
public class SchemaWithNotFullSpecifiedResolvedType : Schema
{
    public SchemaWithNotFullSpecifiedResolvedType()
    {
        var stringFilterInputType = new InputObjectGraphType { Name = "InputString" };

        stringFilterInputType.AddField(new FieldType
        {
            Name = "eq",
            ResolvedType = new StringGraphType()
        });
        stringFilterInputType.AddField(new FieldType
        {
            Name = "in",
            ResolvedType = new ListGraphType<StringGraphType>()
        });
        stringFilterInputType.AddField(new FieldType
        {
            Name = "not",
            ResolvedType = new NonNullGraphType<StringGraphType>()
        });

        var query = new ObjectGraphType();
        query.Field("test", new StringGraphType())
            .Arguments(new QueryArgument(stringFilterInputType) { Name = "a" })
            .Resolve(_ => "ok");
        Query = query;
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
        public IList<string> Names { get; set; }
    }
}

public class SchemaWithInvalidDefault2 : Schema
{
    public SchemaWithInvalidDefault2()
    {
        var root = new ObjectGraphType();
        root.Field<NonNullGraphType<StringGraphType>>("field")
            .Argument<NonNullGraphType<SchemaWithInvalidDefault1.SomeInputType>>("argOne", arg => arg.DefaultValue = new SchemaWithInvalidDefault1.SomeInput { Names = new List<string> { "a", null, "b" } });
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
    }
}

public class SchemaWithEnumWithoutValues2 : Schema
{
    public SchemaWithEnumWithoutValues2()
    {
        var type = new EnumerationGraphType();
        RegisterType(type);
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
            Field(_ => _.Count).Directive("maxLength", x =>
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
