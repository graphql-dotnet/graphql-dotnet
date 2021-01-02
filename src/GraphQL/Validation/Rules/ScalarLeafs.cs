using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Scalar leafs
    ///
    /// A GraphQL document is valid only if all leaf fields (fields without
    /// sub selections) are of scalar or enum types.
    /// </summary>
    public class ScalarLeafs : IValidationRule
    {
        public static readonly ScalarLeafs Instance = new ScalarLeafs();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ => _.Match<Field>(f => Field(context.TypeInfo.GetLastType(), f, context)))
                .ToTask();
        }

        private void Field(IGraphType type, Field field, ValidationContext context)
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
