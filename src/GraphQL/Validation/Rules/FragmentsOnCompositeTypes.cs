using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Fragments on composite type
    ///
    /// Fragments use a type condition to determine if they apply, since fragments
    /// can only be spread into a composite type (object, interface, or union), the
    /// type condition must also be a composite type.
    /// </summary>
    public class FragmentsOnCompositeTypes : IValidationRule
    {
        public string InlineFragmentOnNonCompositeErrorMessage(string type)
        {
            return $"Fragment cannot condition on non composite type \"{type}\".";
        }

        public string FragmentOnNonCompositeErrorMessage(string fragName, string type)
        {
            return $"Fragment \"{fragName}\" cannot condition on non composite type \"{type}\".";
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<InlineFragment>(node =>
                {
                    var type = context.TypeInfo.GetLastType();
                    if (node.Type != null && type != null && !type.IsCompositeType())
                    {
                        context.ReportError(new ValidationError(
                            context.OriginalQuery,
                            "5.4.1.3",
                            InlineFragmentOnNonCompositeErrorMessage(context.Print(node.Type)),
                            node.Type));
                    }
                });

                _.Match<FragmentDefinition>(node =>
                {
                    var type = context.TypeInfo.GetLastType();
                    if (type != null && !type.IsCompositeType())
                    {
                        context.ReportError(new ValidationError(
                            context.OriginalQuery,
                            "5.4.1.3",
                            FragmentOnNonCompositeErrorMessage(node.Name, context.Print(node.Type)),
                            node.Type));
                    }
                });
            });
        }
    }
}
