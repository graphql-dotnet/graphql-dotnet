using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Complexity;

public class ComplexityTests
{
    [Fact]
    public void EnsureDefaultsHaveNotChanged()
    {
        // if the defaults change, the documentation and tests for the examples provided
        // in the documentation should be updated
        var configuration = new ComplexityOptions();
        configuration.MaxComplexity.ShouldBeNull();
        configuration.MaxDepth.ShouldBeNull();
        configuration.DefaultScalarImpact.ShouldBe(1);
        configuration.DefaultObjectImpact.ShouldBe(1);
        configuration.DefaultListImpactMultiplier.ShouldBe(20);
        configuration.ValidateComplexityDelegate.ShouldBeNull();
        configuration.DefaultComplexityImpactDelegate.ShouldNotBeNull();
    }

    [Fact]
    public void TestCycleDetection()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: Field2Type } type Field2Type { field3: Field3Type } type Field3Type { field1: Field1Type }");
        var query = """
            {
              field1 {
                ...frag1
              }
            }

            fragment frag1 on Field1Type {
              field2 {
                ...frag2
              }
            }

            fragment frag2 on Field2Type {
              field3 {
                ...frag3
              }
            }

            fragment frag3 on Field3Type {
              field1 {
                ...frag1
              }
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Fragment 'frag1' has a circular reference.");
    }

    [Fact]
    public void TestInvalidFragmentName()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: Field2Type } type Field2Type { field3: Field3Type } type Field3Type { field1: Field1Type }");
        var query = """
            {
              field1 {
                ...frag1
              }
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Fragment 'frag1' not found in document.");
    }

    [Fact]
    public void TestInvalidTypeConditionFragment()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: String }");
        var query = """
            {
              field1 {
                ...frag1
              }
            }
            fragment frag1 on InvalidType {
              field2
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Type 'InvalidType' not found in schema.");
    }

    [Fact]
    public void TestInvalidTypeConditionFragmentSpread()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: String }");
        var query = """
            {
              field1 {
                ... on InvalidType {
                  field2
                }
              }
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Type 'InvalidType' not found in schema.");
    }

    [Fact]
    public void TestInvalidField()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: String }");
        var query = """
            {
              invalidField
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Field 'invalidField' not found in type 'Query'.");
    }

    [Fact]
    public void TestInvalidParentType()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: String }");
        var query = """
            {
              field1 {
                field2 {
                  invalidField
                }
              }
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Type 'String' is not an object or interface type.");
    }

    [Fact]
    public void TestInvalidOperation()
    {
        var schema = SchemaFor("type Query { field1: Field1Type } type Field1Type { field2: String }");
        var query = """
            mutation {
              invalidField
            }
            """;
        Should.Throw<InvalidOperationException>(
            () => Analyze(GraphQLParser.Parser.Parse(query), schema, noRules: true))
            .Message.ShouldBe("Schema is not configured for operation type: Mutation");
    }

    [Theory]
    [InlineData(100, "type Query { field1: String }", "{ field1 }", null, 1.5, 1.5, 4, 1.5, 1)]
    [InlineData(101, "type Query { field1: String field2: String field3: String }", "{ field1 field2 field3 }", null, 1.5, 1.5, 4, 4.5, 1)]
    [InlineData(110, "type Query { field1: String @complexity(value: 5) }", "{ field1 }", null, 1.5, 1.5, 4, 5, 1)]
    [InlineData(111, "type Query { field1: String @complexity(value: 5) field2: String field3: String }", "{ field1 field2 field3 }", null, 1.5, 1.5, 4, 8, 1)]
    [InlineData(112, "type Query { field1: String @complexity(value: 0) }", "{ field1 }", null, 1.5, 1.5, 4, 0, 1)]
    [InlineData(113, "type Query { field1: String @complexity(value: 0) field2: String field3: String }", "{ field1 field2 field3 }", null, 1.5, 1.5, 4, 3, 1)]
    [InlineData(120, "type Query { field1: Field1Type } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 3, 2)]
    [InlineData(121, "type Query { field1: Field1Type } type Field1Type { field2: String field3: String field4: String }", "{ field1 { field2 field3 field4 } }", null, 1.5, 1.5, 4, 6, 2)]
    [InlineData(130, "type Query { field1: Field1Type @complexity(value: 5) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 6.5, 2)]
    [InlineData(131, "type Query { field1: Field1Type @complexity(value: 5) } type Field1Type { field2: String field3: String field4: String }", "{ field1 { field2 field3 field4 } }", null, 1.5, 1.5, 4, 9.5, 2)]
    [InlineData(132, "type Query { field1: Field1Type @complexity(value: 0) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 1.5, 2)]
    [InlineData(133, "type Query { field1: Field1Type @complexity(value: 0) } type Field1Type { field2: String field3: String field4: String }", "{ field1 { field2 field3 field4 } }", null, 1.5, 1.5, 4, 4.5, 2)]
    [InlineData(134, "type Query { field1: Field1Type @complexity(value: 1) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 2.5, 2)]
    [InlineData(135, "type Query { field1: Field1Type @complexity(value: 1) } type Field1Type { field2: String field3: String field4: String }", "{ field1 { field2 field3 field4 } }", null, 1.5, 1.5, 4, 5.5, 2)]
    [InlineData(140, "type Query { field1: [Field1Type] } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 7.5, 2)]
    [InlineData(141, "type Query { field1: [Field1Type] } type Field1Type { field2: String @complexity(value: 5) }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 21.5, 2)]
    [InlineData(142, "type Query { field1: [Field1Type] @complexity(value: 5) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 11, 2)]
    [InlineData(143, "type Query { field1: [Field1Type] @complexity(value: 0) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 6, 2)]
    [InlineData(144, "type Query { field1: [Field1Type] @complexity(children: 5) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 9, 2)]
    [InlineData(145, "type Query { field1: [Field1Type] @complexity(children: 1) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 3, 2)]
    [InlineData(146, "type Query { field1: [Field1Type] @complexity(children: 0) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 1.5, 2)]
    [InlineData(147, "type Query { field1: [Field1Type] @complexity(value: 0, children: 0) } type Field1Type { field2: String }", "{ field1 { field2 } }", null, 1.5, 1.5, 4, 0, 2)]
    [InlineData(150, "type Query { field1(id: ID): Field1Type } type Field1Type { field2: String }", "{ field1(id: 500) { field2 } }", null, 1.5, 1.5, 4, 3, 2)]
    [InlineData(151, "type Query { field1(id: ID): Field1Type @complexity(value: 5) } type Field1Type { field2: String }", "{ field1(id: 500) { field2 } }", null, 1.5, 1.5, 4, 6.5, 2)]
    [InlineData(152, "type Query { field1(id: ID): Field1Type @complexity(value: 0) } type Field1Type { field2: String }", "{ field1(id: 500) { field2 } }", null, 1.5, 1.5, 4, 1.5, 2)]
    [InlineData(160, "type Query { field1(first: Int): Field1Type } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 3, 2)]
    [InlineData(161, "type Query { field1(first: Int): Field1Type @complexity(value: 5) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 6.5, 2)]
    [InlineData(162, "type Query { field1(first: Int): Field1Type @complexity(value: 0) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 1.5, 2)]
    [InlineData(163, "type Query { field1(first: Int): Field1Type @complexity(value: 1) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 2.5, 2)]
    [InlineData(170, "type Query { field1(last: Int): Field1Type } type Field1Type { field2: [Field3Type] } type Field3Type { field3: String }", "{ field1(last: 10) { field2 { field3 } } }", null, 1.5, 1.5, 4, 18, 3)]
    [InlineData(171, "type Query { field1(last: Int): Field1Type } type Field1Type { field2: [Field3Type] } type Field3Type { field3: String }", "query q($last: Int) { field1(last: $last) { field2 { field3 } } }", """{"last":10}""", 1.5, 1.5, 4, 18, 3)]
    [InlineData(180, "type Query { field1(first: Int): Field1Type } type Field1Type { field2: [Field3Type] } type Field3Type { field3: String }", "{ field1(first: 10) { field2 { field3 } } }", null, 1.5, 1.5, 4, 18, 3)]
    [InlineData(181, "type Query { field1(first: Int): Field1Type @complexity(value: 5) } type Field1Type { field2: [Field3Type] } type Field3Type { field3: String }", "{ field1(first: 10) { field2 { field3 } } }", null, 1.5, 1.5, 4, 21.5, 3)]
    [InlineData(182, "type Query { field1(first: Int): Field1Type @complexity(value: 0) } type Field1Type { field2: [Field3Type] } type Field3Type { field3: String }", "{ field1(first: 10) { field2 { field3 } } }", null, 1.5, 1.5, 4, 16.5, 3)]
    [InlineData(183, "type Query { field1(first: Int): Field1Type @complexity(value: 1) } type Field1Type { field2: [Field3Type] } type Field3Type { field3: String }", "{ field1(first: 10) { field2 { field3 } } }", null, 1.5, 1.5, 4, 17.5, 3)]
    [InlineData(190, "type Query { field1(first: Int): [Field1Type] } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 16.5, 2)]
    [InlineData(191, "type Query { field1(first: Int): [Field1Type] @complexity(value: 5) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 20, 2)]
    [InlineData(192, "type Query { field1(first: Int): [Field1Type] @complexity(value: 0) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 15, 2)]
    [InlineData(193, "type Query { field1(first: Int): [Field1Type] @complexity(value: 1) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 16, 2)]
    [InlineData(194, "type Query { field1(first: Int): [Field1Type] @complexity(children: 5) } type Field1Type { field2: String }", "{ field1(first: 10) { field2 } }", null, 1.5, 1.5, 4, 9, 2)]
    [InlineData(195, "type Query { field1(first: Int): [Field1Type] @complexity(children: 5) } type Field1Type { field2: String }", "{ field1(first: 20) { field2 } }", null, 1.5, 1.5, 4, 9, 2)]
    // test examples provided in documentation
    [InlineData(200, """
        type Query { users(first: Int): [User], products(id: ID): [Products] }
        type User { id: ID, posts: [Post] }
        type Post { id: ID, comments: [Comment] }
        type Comment { id: ID }
        type Products { id: ID, name: String, photos: [Photo], category: Category }
        type Photo { id: ID, name: String }
        type Category { id: ID, name: String }
        """,
        """
        query {
          users(first: 10) { id posts { id comments { id } } }
          products(id: "5") { id name photos { id name } category { id name } }
        }
        """, null, 1, 1, 20, 4468, 4)]
    [InlineData(201, """
        type Query { users: [User] @complexity(value: 1, children: 100) }
        type User { id: ID posts: [Post] }
        type Post { id: ID comments: [Comment] }
        type Comment { id: ID }
        """, "query { users { id posts { id comments { id } } } }", null, 1, 1, 7, 6501, 4)]
    [InlineData(202, """
        type Query { users: [User] @complexity(value: 50) }
        type User { id: ID posts: [Post] }
        type Post { id: ID comments: [Comment] }
        type Comment { id: ID }
        """, "query { users { id posts { id comments { id } } } }", null, 1, 20, 20, 16870, 4)]
    public void TestComplexityCases(int idx, string sdl, string query, string? variables, double scalarImpact, double objectImpact, double listMultiplier, double complexity, int totalQueryDepth)
    {
        _ = idx;
        var schema = SchemaFor(sdl);
        var inputs = new SystemTextJson.GraphQLSerializer().Deserialize<Inputs>(variables) ?? Inputs.Empty;
        var result = Analyze(GraphQLParser.Parser.Parse(query), schema, inputs, c =>
        {
            c.DefaultScalarImpact = scalarImpact;
            c.DefaultObjectImpact = objectImpact;
            c.DefaultListImpactMultiplier = listMultiplier;
        });
        var actual = (result.TotalComplexity, result.MaxDepth);
        actual.ShouldBe((complexity, totalQueryDepth));
    }

    [Fact]
    public void CustomCalculation()
    {
        var product = new ObjectGraphType() { Name = "Product" };
        product.Field<IdGraphType>("id");
        product.Field<StringGraphType>("name");
        var query = new ObjectGraphType() { Name = "Query" };
        query.Field("products", new ListGraphType(product))
            .Argument<IntGraphType>("offset")
            .Argument<IntGraphType>("limit")
            .WithComplexityImpact(context =>
            {
                var fieldImpact = 1;
                var childImpactModifier = context.GetArgument<int>("limit", 20);
                return new(fieldImpact, childImpactModifier);
            });
        var schema = new Schema { Query = query };
        var queryText = "{ products(offset: 10, limit: 5) { id name } }";
        var document = GraphQLParser.Parser.Parse(queryText);
        var result = Analyze(document, schema);
        result.TotalComplexity.ShouldBe(11); // 1 for products + (1*5) for id + (1*5) for name
        result.MaxDepth.ShouldBe(2);
    }

    [Fact]
    public void Attributes()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<MyQuery>());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var queryText = "{ users { id name age weight } }";
        var document = GraphQLParser.Parser.Parse(queryText);
        var result = Analyze(document, schema);
        result.TotalComplexity.ShouldBe(98);
        result.MaxDepth.ShouldBe(2);
    }

    private class MyQuery
    {
        [Complexity(3, 10)]
        public static IEnumerable<MyUser> Users => null!;
    }

    public class MyUser
    {
        [Complexity(0.5)]
        public int Id => 0;
        [Complexity(5)]
        public string Name => "test";
        [Complexity(typeof(MyFieldAnalyzer))]
        public int Age => 10;
#if !NETFRAMEWORK
        [Complexity<MyFieldAnalyzer>]
#else
        [Complexity(typeof(MyFieldAnalyzer))]
#endif
        public int Weight => 150;
    }

    private class MyFieldAnalyzer : IFieldComplexityAnalyzer
    {
        public FieldComplexityResult Analyze(FieldImpactContext context) => new(2, 2);
    }

    private (double TotalComplexity, double MaxDepth) Analyze(GraphQLDocument document, ISchema schema, Inputs? variables = null, Action<ComplexityOptions>? configure = null, bool noRules = false)
    {
        var validationOptions = new ValidationOptions
        {
            Document = document,
            Operation = document.Operation(),
            Schema = schema,
            Variables = variables ?? Inputs.Empty,
            Rules = !noRules ? null : Array.Empty<IValidationRule>(),
        };
        var validationResult = new DocumentValidator().ValidateAsync(validationOptions).GetAwaiter().GetResult();
        if (validationResult.Errors.Count > 0)
        {
            validationResult.Errors[0].Message.ShouldBeNull();
        }
        var validationContext = new ValidationContext
        {
            ArgumentValues = validationResult.ArgumentValues as Dictionary<GraphQLField, IDictionary<string, ArgumentValue>>,
            DirectiveValues = validationResult.DirectiveValues as Dictionary<ASTNode, IDictionary<string, DirectiveInfo>>,
            Document = document,
            Operation = document.Operation(),
            Schema = schema,
            Variables = variables ?? Inputs.Empty,
        };
        var complexityConfiguration = new ComplexityOptions();
        configure?.Invoke(complexityConfiguration);
        return ComplexityVisitor.RunAsync(validationContext, complexityConfiguration).GetAwaiter().GetResult();
    }

    private ISchema SchemaFor(string sdl)
    {
        sdl += """

            directive @complexity(
                value: Float,
                children: Float,
            ) on FIELD_DEFINITION
            """;
        var schema = Schema.For(sdl);
        schema.RegisterVisitor(new DirectiveToComplexityVisitor());
        schema.Initialize();
        return schema;
    }

    private class DirectiveToComplexityVisitor : GraphQL.Utilities.BaseSchemaNodeVisitor
    {
        public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
        {
            var impact = field.GetAppliedDirectives()?.Find("complexity")?.FindArgument("value")?.Value;
            var listImpact = field.GetAppliedDirectives()?.Find("complexity")?.FindArgument("children")?.Value;
            if (impact != null && listImpact == null)
            {
                field.WithComplexityImpact((double)Convert.ChangeType(impact, typeof(double)));
            }
            else if (impact == null && listImpact != null)
            {
                var listImpact2 = (double)Convert.ChangeType(listImpact, typeof(double));
                field.WithComplexityImpact(context =>
                {
                    var impact = context.Configuration.DefaultObjectImpact;
                    return new(impact, listImpact2);
                });
            }
            else if (impact != null && listImpact != null)
            {
                field.WithComplexityImpact(
                    (double)Convert.ChangeType(impact, typeof(double)),
                    (double)Convert.ChangeType(listImpact, typeof(double)));
            }
        }
    }
}
