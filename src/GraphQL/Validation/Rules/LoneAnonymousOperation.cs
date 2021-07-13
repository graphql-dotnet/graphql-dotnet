#nullable enable

using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public static readonly LoneAnonymousOperation Instance = new LoneAnonymousOperation();

        /// <inheritdoc/>
        /// <exception cref="LoneAnonymousOperationError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<Operation>((op, context) =>
        {
            if (string.IsNullOrWhiteSpace(op.Name) && context.Document.Operations.Count > 1)
            {
                context.ReportError(new LoneAnonymousOperationError(context, op));
            }
        }).ToTask();
    }
}
