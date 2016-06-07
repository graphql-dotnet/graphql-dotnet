using System.Collections.Generic;
using GraphQL.Language;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique operation names
    /// 
    /// A GraphQL document is only valid if all defined operations have unique names.
    /// </summary>
    public class UniqueOperationNames : IValidationRule
    {
        public INodeVisitor Validate(ValidationContext context)
        {
            var frequency = new Dictionary<string, string>();

            return new NodeVisitorMatchFuncListener<Operation>(
                n => n is Operation,
                op =>
                {
                    if (context.Document.Operations.Count < 2)
                    {
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(op.Name))
                    {
                        return;
                    }

                    if (frequency.ContainsKey(op.Name))
                    {
                        context.ReportError(
                            new ValidationError(
                                "5.1.1.1",
                                $"There can only be one operation named {op.Name}.",
                                op));
                    }
                    else
                    {
                        frequency[op.Name] = op.Name;
                    }
                });
        }
    }
}
