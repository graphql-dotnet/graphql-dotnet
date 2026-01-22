using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

/// <summary>
/// Tests for custom validation rule examples from the documentation.
/// These tests ensure that the documented examples are functional and correct.
/// </summary>
public class CustomValidationExamplesTests
{
    #region Example 3: MaxDepthValidationRule

    /// <summary>
    /// Validates that queries do not exceed a maximum depth to prevent deeply nested queries
    /// that could cause performance issues.
    /// </summary>
    public class MaxDepthValidationRule : ValidationRuleBase, INodeVisitor
    {
        private readonly int _maxDepth;
        private int _currentDepth;

        public MaxDepthValidationRule(int maxDepth = 10)
        {
            _maxDepth = maxDepth;
        }

        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        {
            _currentDepth = 0;
            return new ValueTask<INodeVisitor?>(this);
        }

        ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField or GraphQLFragmentDefinition or GraphQLInlineFragment)
            {
                _currentDepth++;
                if (_currentDepth > _maxDepth)
                {
                    context.ReportError(new ValidationError(
                        $"Query exceeds maximum depth of {_maxDepth}. Deeply nested queries can cause performance issues."));
                }
            }
            return default;
        }

        ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField or GraphQLFragmentDefinition or GraphQLInlineFragment)
            {
                _currentDepth--;
            }
            return default;
        }
    }

    public class MaxDepthValidationRuleTests : ValidationTestBase<MaxDepthValidationRule, ValidationSchema>
    {
        [Fact]
        public void accepts_shallow_queries()
        {
            ShouldPassRule("""
                {
                  dog {
                    name
                  }
                }
                """);
        }

        [Fact]
        public void rejects_deep_queries()
        {
            var rule = new MaxDepthValidationRule(maxDepth: 2);
            ShouldFailRule(_ =>
            {
                _.Rule(rule);
                _.Query = """
                    {
                      dog {
                        owner {
                          name
                        }
                      }
                    }
                    """;
                _.Error(
                    message: "Query exceeds maximum depth of 2. Deeply nested queries can cause performance issues.",
                    line: 4,
                    column: 5);
            });
        }
    }

    #endregion

    #region Example 4: RequiresAuthenticationValidationRule

    /// <summary>
    /// Validates that authenticated fields are only accessed when a user is authenticated.
    /// This rule checks for fields marked with "requiresAuth" metadata.
    /// </summary>
    public class RequiresAuthenticationValidationRule : ValidationRuleBase
    {
        private static readonly INodeVisitor _visitor = new MatchingNodeVisitor<GraphQLField>(
            (fieldNode, context) =>
            {
                var fieldDef = context.TypeInfo.GetFieldDef();
                if (fieldDef == null)
                    return;

                // Check if the field requires authentication
                var requiresAuth = fieldDef.GetMetadata<bool>("requiresAuth", false);
                if (!requiresAuth)
                    return;

                // Check if user is authenticated (assumes UserContext has IsAuthenticated property)
                var isAuthenticated = context.UserContext?.GetType()
                    .GetProperty("IsAuthenticated")
                    ?.GetValue(context.UserContext) as bool? ?? false;

                if (!isAuthenticated)
                {
                    context.ReportError(new ValidationError(
                        $"Field '{fieldDef.Name}' requires authentication.",
                        fieldNode));
                }
            });

        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);
    }

    public class AuthTestSchema : Schema
    {
        public AuthTestSchema()
        {
            Query = new AuthTestQuery();
        }
    }

    public class AuthTestQuery : ObjectGraphType
    {
        public AuthTestQuery()
        {
            Field<StringGraphType>("publicField")
                .Resolve(_ => "public data");

            Field<StringGraphType>("privateField")
                .WithMetadata("requiresAuth", true)
                .Resolve(_ => "private data");
        }
    }

    public class AuthUserContext
    {
        public bool IsAuthenticated { get; set; }
    }

    public class RequiresAuthenticationValidationRuleTests : ValidationTestBase<RequiresAuthenticationValidationRule, AuthTestSchema>
    {
        [Fact]
        public void allows_public_fields_without_auth()
        {
            ShouldPassRule("""
                {
                  publicField
                }
                """);
        }

        [Fact]
        public void allows_private_fields_with_auth()
        {
            ShouldPassRule(config =>
            {
                config.Query = """
                    {
                      privateField
                    }
                    """;
                config.UserContext = new AuthUserContext { IsAuthenticated = true };
            });
        }

        [Fact]
        public void rejects_private_fields_without_auth()
        {
            ShouldFailRule(_ =>
            {
                _.Query = """
                    {
                      privateField
                    }
                    """;
                _.UserContext = new AuthUserContext { IsAuthenticated = false };
                _.Error(
                    message: "Field 'privateField' requires authentication.",
                    line: 2,
                    column: 3);
            });
        }
    }

    #endregion

    #region Example 5: DateRangeValidationRule

    /// <summary>
    /// Validates that when filtering by date range, both start and end dates are provided.
    /// </summary>
    public class DateRangeValidationRule : ValidationRuleBase
    {
        private static readonly INodeVisitor _visitor = new MatchingNodeVisitor<GraphQLField>(
            (fieldNode, context) =>
            {
                var fieldDef = context.TypeInfo.GetFieldDef();
                if (fieldDef == null)
                    return;

                // Check if this field has date range arguments
                var hasStartDate = fieldDef.Arguments?.Any(a => a.Name == "startDate") ?? false;
                var hasEndDate = fieldDef.Arguments?.Any(a => a.Name == "endDate") ?? false;

                if (!hasStartDate || !hasEndDate)
                    return;

                // Check which arguments are provided in the query
                var providedArgNames = fieldNode.Arguments?
                    .Select(arg => arg.Name.Value.ToString())
                    .ToHashSet() ?? new HashSet<string>();

                var hasStartDateArg = providedArgNames.Contains("startDate");
                var hasEndDateArg = providedArgNames.Contains("endDate");

                // If one is provided, both must be provided
                if (hasStartDateArg != hasEndDateArg)
                {
                    context.ReportError(new ValidationError(
                        $"Field '{fieldDef.Name}' requires both 'startDate' and 'endDate' arguments when filtering by date range.",
                        fieldNode));
                }
            });

        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);
    }

    public class DateRangeTestSchema : Schema
    {
        public DateRangeTestSchema()
        {
            Query = new DateRangeTestQuery();
        }
    }

    public class DateRangeTestQuery : ObjectGraphType
    {
        public DateRangeTestQuery()
        {
            Field<ListGraphType<StringGraphType>>("events")
                .Argument<DateTimeGraphType>("startDate")
                .Argument<DateTimeGraphType>("endDate")
                .Resolve(_ => new[] { "event1", "event2" });
        }
    }

    public class DateRangeValidationRuleTests : ValidationTestBase<DateRangeValidationRule, DateRangeTestSchema>
    {
        [Fact]
        public void allows_both_dates_provided()
        {
            ShouldPassRule("""
                {
                  events(startDate: "2024-01-01", endDate: "2024-12-31")
                }
                """);
        }

        [Fact]
        public void allows_neither_date_provided()
        {
            ShouldPassRule("""
                {
                  events
                }
                """);
        }

        [Fact]
        public void rejects_only_start_date()
        {
            ShouldFailRule(_ =>
            {
                _.Query = """
                    {
                      events(startDate: "2024-01-01")
                    }
                    """;
                _.Error(
                    message: "Field 'events' requires both 'startDate' and 'endDate' arguments when filtering by date range.",
                    line: 2,
                    column: 3);
            });
        }

        [Fact]
        public void rejects_only_end_date()
        {
            ShouldFailRule(_ =>
            {
                _.Query = """
                    {
                      events(endDate: "2024-12-31")
                    }
                    """;
                _.Error(
                    message: "Field 'events' requires both 'startDate' and 'endDate' arguments when filtering by date range.",
                    line: 2,
                    column: 3);
            });
        }
    }

    #endregion

    #region Example 6: MutationValidationRule

    /// <summary>
    /// Validates multiple aspects of mutation operations in a single rule.
    /// </summary>
    public class MutationValidationRule : ValidationRuleBase
    {
        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            // Ensure mutations are not executed in batches (more than one mutation in an operation)
            new MatchingNodeVisitor<GraphQLOperationDefinition>((operation, context) =>
            {
                if (operation.Operation == GraphQLParser.AST.OperationType.Mutation)
                {
                    if (operation.SelectionSet.Selections.Count > 1)
                    {
                        context.ReportError(new ValidationError(
                            "Only one mutation operation is allowed per request.",
                            operation));
                    }
                }
            }),

            // Ensure mutation fields have required arguments
            new MatchingNodeVisitor<GraphQLField>((fieldNode, context) =>
            {
                if (context.Operation?.Operation != GraphQLParser.AST.OperationType.Mutation)
                    return;

                var fieldDef = context.TypeInfo.GetFieldDef();
                if (fieldDef == null)
                    return;

                // Check for required confirmation argument on delete operations
                if (fieldDef.Name.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
                {
                    var hasConfirm = fieldNode.Arguments?
                        .Any(arg => arg.Name.Value.ToString() == "confirm") ?? false;

                    if (!hasConfirm)
                    {
                        context.ReportError(new ValidationError(
                            $"Mutation '{fieldDef.Name}' requires a 'confirm' argument.",
                            fieldNode));
                    }
                }
            })
        );
    }

    public class MutationTestSchema : Schema
    {
        public MutationTestSchema()
        {
            Query = new MutationTestQuery();
            Mutation = new MutationTestMutation();
        }
    }

    public class MutationTestQuery : ObjectGraphType
    {
        public MutationTestQuery()
        {
            Field<StringGraphType>("hello")
                .Resolve(_ => "world");
        }
    }

    public class MutationTestMutation : ObjectGraphType
    {
        public MutationTestMutation()
        {
            Field<StringGraphType>("createUser")
                .Argument<NonNullGraphType<StringGraphType>>("name")
                .Resolve(_ => "user created");

            Field<StringGraphType>("deleteUser")
                .Argument<NonNullGraphType<IdGraphType>>("id")
                .Argument<BooleanGraphType>("confirm")
                .Resolve(_ => "user deleted");
        }
    }

    public class MutationValidationRuleTests : ValidationTestBase<MutationValidationRule, MutationTestSchema>
    {
        [Fact]
        public void allows_single_mutation()
        {
            ShouldPassRule("""
                mutation {
                  createUser(name: "John")
                }
                """);
        }

        [Fact]
        public void allows_delete_with_confirm()
        {
            ShouldPassRule("""
                mutation {
                  deleteUser(id: "1", confirm: true)
                }
                """);
        }

        [Fact]
        public void rejects_multiple_mutations()
        {
            ShouldFailRule(_ =>
            {
                _.Query = """
                    mutation {
                      createUser(name: "John")
                      deleteUser(id: "1", confirm: true)
                    }
                    """;
                _.Error(
                    message: "Only one mutation operation is allowed per request.",
                    line: 1,
                    column: 1);
            });
        }

        [Fact]
        public void rejects_delete_without_confirm()
        {
            ShouldFailRule(_ =>
            {
                _.Query = """
                    mutation {
                      deleteUser(id: "1")
                    }
                    """;
                _.Error(
                    message: "Mutation 'deleteUser' requires a 'confirm' argument.",
                    line: 2,
                    column: 3);
            });
        }
    }

    #endregion
}
