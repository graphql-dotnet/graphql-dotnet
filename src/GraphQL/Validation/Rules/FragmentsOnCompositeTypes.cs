using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public static readonly FragmentsOnCompositeTypes Instance = new FragmentsOnCompositeTypes();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<InlineFragment>((node, context) =>
                {
                    var type = context.TypeInfo.GetLastType();
                    if (node.Type != null && type != null && !type.IsCompositeType())
                    {
                        context.ReportError(new FragmentsOnCompositeTypesError(context, node));
                    }
                });

                _.Match<FragmentDefinition>((node, context) =>
                {
                    var type = context.TypeInfo.GetLastType();
                    if (type != null && !type.IsCompositeType())
                    {
                        context.ReportError(new FragmentsOnCompositeTypesError(context, node));
                    }
                });
            }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
