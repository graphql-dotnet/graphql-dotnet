#nullable enable

using System;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known directives:
    ///
    /// GraphQL servers define what directives they support and where they support them.
    /// For each usage of a directive, the directive must be available on that server and
    /// must be used in a location that the server has declared support for.
    /// </summary>
    public class KnownDirectivesInAllowedLocations : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly KnownDirectivesInAllowedLocations Instance = new KnownDirectivesInAllowedLocations();

        /// <inheritdoc/>
        /// <exception cref="KnownDirectivesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<Directive>((node, context) =>
        {
            var directiveDef = context.Schema.Directives.Find(node.Name);
            if (directiveDef == null)
            {
                context.ReportError(new KnownDirectivesError(context, node));
            }
            else
            {
                var candidateLocation = DirectiveLocationForAstPath(context);
                if (!directiveDef.Locations.Contains(candidateLocation))
                {
                    context.ReportError(new DirectivesInAllowedLocationsError(context, node, candidateLocation));
                }
            }
        }).ToTask();

        private static DirectiveLocation DirectiveLocationForAstPath(ValidationContext context)
        {
            var appliedTo = context.TypeInfo.GetAncestor(1);

            if (appliedTo is Directives || appliedTo is Arguments)
            {
                appliedTo = context.TypeInfo.GetAncestor(2);
            }

            return appliedTo switch
            {
                Operation op => op.OperationType switch
                {
                    OperationType.Query => DirectiveLocation.Query,
                    OperationType.Mutation => DirectiveLocation.Mutation,
                    OperationType.Subscription => DirectiveLocation.Subscription,
                    _ => throw new InvalidOperationException($"Unknown operation type '{op.OperationType}.")
                },
                Field _ => DirectiveLocation.Field,
                FragmentSpread _ => DirectiveLocation.FragmentSpread,
                InlineFragment _ => DirectiveLocation.InlineFragment,
                FragmentDefinition _ => DirectiveLocation.FragmentDefinition,
                _ => throw new InvalidOperationException($"Unable to determine directive location for '{appliedTo?.StringFrom(context.Document)}'.")
            };
        }
    }
}
