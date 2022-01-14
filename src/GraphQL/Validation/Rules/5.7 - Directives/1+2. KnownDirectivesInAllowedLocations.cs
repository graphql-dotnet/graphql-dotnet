using System;
using System.Threading.Tasks;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

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
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLDirective>((node, context) =>
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
        });

        private static DirectiveLocation DirectiveLocationForAstPath(ValidationContext context)
        {
            var appliedTo = context.TypeInfo.GetAncestor(1);

            if (appliedTo is GraphQLDirectives || appliedTo is GraphQLArguments)
            {
                appliedTo = context.TypeInfo.GetAncestor(2);
            }

            return appliedTo switch
            {
                GraphQLOperationDefinition op => op.Operation switch
                {
                    OperationType.Query => DirectiveLocation.Query,
                    OperationType.Mutation => DirectiveLocation.Mutation,
                    OperationType.Subscription => DirectiveLocation.Subscription,
                    _ => throw new InvalidOperationException($"Unknown operation type '{op.Operation}.")
                },
                GraphQLField _ => DirectiveLocation.Field,
                GraphQLFragmentSpread _ => DirectiveLocation.FragmentSpread,
                GraphQLInlineFragment _ => DirectiveLocation.InlineFragment,
                GraphQLFragmentDefinition _ => DirectiveLocation.FragmentDefinition,
                _ => throw new InvalidOperationException($"Unable to determine directive location for '{appliedTo?.StringFrom(context.OriginalQuery)}'.")
            };
        }
    }
}
