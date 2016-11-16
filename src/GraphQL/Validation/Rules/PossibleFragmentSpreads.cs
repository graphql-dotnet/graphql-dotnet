using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Possible fragment spread
    ///
    /// A fragment spread is only valid if the type condition could ever possibly
    /// be true: if there is a non-empty intersection of the possible parent types,
    /// and possible types which pass the type condition.
    /// </summary>
    public class PossibleFragmentSpreads : IValidationRule
    {
        public string TypeIncompatibleSpreadMessage(string fragName, string parentType, string fragType)
        {
            return $"Fragment \"{fragName}\" cannot be spread here as objects of type \"{parentType}\" can never be of type \"{fragType}\".";
        }

        public string TypeIncompatibleAnonSpreadMessage(string parentType, string fragType)
        {
            return $"Fragment cannot be spread here as objects of type \"{parentType}\" can never be of type \"{fragType}\".";
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<InlineFragment>(node =>
                {
                    var fragType = context.TypeInfo.GetLastType();
                    var parentType = context.TypeInfo.GetParentType().GetNamedType();

                    if (fragType != null && parentType != null && !context.Schema.DoTypesOverlap(fragType, parentType))
                    {
                        context.ReportError(new ValidationError(
                            context.OriginalQuery,
                            "5.4.2.3",
                            TypeIncompatibleAnonSpreadMessage(context.Print(parentType), context.Print(fragType)),
                            node));
                    }
                });

                _.Match<FragmentSpread>(node =>
                {
                    var fragName = node.Name;
                    var fragType = getFragmentType(context, fragName);
                    var parentType = context.TypeInfo.GetParentType().GetNamedType();

                    if (fragType != null && parentType != null && !context.Schema.DoTypesOverlap(fragType, parentType))
                    {
                        context.ReportError(new ValidationError(
                            context.OriginalQuery,
                            "5.4.2.3",
                            TypeIncompatibleSpreadMessage(fragName, context.Print(parentType), context.Print(fragType)),
                            node));
                    }
                });
            });
        }

        private IGraphType getFragmentType(ValidationContext context, string name)
        {
            var frag = context.GetFragment(name);
            return frag.Type.GraphTypeFromType(context.Schema);
        }
    }
}
