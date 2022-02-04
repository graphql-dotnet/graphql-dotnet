using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Lone anonymous operation:
    ///
    /// A GraphQL document is only valid if when it contains an anonymous operation
    /// (the query short-hand) that it contains only that one operation definition.
    /// </summary>
    public class LoneAnonymousOperation : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly LoneAnonymousOperation Instance = new();

        /// <inheritdoc/>
        /// <exception cref="LoneAnonymousOperationError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLOperationDefinition>((op, context) =>
        {
            if (op.Name is null && context.Document.OperationsCount() > 1)
            {
                context.ReportError(new LoneAnonymousOperationError(context, op));
            }
        });
    }
}
