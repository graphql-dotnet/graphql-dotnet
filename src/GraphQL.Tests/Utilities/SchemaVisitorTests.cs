using GraphQL.Tests.Utilities.Visitors;
using GraphQL.Types;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class SchemaVisitorTests : SchemaBuilderTestBase
    {
        [Fact]
        public void can_create_basic_custom_directive()
        {
            Builder.RegisterDirectiveVisitor<UppercaseDirectiveVisitor>("upper");

            AssertQuery(_ =>
            {
                _.Definitions = @"
                    type Query {
                        hello: String @upper
                    }
                ";

                _.Query = "{ hello }";
                _.Root = new { Hello = "Hello World!" };
                _.ExpectedResult = @"{ ""hello"": ""HELLO WORLD!"" }";
            });
        }

        [Fact]
        public void can_create_custom_directive_with_tasks()
        {
            Builder.RegisterDirectiveVisitor<AsyncUppercaseDirectiveVisitor>("upper");
            Builder.Types.Include<Query>();

            AssertQuery(_ =>
            {
                _.Definitions = @"
                    type Query {
                        hello: String @upper
                    }
                ";

                _.Query = "{ hello }";
                _.ExpectedResult = @"{ ""hello"": ""HELLO WORLD2!"" }";
            });
        }

        public class Query
        {
            public Task<string> Hello() => Task.FromResult("Hello World2!");
        }

        [Fact]
        public void can_apply_custom_directive_when_graph_type_first()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Name("hello")
                .Resolve(_ => "Hello World!")
                .Directive(new UppercaseDirectiveVisitor());

            var directives = objectType.Fields.First().GetDirectives().ToList();
            directives.ShouldNotBeNull();
            directives.Count.ShouldBe(1, "Only 1 directive should be added");
            directives.Any(d => d.Name == "upper").ShouldBeTrue();

            var queryResult = CreateQueryResult(@"{ ""hello"": ""HELLO WORLD!"" }");
            var schema = new Schema { Query = objectType };

            AssertQuery(_ =>
            {
                _.Schema = schema;
                _.Query = "{ hello }";
            }, queryResult);
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
        public void can_apply_custom_directive_to_schema()
        {
            var schema = new Schema();
            schema.AddDirective(new RegisterTypeDirectiveVisitor());

            var directives = schema.GetDirectives().ToList();
            directives.ShouldNotBeNull();
            directives.Count.ShouldBe(1, "Only 1 directive should be added");
            directives.Any(d => d.Name == "registerType").ShouldBeTrue();

            schema.FindType("TestAdditionalType").ShouldNotBeNull();
        }

        [Fact]
        public void can_create_custom_directive_for_all_locations()
        {
            Builder.RegisterDirectiveVisitor<DescriptionDirectiveVisitor>("description");
            Builder.RegisterDirectiveVisitor<RegisterTypeDirectiveVisitor>("registerType");
            Builder.Types.For("TestType").IsTypeOf<TestType>();
            Builder.Types.For("TestTypeForUnion").IsTypeOf<TestTypeForUnion>();

            var schema = Builder.Build(@"
                    type Query {
                        hello: String
                    }

                    interface TestInterface @description(description: ""interface""){
                        id: ID!
                    }

                    type TestType implements TestInterface @description(description: ""type"") {
                        id: ID!
                        name(arg: Int @description(description: ""arg"")): String @description(description: ""field"")
                    }

                    type TestTypeForUnion {
                        field: ID!
                    }

                    union TestUnion @description(description: ""union"") = TestType | TestTypeForUnion

                    enum TestEnum @description(description: ""enum-definition""){
                      TESTVAL1 @description(description: ""enum-value"")
                      TESTVAL2
                      TESTVAL3
                    }

                    input TestInputType @description(description: ""input-type"") {
                        id: Int = 0 @description(description: ""input-field"")
                    }

                    scalar TestScalar @description(description: ""scalar"")

                    schema @registerType {
                      query: Query
                    } 
            ");
            schema.Initialize();

            // object type
            var type = schema.FindType("TestType");
            type.ShouldNotBeNull();
            var objType = type.ShouldBeOfType<ObjectGraphType>();
            objType.Description.ShouldBe("type");

            var field = objType.Fields.FirstOrDefault(f => f.Name == "name");
            field.ShouldNotBeNull();
            field.Description.ShouldBe("field");

            var arg = field.Arguments.Find("arg");
            arg.ShouldNotBeNull();
            arg.Description.ShouldBe("arg");

            type = schema.FindType("TestInterface");
            type.ShouldNotBeNull();
            var interfaceType = type.ShouldBeOfType<InterfaceGraphType>();
            interfaceType.Description.ShouldBe("interface");

            type = schema.FindType("TestUnion");
            type.ShouldNotBeNull();
            var unionType = type.ShouldBeOfType<UnionGraphType>();
            unionType.Description.ShouldBe("union");

            type = schema.FindType("TestEnum");
            type.ShouldNotBeNull();
            var enumType = type.ShouldBeOfType<EnumerationGraphType>();
            enumType.Description.ShouldBe("enum-definition");

            var enumVal = enumType.Values.FirstOrDefault(ev => ev.Name == "TESTVAL1");
            enumVal.ShouldNotBeNull();
            enumVal.Description.ShouldBe("enum-value");

            type = schema.FindType("TestInputType");
            type.ShouldNotBeNull();
            var inputType = type.ShouldBeOfType<InputObjectGraphType>();
            inputType.Description.ShouldBe("input-type");

            field = inputType.Fields.FirstOrDefault(f => f.Name == "id");
            field.ShouldNotBeNull();
            field.Description.ShouldBe("input-field");

            // registerType directive test
            type = schema.FindType("TestAdditionalType");
            type.ShouldNotBeNull();
        }

        [Fact]
        public void can_create_custom_directive_for_all_locations_graph_type_first()
        {
            var objectType = new ObjectGraphType();
            objectType.AddDirective(new DescriptionDirectiveVisitor("type"));
            objectType.Description.ShouldBe("type");

            var field = objectType.Field<StringGraphType>("test");
            field.AddDirective(new DescriptionDirectiveVisitor("field"));
            field.Description.ShouldBe("field");

            var interfaceType = new InterfaceGraphType();
            interfaceType.AddDirective(new DescriptionDirectiveVisitor("interface"));
            interfaceType.Description.ShouldBe("interface");

            var unionType = new UnionGraphType();
            unionType.AddDirective(new DescriptionDirectiveVisitor("union"));
            unionType.Description.ShouldBe("union");

            var arg = new QueryArgument(new StringGraphType());
            arg.AddDirective(new DescriptionDirectiveVisitor("arg"));
            arg.Description.ShouldBe("arg");

            var enumType = new EnumerationGraphType();
            enumType.AddDirective(new DescriptionDirectiveVisitor("enumType"));
            enumType.Description.ShouldBe("enumType");

            var enumValue = new EnumValueDefinition();
            enumValue.AddDirective(new DescriptionDirectiveVisitor("enumValue"));
            enumValue.Description.ShouldBe("enumValue");

            var inputType = new InputObjectGraphType();
            inputType.AddDirective(new DescriptionDirectiveVisitor("inputType"));
            inputType.Description.ShouldBe("inputType");

            field = inputType.Field<StringGraphType>("test");
            field.AddDirective(new DescriptionDirectiveVisitor("input-field"));
            field.Description.ShouldBe("input-field");

            var scalarType = new BigIntGraphType();
            scalarType.AddDirective(new DescriptionDirectiveVisitor("scalar"));
            scalarType.Description.ShouldBe("scalar");
        }
    }
}
