using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Unique argument names:
///
/// A GraphQL field or directive is only valid if all supplied arguments at a given field
/// are uniquely named.
/// </summary>
public class UniqueArgumentNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly UniqueArgumentNames Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="UniqueArgumentNames"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public UniqueArgumentNames()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="UniqueArgumentNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLField>((__, context) => context.TypeInfo.UniqueArgumentNames_KnownArgs?.Clear()),
        new MatchingNodeVisitor<GraphQLDirective>((__, context) => context.TypeInfo.UniqueArgumentNames_KnownArgs?.Clear()),
        new MatchingNodeVisitor<GraphQLArgument>((argument, context) =>
        {
            var knownArgs = context.TypeInfo.UniqueArgumentNames_KnownArgs ??= new();
            var argName = argument.Name;
            if (knownArgs.TryGetValue(argName, out var arg))
            {
                context.ReportError(new UniqueArgumentNamesError(context, arg, argument));
            }
            else
            {
                knownArgs[argName] = argument;
            }
        })
    );
}
