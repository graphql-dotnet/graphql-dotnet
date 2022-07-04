using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique argument names:
    ///
    /// A GraphQL field or directive is only valid if all supplied arguments at a given field
    /// are uniquely named.
    /// </summary>
    public class UniqueArgumentNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueArgumentNames Instance = new();

        /// <inheritdoc/>
        /// <exception cref="UniqueArgumentNamesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

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
}
