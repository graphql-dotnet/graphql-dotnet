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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            if (context.Document.Operations.Count < 2)
                return _nullNodeVisitor;

            var frequency = new HashSet<string>();

            return new MatchingNodeVisitor<Operation>((op, context) =>
                {
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

        private static readonly Task<INodeVisitor> _nullNodeVisitor = Task.FromResult((INodeVisitor)NullNodeVisitor.Instance);

        private class NullNodeVisitor : INodeVisitor
        {
            private NullNodeVisitor() { }
            public static readonly NullNodeVisitor Instance = new NullNodeVisitor();
            void INodeVisitor.Enter(INode node, ValidationContext context) { }
            void INodeVisitor.Leave(INode node, ValidationContext context) { }
        }
    }
}
