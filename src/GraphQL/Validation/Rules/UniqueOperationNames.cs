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

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Document>((_, context) =>
                {
                    if (context.Document.Operations.Count >= 2)
                        context.Set<UniqueOperationNames>(new HashSet<string>());
                });
                _.Match<Operation>((op, context) =>
                {
                    if (context.Document.Operations.Count >= 2 && !string.IsNullOrWhiteSpace(op.Name))
                    {
                        var frequency = context.Get<UniqueOperationNames, HashSet<string>>();
                        if (!frequency.Add(op.Name))
                        {
                            context.ReportError(new UniqueOperationNamesError(context, op));
                        }
                    }
                });
            }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
