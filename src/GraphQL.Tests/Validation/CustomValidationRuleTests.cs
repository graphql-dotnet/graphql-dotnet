using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

/// <summary>
/// Tests for the custom validation rule examples shown in the documentation at
/// docs2/site/docs/getting-started/query-validation.md.
/// These tests verify that the documented code samples are functional.
/// </summary>
public class CustomValidationRuleTests
{
    // -----------------------------------------------------------------------
    // Example 1: Disabling Introspection Requests
    // Documented in query-validation.md under "Example 1"
    // -----------------------------------------------------------------------

    /// <summary>
    /// Custom validation rule from the documentation that disables introspection.
    /// This matches the INodeVisitor implementation in "Example 1" of query-validation.md.
    /// </summary>
    private class NoIntrospectionValidationRule : ValidationRuleBase, INodeVisitor
    {
        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(this);

        ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField field)
            {
                if (field.Name.Value == "__schema" || field.Name.Value == "__type")
                    context.ReportError(new ValidationError("Introspection queries are not allowed."));
            }
            return default;
        }

        ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
    }

    /// <summary>
    /// Alternative implementation using MatchingNodeVisitor, also documented in Example 1.
    /// </summary>
    private class NoIntrospectionValidationRuleAlt : ValidationRuleBase
    {
        private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
            (field, context) =>
            {
                if (field.Name.Value == "__schema" || field.Name.Value == "__type")
                    context.ReportError(new ValidationError("Introspection queries are not allowed."));
            });

        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);
    }

    [Fact]
    public void NoIntrospection_allows_non_introspection_queries()
    {
        var result = Validate("{ __typename }", new NoIntrospectionValidationRule());
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void NoIntrospection_blocks_schema_field()
    {
        var result = Validate("{ __schema { description } }", new NoIntrospectionValidationRule());
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Introspection queries are not allowed.");
    }

    [Fact]
    public void NoIntrospection_blocks_type_field()
    {
        var result = Validate("""{ __type(name: "Query") { kind } }""", new NoIntrospectionValidationRule());
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Introspection queries are not allowed.");
    }

    [Fact]
    public void NoIntrospection_alt_allows_non_introspection_queries()
    {
        var result = Validate("{ __typename }", new NoIntrospectionValidationRuleAlt());
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void NoIntrospection_alt_blocks_schema_field()
    {
        var result = Validate("{ __schema { description } }", new NoIntrospectionValidationRuleAlt());
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Introspection queries are not allowed.");
    }

    // -----------------------------------------------------------------------
    // Example 2: Limiting Connections to Under 1000 Rows
    // Documented in query-validation.md under "Example 2"
    // -----------------------------------------------------------------------

    /// <summary>
    /// Schema with a connection-style field for testing row-limit validation.
    /// </summary>
    private class ConnectionSchema : Schema
    {
        public ConnectionSchema()
        {
            var userType = new ObjectGraphType { Name = "User" };
            userType.Field<StringGraphType>("name");

            var userConnectionType = new ObjectGraphType { Name = "UserConnection" };
            userConnectionType.Field(
                new FieldType
                {
                    Name = "nodes",
                    ResolvedType = new ListGraphType(userType),
                });

            var queryType = new ObjectGraphType { Name = "Query" };
            queryType.Field(
                new FieldType
                {
                    Name = "users",
                    ResolvedType = userConnectionType,
                    Arguments = new QueryArguments(
                        new QueryArgument<IntGraphType> { Name = "first" },
                        new QueryArgument<IntGraphType> { Name = "last" }
                    )
                });

            Query = queryType;
        }
    }

    /// <summary>
    /// Validation rule from the documentation that limits connections to under 1000 rows.
    /// This matches "Example 2" in query-validation.md.
    /// </summary>
    private class NoConnectionOver1000ValidationRule : ValidationRuleBase, INodeVisitor
    {
        public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
            => context.ArgumentValues != null ? new(this) : default;

        ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is not GraphQLField fieldNode)
                return default;

            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef == null || fieldDef.ResolvedType?.GetNamedType() is not IObjectGraphType connectionType || !connectionType.Name.EndsWith("Connection"))
                return default;

            if (!(context.ArgumentValues?.TryGetValue(fieldNode, out var args) ?? false))
                return default;

            ArgumentValue lastArg = default;
            if (!args.TryGetValue("first", out var firstArg) && !args.TryGetValue("last", out lastArg))
                return default;

            var rows = (int?)firstArg.Value ?? (int?)lastArg.Value ?? 0;
            if (rows > 1000)
                context.ReportError(new ValidationError("Cannot return more than 1000 rows"));

            return default;
        }

        ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
    }

    [Fact]
    public void ConnectionLimit_allows_queries_under_limit()
    {
        var schema = new ConnectionSchema();
        schema.Initialize();
        var result = Validate("{ users(first: 10) { nodes { name } } }", new NoConnectionOver1000ValidationRule(), schema);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ConnectionLimit_allows_queries_at_limit()
    {
        var schema = new ConnectionSchema();
        schema.Initialize();
        var result = Validate("{ users(first: 1000) { nodes { name } } }", new NoConnectionOver1000ValidationRule(), schema);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ConnectionLimit_blocks_queries_over_limit_using_first()
    {
        var schema = new ConnectionSchema();
        schema.Initialize();
        var result = Validate("{ users(first: 1001) { nodes { name } } }", new NoConnectionOver1000ValidationRule(), schema);
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Cannot return more than 1000 rows");
    }

    [Fact]
    public void ConnectionLimit_blocks_queries_over_limit_using_last()
    {
        var schema = new ConnectionSchema();
        schema.Initialize();
        var result = Validate("{ users(last: 2000) { nodes { name } } }", new NoConnectionOver1000ValidationRule(), schema);
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Cannot return more than 1000 rows");
    }

    [Fact]
    public void ConnectionLimit_allows_queries_without_paging_args()
    {
        var schema = new ConnectionSchema();
        schema.Initialize();
        var result = Validate("{ users { nodes { name } } }", new NoConnectionOver1000ValidationRule(), schema);
        result.IsValid.ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // Helper methods
    // -----------------------------------------------------------------------

    private static IValidationResult Validate(string query, IValidationRule rule, ISchema? schema = null)
    {
        var simpleSchema = schema ?? CreateSimpleSchema();
        var documentBuilder = new GraphQLDocumentBuilder();
        var document = documentBuilder.Build(query);
        var validator = new DocumentValidator();
        return validator.ValidateAsync(new ValidationOptions
        {
            Schema = simpleSchema,
            Document = document,
            Rules = new[] { rule },
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().FirstOrDefault()!,
            Variables = Inputs.Empty
        }).GetAwaiter().GetResult();
    }

    private static ISchema CreateSimpleSchema()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("hello");
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        return schema;
    }
}
