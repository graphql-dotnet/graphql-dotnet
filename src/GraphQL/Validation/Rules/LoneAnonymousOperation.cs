using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Lone anonymous operation
    ///
    /// A GraphQL document is only valid if when it contains an anonymous operation
    /// (the query short-hand) that it contains only that one operation definition.
    /// </summary>
    public class LoneAnonymousOperation : IValidationRule
    {
        public static readonly LoneAnonymousOperation Instance = new LoneAnonymousOperation();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var operationCount = context.Document.Operations.Count;

            return new MatchingNodeVisitor<Operation>(op =>
                {
                    if (string.IsNullOrWhiteSpace(op.Name)
                        && operationCount > 1)
                    {
                        context.ReportError(new LoneAnonymousOperationError(context, op));
                    }
                }).ToTask();
        }
    }
}
