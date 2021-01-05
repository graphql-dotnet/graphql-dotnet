using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique operation names
    ///
    /// A GraphQL document is only valid if all defined operations have unique names.
    /// </summary>
    public class UniqueOperationNames : IValidationRule
    {
        public static readonly UniqueOperationNames Instance = new UniqueOperationNames();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var frequency = new HashSet<string>();

            return new MatchingNodeVisitor<Operation>(op =>
                    {
                        if (context.Document.Operations.Count < 2)
                        {
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(op.Name))
                        {
                            return;
                        }

                        if (!frequency.Add(op.Name))
                        {
                            context.ReportError(new UniqueOperationNamesError(context, op));
                        }
                }).ToTask();
        }
    }
}
