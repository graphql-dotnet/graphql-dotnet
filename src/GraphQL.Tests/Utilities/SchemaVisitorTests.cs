using GraphQL.Tests.Utilities.Visitors;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities;

public class SchemaVisitorTests : SchemaBuilderTestBase
{
    [Fact]
    public void can_create_basic_custom_directive()
    {
        AssertQuery(_ =>
        {
            _.Definitions = @"
                    type Query {
                        hello: String @upper
                    }
                ";
            _.ConfigureBuildedSchema = schema => { schema.Directives.Register(new UpperDirective()); schema.RegisterVisitor<UppercaseDirectiveVisitor>(); };
            _.Query = "{ hello }";
            _.Root = new { Hello = "Hello World!" };
            _.ExpectedResult = @"{ ""hello"": ""HELLO WORLD!"" }";
        });
    }

    public class Query
    {
        public Task<string> Hello() => Task.FromResult("Hello World2!");
    }

    public class TestType
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class TestTypeForUnion
    {
        public int Field { get; set; }
    }

    [Fact]
    public void can_create_custom_directive_for_all_locations()
    {
        Builder.Types.For("TestType").IsTypeOf<TestType>();
        Builder.Types.For("TestTypeForUnion").IsTypeOf<TestTypeForUnion>();

        var schema = Builder.Build(@"
                    type Query {
                        hello: String
                    }

                    interface TestInterface @description(text: ""interface""){
                        id: ID!
                    }

                    type TestType implements TestInterface @description(text: ""type"") {
                        id: ID!
                        name(arg: Int @description(text: ""arg"")): String @description(text: ""field"")
                    }

                    type TestTypeForUnion {
                        field: ID!
                    }

                    union TestUnion @description(text: ""union"") = TestType | TestTypeForUnion

                    enum TestEnum @description(text: ""enum-definition""){
                      TESTVAL1 @description(text: ""enum-value"")
                      TESTVAL2
                      TESTVAL3
                    }

                    input TestInputType @description(text: ""input-type"") {
                        id: Int = 0 @description(text: ""input-field"")
                    }

                    scalar TestScalar @description(text: ""scalar"")

                    schema @description(text: ""schema description"") {
                      query: Query
                    }
            ");
        schema.Directives.Register(new Directive("description",
            DirectiveLocation.Schema, DirectiveLocation.Union, DirectiveLocation.Interface, DirectiveLocation.Object,
            DirectiveLocation.InputObject, DirectiveLocation.FieldDefinition, DirectiveLocation.InputFieldDefinition,
            DirectiveLocation.ArgumentDefinition, DirectiveLocation.Enum, DirectiveLocation.EnumValue)
        {
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "text" })
        });
        schema.RegisterVisitor<DescriptionDirectiveVisitor>();
        schema.Initialize();

        schema.Description.ShouldBe("schema description");

        var type = schema.AllTypes["TestType"];
        type.ShouldNotBeNull();
        var objType = type.ShouldBeOfType<ObjectGraphType>();
        objType.Description.ShouldBe("type");

        var field = objType.Fields.FirstOrDefault(f => f.Name == "name");
        field.ShouldNotBeNull();
        field.Description.ShouldBe("field");

        var arg = field.Arguments.Find("arg");
        arg.ShouldNotBeNull();
        arg.Description.ShouldBe("arg");

        type = schema.AllTypes["TestInterface"];
        type.ShouldNotBeNull();
        var interfaceType = type.ShouldBeOfType<InterfaceGraphType>();
        interfaceType.Description.ShouldBe("interface");

        type = schema.AllTypes["TestUnion"];
        type.ShouldNotBeNull();
        var unionType = type.ShouldBeOfType<UnionGraphType>();
        unionType.Description.ShouldBe("union");

        type = schema.AllTypes["TestEnum"];
        type.ShouldNotBeNull();
        var enumType = type.ShouldBeOfType<EnumerationGraphType>();
        enumType.Description.ShouldBe("enum-definition");

        var enumVal = enumType.Values.FirstOrDefault(ev => ev.Name == "TESTVAL1");
        enumVal.ShouldNotBeNull();
        enumVal.Description.ShouldBe("enum-value");

        type = schema.AllTypes["TestInputType"];
        type.ShouldNotBeNull();
        var inputType = type.ShouldBeOfType<InputObjectGraphType>();
        inputType.Description.ShouldBe("input-type");

        field = inputType.Fields.FirstOrDefault(f => f.Name == "id");
        field.ShouldNotBeNull();
        field.Description.ShouldBe("input-field");
    }

    [Fact]
    public void can_create_custom_directive_for_all_locations_graph_type_first()
    {
        var schema = new Schema();
        schema.ApplyDirective("schema");
        schema.HasAppliedDirectives().ShouldBeTrue();
        schema.GetAppliedDirectives().Count.ShouldBe(1);

        var objectType = new ObjectGraphType();
        objectType.ApplyDirective("type");
        objectType.HasAppliedDirectives().ShouldBeTrue();
        objectType.GetAppliedDirectives().Count.ShouldBe(1);

        var field = objectType.Field<StringGraphType>("test");
        field.ApplyDirective("field");
        field.HasAppliedDirectives().ShouldBeTrue();
        field.GetAppliedDirectives().Count.ShouldBe(1);

        var interfaceType = new InterfaceGraphType();
        interfaceType.ApplyDirective("interface");
        interfaceType.HasAppliedDirectives().ShouldBeTrue();
        interfaceType.GetAppliedDirectives().Count.ShouldBe(1);

        var unionType = new UnionGraphType();
        unionType.ApplyDirective("union");
        unionType.HasAppliedDirectives().ShouldBeTrue();
        unionType.GetAppliedDirectives().Count.ShouldBe(1);

        var arg = new QueryArgument(new StringGraphType());
        arg.ApplyDirective("arg");
        arg.HasAppliedDirectives().ShouldBeTrue();
        arg.GetAppliedDirectives().Count.ShouldBe(1);

        var enumType = new EnumerationGraphType();
        enumType.ApplyDirective("enumType");
        enumType.HasAppliedDirectives().ShouldBeTrue();
        enumType.GetAppliedDirectives().Count.ShouldBe(1);

        var enumValue = new EnumValueDefinition("UNUSED", null);
        enumValue.ApplyDirective("enumValue");
        enumValue.HasAppliedDirectives().ShouldBeTrue();
        enumValue.GetAppliedDirectives().Count.ShouldBe(1);

        var inputType = new InputObjectGraphType();
        inputType.ApplyDirective("inputType");
        inputType.HasAppliedDirectives().ShouldBeTrue();
        inputType.GetAppliedDirectives().Count.ShouldBe(1);

        var input = inputType.Field<StringGraphType>("test");
        input.ApplyDirective("inputField");
        input.HasAppliedDirectives().ShouldBeTrue();
        input.GetAppliedDirectives().Count.ShouldBe(1);

        var scalarType = new BigIntGraphType();
        scalarType.ApplyDirective("scalar");
        scalarType.HasAppliedDirectives().ShouldBeTrue();
        scalarType.GetAppliedDirectives().Count.ShouldBe(1);
    }
}
