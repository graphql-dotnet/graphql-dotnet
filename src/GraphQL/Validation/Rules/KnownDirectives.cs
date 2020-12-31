using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

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
        public static readonly KnownDirectives Instance = new KnownDirectives();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Directive>(node =>
                {
                    var directiveDef = context.Schema.FindDirective(node.Name);
                    if (directiveDef == null)
                    {
                        context.ReportError(new KnownDirectivesError(context, node));
                        return;
                    }

                    var candidateLocation = getDirectiveLocationForAstPath(context.TypeInfo.GetAncestors(), context);
                    if (!directiveDef.Locations.Any(x => x == candidateLocation))
                    {
                        context.ReportError(new KnownDirectivesError(context, node, candidateLocation));
                    }
                });
            }).ToTask();
        }

        private DirectiveLocation getDirectiveLocationForAstPath(INode[] ancestors, ValidationContext context)
        {
            var appliedTo = ancestors[ancestors.Length - 1];

            if (appliedTo is Directives || appliedTo is Arguments)
            {
                appliedTo = ancestors[ancestors.Length - 2];
            }

            if (appliedTo is Operation op)
            {
                switch (op.OperationType)
                {
                    case OperationType.Query:
                        return DirectiveLocation.Query;
                    case OperationType.Mutation:
                        return DirectiveLocation.Mutation;
                    case OperationType.Subscription:
                        return DirectiveLocation.Subscription;
                }
            }
            if (appliedTo is Field)
                return DirectiveLocation.Field;
            if (appliedTo is FragmentSpread)
                return DirectiveLocation.FragmentSpread;
            if (appliedTo is InlineFragment)
                return DirectiveLocation.InlineFragment;
            if (appliedTo is FragmentDefinition)
                return DirectiveLocation.FragmentDefinition;

            throw new InvalidOperationException($"Unable to determine directive location for \"{context.Print(appliedTo)}\".");
        }
    }
}
