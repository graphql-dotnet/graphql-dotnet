using System;
using GraphQL.Language;
using GraphQL.Types;
using System.Linq;

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
            return new NodeVisitorMatchFuncListener<Field>(
                n => n is Field,
                f => Field(context.TypeInfo.GetLastType(), f, context));
        }

        private void Field(GraphType type, Field field, ValidationContext context)
        {
            if (IsLeafType(type))
            {
                if (field.SelectionSet != null && field.SelectionSet.Selections.Any())
                {
                    context.ReportError(new ValidationError("", NoSubselectionAllowedMessage(field.Name, type.Name)));
                }
            }
            else if(field.SelectionSet == null || !field.SelectionSet.Selections.Any())
            {
                context.ReportError(new ValidationError("", RequiredSubselectionMessage(field.Name, type?.Name)));
            }
        }

        private bool IsLeafType(GraphType type)
        {
            return type is ScalarGraphType || type is EnumerationGraphType;
        }
    }
}
