using System.Security.Claims;
using CustomValidationRules.Rules;
using CustomValidationRules.Schema;
using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;

namespace CustomValidationRules;

/// <summary>
/// Demonstrates how to create and use custom validation rules with GraphQL.NET v8.
/// <para>
/// This sample shows four key patterns:
/// <list type="number">
///   <item><b>Pre-node visitor</b> — field-level access control (authentication/roles)</item>
///   <item><b>Stateful visitor</b> — query depth limiting</item>
///   <item><b>Post-node visitor</b> — argument validation with parsed argument values</item>
///   <item><b>Variable visitor</b> — custom variable parsing/validation</item>
/// </list>
/// </para>
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== GraphQL.NET Custom Validation Rules Sample ===\n");

        // ──────────────────────────────────────────────────────────────────────
        // Example 1: Authentication-based field access control
        // ──────────────────────────────────────────────────────────────────────
        Console.WriteLine("--- Example 1: Requires Authentication Rule ---");

        await DemonstrateAuthenticationRule();

        // ──────────────────────────────────────────────────────────────────────
        // Example 2: Query depth limiting
        // ──────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n--- Example 2: Max Query Depth Rule ---");

        await DemonstrateMaxDepthRule();

        // ──────────────────────────────────────────────────────────────────────
        // Example 3: Using field-level Parser/Validator and ValidateArguments
        // ──────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n--- Example 3: Field-level Parser/Validator ---");

        await DemonstrateFieldLevelValidation();

        // ──────────────────────────────────────────────────────────────────────
        // Example 4: Role-based access control
        // ──────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n--- Example 4: Role-based Access Control ---");

        await DemonstrateRoleBasedAccess();

        Console.WriteLine("\n=== All examples completed ===");
    }

    /// <summary>
    /// Demonstrates the <see cref="RequiresAuthenticationRule"/>.
    /// Shows how an unauthenticated request is rejected for protected fields,
    /// while an authenticated request is allowed.
    /// </summary>
    static async Task DemonstrateAuthenticationRule()
    {
        var schema = BuildSampleSchema();
        var rule = new RequiresAuthenticationRule();

        // 1a. Unauthenticated request — should fail for protected fields
        Console.WriteLine("  Unauthenticated request:");
        var result = await ExecuteQueryAsync(
            schema,
            "{ publicData secretData }",
            rules: DocumentValidator.CoreRules.Append(rule),
            user: null); // Not authenticated

        PrintResult(result);

        // 1b. Authenticated request — should succeed
        Console.WriteLine("  Authenticated request:");
        result = await ExecuteQueryAsync(
            schema,
            "{ publicData secretData }",
            rules: DocumentValidator.CoreRules.Append(rule),
            user: new ClaimsPrincipal(new ClaimsIdentity("Bearer")));

        PrintResult(result);
    }

    /// <summary>
    /// Demonstrates the <see cref="MaxQueryDepthRule"/>.
    /// Shows how queries exceeding the depth limit are rejected.
    /// </summary>
    static async Task DemonstrateMaxDepthRule()
    {
        var schema = BuildSampleSchema();
        var rule = new MaxQueryDepthRule(maxDepth: 3);

        // 2a. Shallow query — should pass
        Console.WriteLine("  Shallow query (depth 2):");
        var result = await ExecuteQueryAsync(
            schema,
            "{ hero { name } }",
            rules: DocumentValidator.CoreRules.Append(rule));

        PrintResult(result);

        // 2b. Deep query — should be rejected
        Console.WriteLine("  Deep query (depth 4):");
        result = await ExecuteQueryAsync(
            schema,
            "{ hero { friends { friends { name } } } }",
            rules: DocumentValidator.CoreRules.Append(rule));

        PrintResult(result);
    }

    /// <summary>
    /// Demonstrates field-level Parser, Validator, and ValidateArguments
    /// using the fluent builder API and attributes.
    /// </summary>
    static async Task DemonstrateFieldLevelValidation()
    {
        // Build a schema that uses field-level validation
        var queryType = new ObjectGraphType { Name = "Query" };

        // Using the fluent builder to set ValidateArguments
        queryType.Field<StringGraphType>("greet")
            .Argument<StringGraphType>("name")
            .ValidateArguments(ctx =>
            {
                var name = ctx.GetArgument<string>("name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    ctx.ReportError(new ValidationError(
                        ctx.ValidationContext.Document.Source,
                        "INVALID_NAME",
                        "The 'name' argument must not be empty."));
                }
            })
            .Resolve(ctx => $"Hello, {ctx.GetArgument<string>("name")}!");

        var schema = new Schema { Query = queryType };

        // 3a. Valid argument
        Console.WriteLine("  Valid argument:");
        var result = await ExecuteQueryAsync(
            schema,
            """{ greet(name: "World") }""",
            rules: DocumentValidator.CoreRules);

        PrintResult(result);

        // 3b. Invalid argument (empty name)
        Console.WriteLine("  Invalid argument (empty name):");
        result = await ExecuteQueryAsync(
            schema,
            """{ greet(name: "") }""",
            rules: DocumentValidator.CoreRules);

        PrintResult(result);
    }

    /// <summary>
    /// Demonstrates the <see cref="RoleBasedAccessRule"/>.
    /// </summary>
    static async Task DemonstrateRoleBasedAccess()
    {
        var queryType = new ObjectGraphType { Name = "Query" };

        // Public field
        queryType.Field<StringGraphType>("info")
            .Resolve(_ => "Public information");

        // Admin-only field (using metadata to indicate required roles)
        queryType.Field<StringGraphType>("adminPanel")
            .WithMetadata(RoleBasedAccessRule.REQUIRED_ROLES_METADATA_KEY, "Admin")
            .Resolve(_ => "Admin dashboard");

        // Editor or Admin field
        queryType.Field<StringGraphType>("editContent")
            .WithMetadata(RoleBasedAccessRule.REQUIRED_ROLES_METADATA_KEY, "Admin,Editor")
            .Resolve(_ => "Content editor");

        var schema = new Schema { Query = queryType };
        var rule = new RoleBasedAccessRule();

        // 4a. User with insufficient role
        Console.WriteLine("  User with Viewer role:");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Viewer"),
        }, "Bearer"));

        var result = await ExecuteQueryAsync(
            schema,
            "{ info adminPanel }",
            rules: DocumentValidator.CoreRules.Append(rule),
            user: user);

        PrintResult(result);

        // 4b. User with Admin role
        Console.WriteLine("  User with Admin role:");
        user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
        }, "Bearer"));

        result = await ExecuteQueryAsync(
            schema,
            "{ info adminPanel editContent }",
            rules: DocumentValidator.CoreRules.Append(rule),
            user: user);

        PrintResult(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper methods
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a sample schema for demonstration purposes.
    /// </summary>
    static ISchema BuildSampleSchema()
    {
        var queryType = new ObjectGraphType { Name = "Query" };

        // Public field — no authentication required
        queryType.Field<StringGraphType>("publicData")
            .Resolve(_ => "This is public data");

        // Protected field — requires authentication (via metadata)
        queryType.Field<StringGraphType>("secretData")
            .WithMetadata(RequiresAuthenticationRule.REQUIRES_AUTH_METADATA_KEY, true)
            .Resolve(_ => "This is secret data");

        // Hero field for depth testing — uses the HeroGraphType defined below
        queryType.Field<HeroGraphType>("hero")
            .Resolve(_ => new { Name = "Luke Skywalker" });

        return new Schema { Query = queryType };
    }

    /// <summary>
    /// Executes a GraphQL query with the specified validation rules.
    /// </summary>
    static async Task<ExecutionResult> ExecuteQueryAsync(
        ISchema schema,
        string query,
        IEnumerable<IValidationRule>? rules = null,
        ClaimsPrincipal? user = null)
    {
        var executer = new DocumentExecuter();
        var options = new ExecutionOptions
        {
            Schema = schema,
            Query = query,
            ValidationRules = rules,
            User = user,
        };

        return await executer.ExecuteAsync(options);
    }

    /// <summary>
    /// Prints the execution result to the console.
    /// </summary>
    static void PrintResult(ExecutionResult result)
    {
        if (result.Errors?.Count > 0)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"    ❌ Error: {error.Message}");
            }
        }
        else
        {
            Console.WriteLine($"    ✅ Success: {result.Data}");
        }
    }
}

/// <summary>
/// Simple graph type for Hero, used in depth-testing examples.
/// </summary>
public class HeroGraphType : ObjectGraphType
{
    public HeroGraphType()
    {
        Name = "Hero";
        Field<StringGraphType>("name");
        Field<ListGraphType<HeroGraphType>>("friends");
    }
}
