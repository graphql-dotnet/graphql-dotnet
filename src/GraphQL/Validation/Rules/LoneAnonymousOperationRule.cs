using GraphQL.Language;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Lone anonymous operation
    /// 
    /// A GraphQL document is only valid if when it contains an anonymous operation
    /// (the query short-hand) that it contains only that one operation definition.
    /// </summary>
    public class LoneAnonymousOperationRule : IValidationRule
    {
        public INodeVisitor Validate(ValidationContext context)
        {
            var operationCount = context.Document.Operations.Count;

            return new NodeVisitorMatchFuncListener<Operation>(
                n => n is Operation,
                op =>
                {
                    if (string.IsNullOrWhiteSpace(op.Name)
                        && operationCount > 1)
                    {
                        context.ReportError(
                            new ValidationError(
                                "5.1.2.1",
                                "This anonymous operation must be the only defined operation.",
                                op
                                ));
                    }
                });
        }
    }
}
