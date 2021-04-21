using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Initialization
{
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

        public class MyDirective : DirectiveGraphType
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
        public class TestDirective : DirectiveGraphType
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
}
