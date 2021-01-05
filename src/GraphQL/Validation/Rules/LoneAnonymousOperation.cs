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

        private static readonly Task<INodeVisitor> _task = new MatchingNodeVisitor<Operation>(
            enter: (op, context) =>
            {
                if (string.IsNullOrWhiteSpace(op.Name))
                {
                    context.ReportError(new LoneAnonymousOperationError(context, op));
                }
            },
            shouldRun: context => context.Document.Operations.Count > 1
            ).ToTask();

        /// <inheritdoc/>
        /// <exception cref="LoneAnonymousOperationError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
