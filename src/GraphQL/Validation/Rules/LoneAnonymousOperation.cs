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

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Document>((_, context) => context.Set<LoneAnonymousOperation>(context.Document.Operations.Count));
                _.Match<Operation>((op, context) =>
                {
                    int operationCount = context.Get<LoneAnonymousOperation, int>();
                    if (string.IsNullOrWhiteSpace(op.Name) && operationCount > 1)
                    {
                        context.ReportError(new LoneAnonymousOperationError(context, op));
                    }
                });
            }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
