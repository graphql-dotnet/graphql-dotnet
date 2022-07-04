using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

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
        public static readonly UniqueOperationNames Instance = new();

        /// <inheritdoc/>
        /// <exception cref="UniqueOperationNamesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(context.Document.OperationsCount() < 2 ? null : _nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLOperationDefinition>((op, context) =>
        {
            if (op.Name is null)
            {
                return;
            }

            var frequency = context.TypeInfo.UniqueOperationNames_Frequency ??= new();

            if (!frequency.Add(op.Name))
            {
                context.ReportError(new UniqueOperationNamesError(context, op));
            }
        });
    }
}
