using System;
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
        [Obsolete]
        public Func<string> AnonOperationNotAloneMessage => () =>
            "This anonymous operation must be the only defined operation.";

        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly LoneAnonymousOperation Instance = new LoneAnonymousOperation();

        /// <inheritdoc/>
        /// <exception cref="LoneAnonymousOperationError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var operationCount = context.Document.Operations.Count;

            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(op =>
                {
                    if (string.IsNullOrWhiteSpace(op.Name)
                        && operationCount > 1)
                    {
                        context.ReportError(new LoneAnonymousOperationError(context, op));
                    }
                });
            }).ToTask();
        }
    }
}
