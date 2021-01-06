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

        /// <inheritdoc/>
        /// <exception cref="UniqueOperationNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => context.Document.Operations.Count < 2 ? null : _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<Operation>((op, context) =>
        {
            if (string.IsNullOrWhiteSpace(op.Name))
            {
                return;
            }

            var frequency = context.TypeInfo.UniqueOperationNames_Frequency ??= new HashSet<string>();

            if (!frequency.Add(op.Name))
            {
                context.ReportError(new UniqueOperationNamesError(context, op));
            }
        }).ToTask();
    }
}
