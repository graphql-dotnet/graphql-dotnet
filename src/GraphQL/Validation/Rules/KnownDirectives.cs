using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known directives
    ///
    /// A GraphQL document is only valid if all `@directives` are known by the
    /// schema and legally positioned.
    /// </summary>
    public class KnownDirectives : IValidationRule
    {
        public string UnknownDirectiveMessage(string directiveName)
        {
            return $"Unknown directive \"{directiveName}\".";
        }

        public string MisplacedDirectiveMessage(string directiveName, string location)
        {
            return $"Directive \"{directiveName}\" may not be used on {location}.";
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Directive>(node =>
                {
                    var directiveDef = context.Schema.Directives.SingleOrDefault(x => x.Name == node.Name);
                    if (directiveDef == null)
                    {
                        context.ReportError(new ValidationError(context.OriginalQuery, "5.6.1", UnknownDirectiveMessage(node.Name), node));
                        return;
                    }

                    var canidateLocation = getDirectiveLocationForAstPath(context.TypeInfo.GetAncestors(), context);
                    if (!directiveDef.Locations.Any(x => x == canidateLocation))
                    {
                        context.ReportError(new ValidationError(
                            context.OriginalQuery,
                            "5.6.1",
                            MisplacedDirectiveMessage(node.Name, canidateLocation.ToString()),
                            node));
                    }
                });
            });
        }

        private DirectiveLocation getDirectiveLocationForAstPath(INode[] ancestors, ValidationContext context)
        {
            var appliedTo = ancestors[ancestors.Length - 1];

            if (appliedTo is Directives || appliedTo is Arguments)
            {
                appliedTo = ancestors[ancestors.Length - 2];
            }

            if (appliedTo is Operation)
            {
                var op = (Operation) appliedTo;
                switch (op.OperationType)
                {
                    case OperationType.Query: return DirectiveLocation.Query;
                    case OperationType.Mutation: return DirectiveLocation.Mutation;
                    case OperationType.Subscription: return DirectiveLocation.Subscription;
                }
            }
            if (appliedTo is Field) return DirectiveLocation.Field;
            if (appliedTo is FragmentSpread) return DirectiveLocation.FragmentSpread;
            if (appliedTo is InlineFragment) return DirectiveLocation.InlineFragment;
            if (appliedTo is FragmentDefinition) return DirectiveLocation.FragmentDefinition;

            throw new ExecutionError($"Unable to determine directive location for \"{context.Print(appliedTo)}\".");
        }
    }
}
