using GraphQL.Types;
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
        Query = new ObjectGraphType { Name = "Dup" };
        Query.Field(
            "field",
            new StringGraphType(),
            arguments: new QueryArguments(
                new QueryArgument<StringGraphType> { Name = "arg" },
                new QueryArgument<StringGraphType> { Name = "arg" }
            ));
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
            Field<NonNullGraphType<StringGraphType>>("id", arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "x" }));
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

        Query = new ObjectGraphType();
        Query.Field(
            "test",
            new StringGraphType(),
            arguments: new QueryArguments(new QueryArgument(stringFilterInputType) { Name = "a" }),
            resolve: context => "ok");
    }
}

public class SchemaWithInvalidDefault1 : Schema
{
    public SchemaWithInvalidDefault1()
    {
        var root = new ObjectGraphType();
        root.Field<NonNullGraphType<StringGraphType>>(
           "field",
           arguments: new QueryArguments(
               new QueryArgument<NonNullGraphType<SomeInputType>>
               {
                   Name = "argOne",
                   DefaultValue = new SomeInput { Names = null }
               }));
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
        root.Field<NonNullGraphType<StringGraphType>>(
           "field",
           arguments: new QueryArguments(
               new QueryArgument<NonNullGraphType<SchemaWithInvalidDefault1.SomeInputType>>
               {
                   Name = "argOne",
                   DefaultValue = new SchemaWithInvalidDefault1.SomeInput { Names = new List<string> { "a", null, "b" } }
               }));
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
