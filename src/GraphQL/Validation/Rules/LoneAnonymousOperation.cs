using System;
using GraphQL.Language.AST;

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
        public Func<string> AnonOperationNotAloneMessage => () =>
            "This anonymous operation must be the only defined operation.";

        public INodeVisitor Validate(ValidationContext context)
        {
            var operationCount = context.Document.Operations.Count;

            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(op =>
                {
                    if (string.IsNullOrWhiteSpace(op.Name)
                        && operationCount > 1)
                    {
                        var error = new ValidationError(
                            context.OriginalQuery,
                            "5.1.2.1",
                            AnonOperationNotAloneMessage(),
                            op);
                        context.ReportError(error);
                    }
                });
            });
        }
    }
}
