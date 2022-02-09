using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Scalar leafs:
    ///
    /// A GraphQL document is valid only if all leaf fields (fields without
    /// sub selections) are of scalar or enum types.
    /// </summary>
    public class ScalarLeafs : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly ScalarLeafs Instance = new();

        /// <inheritdoc/>
        /// <exception cref="ScalarLeafsError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor =
            new MatchingNodeVisitor<GraphQLField>((f, context) => Field(context.TypeInfo.GetLastType(), f, context));

        private static void Field(IGraphType? type, GraphQLField field, ValidationContext context)
        {
            if (type == null)
            {
                return;
            }

            if (type.IsLeafType())
            {
                if (field.SelectionSet != null && field.SelectionSet.Selections.Count > 0)
                {
                    context.ReportError(new ScalarLeafsError(context, field.SelectionSet, field, type));
                }
            }
            else if (field.SelectionSet == null || field.SelectionSet.Selections.Count == 0)
            {
                context.ReportError(new ScalarLeafsError(context, field, type));
            }
        }
    }
}
