using System;
using GraphQL.Types;
using System.Linq;
using GraphQL.Language.AST;

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
        public Func<string, string, string> NoSubselectionAllowedMessage = (field, type) =>
            $"Field {field} of type {type} must not have a sub selection";

        public Func<string, string, string> RequiredSubselectionMessage = (field, type) =>
            $"Field {field} of type {type} must have a sub selection";

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Field>(f => Field(context.TypeInfo.GetLastType(), f, context));
            });
        }

        private void Field(IGraphType type, Field field, ValidationContext context)
        {
            if (type == null)
            {
                return;
            }

            if (type.IsLeafType())
            {
                if (field.SelectionSet != null && field.SelectionSet.Selections.Any())
                {
                    var error = new ValidationError(context.OriginalQuery, "5.2.3", NoSubselectionAllowedMessage(field.Name, context.Print(type)), field.SelectionSet);
                    context.ReportError(error);
                }
            }
            else if(field.SelectionSet == null || !field.SelectionSet.Selections.Any())
            {
                var error = new ValidationError(context.OriginalQuery, "5.2.3", RequiredSubselectionMessage(field.Name, context.Print(type)), field);
                context.ReportError(error);
            }
        }
    }
}
