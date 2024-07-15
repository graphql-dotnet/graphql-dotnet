using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Rules.Custom;
using GraphQLParser.AST;

namespace GraphQL.Tests.Complexity;

public class ComplexityTests
{
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

    private (double TotalComplexity, double MaxDepth) Analyze(GraphQLDocument document, ISchema schema, Inputs variables, Action<ComplexityConfiguration>? configure = null)
    {
        var validationOptions = new ValidationOptions
        {
            Document = document,
            Operation = document.Operation(),
            Schema = schema,
            Variables = variables
        };
        var validationResult = new DocumentValidator().ValidateAsync(validationOptions).GetAwaiter().GetResult();
        validationResult.Errors.Count.ShouldBe(0);
        var validationContext = new ValidationContext
        {
            ArgumentValues = validationResult.ArgumentValues as Dictionary<GraphQLField, IDictionary<string, ArgumentValue>>,
            DirectiveValues = validationResult.DirectiveValues as Dictionary<ASTNode, IDictionary<string, DirectiveInfo>>,
            Document = document,
            Operation = document.Operation(),
            Schema = schema,
            Variables = variables,
        };
        var complexityConfiguration = new ComplexityConfiguration();
        configure?.Invoke(complexityConfiguration);
        return ComplexityValidationRule.ComplexityVisitor.RunAsync(validationContext, complexityConfiguration).GetAwaiter().GetResult();
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

    private class DirectiveToComplexityVisitor : BaseSchemaNodeVisitor
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
                    return (impact, listImpact2);
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
