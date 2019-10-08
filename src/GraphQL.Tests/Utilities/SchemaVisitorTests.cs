using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
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
                _.ExpectedResult = "{ 'hello': 'HELLO WORLD!' }";
            });
        }

        public class UppercaseDirectiveVisitor : SchemaDirectiveVisitor
        {
            public override void VisitField(FieldType field)
            {
                var inner = field.Resolver ?? new NameFieldResolver();
                field.Resolver = new FuncFieldResolver<object>(context =>
                {
                    var result = inner.Resolve(context);

                    if (result is string str)
                    {
                        return str.ToUpperInvariant();
                    }

                    return result;
                });
            }
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
                _.ExpectedResult = "{ 'hello': 'HELLO WORLD2!' }";
            });
        }

        public class Query
        {
            public Task<string> Hello()
            {
                return Task.FromResult("Hello World2!");
            }
        }

        public class AsyncUppercaseDirectiveVisitor : SchemaDirectiveVisitor
        {
            public override void VisitField(FieldType field)
            {
                var inner = WrapResolver(field.Resolver);
                field.Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    var result = await inner.ResolveAsync(context);

                    if (result is string str)
                    {
                        return str.ToUpperInvariant();
                    }

                    return result;
                });
            }
        }

        [Fact]
        public void can_apply_custom_directive_when_graph_type_first()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Name("hello")
                .Resolve(_ => "Hello World!")
                .Directive("upper", new UppercaseDirectiveVisitor());

            var directivesMetadata = objectType.Fields.First().GetDirectives();
            directivesMetadata.ShouldNotBeNull();
            directivesMetadata.Count.ShouldBe(1, "Only 1 directive should be added");
            directivesMetadata.ContainsKey("upper").ShouldBeTrue();

            var queryResult = CreateQueryResult("{ 'hello': 'HELLO WORLD!' }");
            var schema = new Schema { Query = objectType };

            AssertQuery(_ =>
            {
                _.Schema = schema;
                _.Query = "{ hello }";
            }, queryResult);
        }

        public class RegisterTypeDirectiveVisitor : SchemaDirectiveVisitor
        {
            public override void VisitSchema(Schema schema)
            {
                schema.RegisterType(new ObjectGraphType
                {
                    Name = "TestType"
                });
            }
        }

        public class DescriptionDirectiveVisitor : SchemaDirectiveVisitor
        {
            public DescriptionDirectiveVisitor()
            {
            }

            public DescriptionDirectiveVisitor(string description)
            {
                Arguments.Add("description", description);
            }

            public override void VisitObjectGraphType(IObjectGraphType type)
            {
                type.Description = GetArgument("description", string.Empty);
            }

            public override void VisitObjectGraphType(ObjectGraphType type)
            {
                type.Description = GetArgument("description", string.Empty);
            }

            public override void VisitEnum(EnumerationGraphType type)
            {
                type.Description = GetArgument("description", string.Empty);
            }

            public override void VisitEnumValue(EnumValueDefinition value)
            {
                value.Description = GetArgument("description", string.Empty);
            }

            public override void VisitScalar(ScalarGraphType scalar)
            {
                scalar.Description = GetArgument("description", string.Empty);
            }

            public override void VisitField(FieldType field)
            {
                field.Description = GetArgument("description", string.Empty);
            }

            public override void VisitInterface(InterfaceGraphType interfaceDefinition)
            {
                interfaceDefinition.Description = GetArgument("description", string.Empty);
            }

            public override void VisitUnion(UnionGraphType union)
            {
                union.Description = GetArgument("description", string.Empty);
            }

            public override void VisitArgumentDefinition(QueryArgument argument)
            {
                argument.Description = GetArgument("description", string.Empty);
            }

            public override void VisitInputObject(InputObjectGraphType type)
            {
                type.Description = GetArgument("description", string.Empty);
            }

            public override void VisitInputFieldDefinition(FieldType value)
            {
                value.Description = GetArgument("description", string.Empty);
            }
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
            schema.AddDirective("test", new RegisterTypeDirectiveVisitor());

            var directivesMetadata = schema.GetDirectives();
            directivesMetadata.ShouldNotBeNull();
            directivesMetadata.Count.ShouldBe(1, "Only 1 directive should be added");
            directivesMetadata.ContainsKey("test").ShouldBeTrue();

            schema.FindType("TestType").ShouldNotBeNull();
        }

        [Fact]
        public void can_create_custom_directive_for_all_locations()
        {
            Builder.RegisterDirectiveVisitor<DescriptionDirectiveVisitor>("description");
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
        }

        [Fact]
        public void can_create_custom_directive_for_all_locations_graph_type_first()
        {
            var objectType = new ObjectGraphType();
            objectType.AddDirective("desc", new DescriptionDirectiveVisitor("type"));
            objectType.Description.ShouldBe("type");

            var interfaceType = new InterfaceGraphType();
            interfaceType.AddDirective("desc", new DescriptionDirectiveVisitor("interface"));
            interfaceType.Description.ShouldBe("interface");

            var unionType = new UnionGraphType();
            unionType.AddDirective("desc", new DescriptionDirectiveVisitor("union"));
            unionType.Description.ShouldBe("union");

            var arg = new QueryArgument(new StringGraphType());
            arg.AddDirective("desc", new DescriptionDirectiveVisitor("arg"));
            arg.Description.ShouldBe("arg");

            var enumType = new EnumerationGraphType();
            enumType.AddDirective("desc", new DescriptionDirectiveVisitor("enumType"));
            enumType.Description.ShouldBe("enumType");

            var enumValue = new EnumValueDefinition();
            enumValue.AddDirective("desc", new DescriptionDirectiveVisitor("enumValue"));
            enumValue.Description.ShouldBe("enumValue");

            var inputType = new InputObjectGraphType();
            inputType.AddDirective("desc", new DescriptionDirectiveVisitor("inputType"));
            inputType.Description.ShouldBe("inputType");
        }
    }
}
