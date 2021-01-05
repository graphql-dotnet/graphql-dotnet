using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique operation names:
    ///
    /// A GraphQL document is only valid if all defined operations have unique names.
    /// </summary>
    public class UniqueOperationNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueOperationNames Instance = new UniqueOperationNames();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Document>((_, context) => context.Set<UniqueOperationNames>(new HashSet<string>()));
                _.Match<Operation>((op, context) =>
                {
                    if (!string.IsNullOrWhiteSpace(op.Name))
                    {
                        var frequency = context.Get<UniqueOperationNames, HashSet<string>>();
                        if (!frequency.Add(op.Name))
                        {
                            context.ReportError(new UniqueOperationNamesError(context, op));
                        }
                    }
                });
            },
            shouldRun: context => context.Document.Operations.Count >= 2
            ).ToTask();

        /// <inheritdoc/>
        /// <exception cref="UniqueOperationNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
