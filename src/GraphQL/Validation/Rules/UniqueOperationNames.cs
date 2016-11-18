using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique operation names
    ///
    /// A GraphQL document is only valid if all defined operations have unique names.
    /// </summary>
    public class UniqueOperationNames : IValidationRule
    {
        public Func<string, string> DuplicateOperationNameMessage => opName =>
            $"There can only be one operation named {opName}.";

        public INodeVisitor Validate(ValidationContext context)
        {
            var frequency = new Dictionary<string, string>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(
                    enter: op =>
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
                            var error = new ValidationError(
                                context.OriginalQuery,
                                "5.1.1.1",
                                DuplicateOperationNameMessage(op.Name),
                                op);
                            context.ReportError(error);
                        }
                        else
                        {
                            frequency[op.Name] = op.Name;
                        }
                    });
            });
        }
    }
}
