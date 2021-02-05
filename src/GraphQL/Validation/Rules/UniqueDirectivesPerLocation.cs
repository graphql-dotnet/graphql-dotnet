using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique directive names per location:
    ///
    /// A GraphQL document is only valid if all directives at a given location
    /// are uniquely named.
    /// </summary>
    public class UniqueDirectivesPerLocation : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueDirectivesPerLocation Instance = new UniqueDirectivesPerLocation();

        /// <inheritdoc/>
        /// <exception cref="UniqueDirectivesPerLocationError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<Operation>((f, context) => CheckDirectives(context, f.Directives)),

            new MatchingNodeVisitor<Field>((f, context) => CheckDirectives(context, f.Directives)),

            new MatchingNodeVisitor<FragmentDefinition>((f, context) => CheckDirectives(context, f.Directives)),

            new MatchingNodeVisitor<FragmentSpread>((f, context) => CheckDirectives(context, f.Directives)),

            new MatchingNodeVisitor<InlineFragment>((f, context) => CheckDirectives(context, f.Directives))
        ).ToTask();

        private static void CheckDirectives(ValidationContext context, Directives directives)
        {
            if (directives == null || directives.Count == 0)
                return;

            if (!directives.HasDuplicates)
                return;

            var knownDirectives = new Dictionary<string, Directive>(directives.Count);

            foreach (var directive in directives)
            {
                if (knownDirectives.ContainsKey(directive.Name))
                {
                    context.ReportError(new UniqueDirectivesPerLocationError(context, knownDirectives[directive.Name], directive));
                }
                else
                {
                    knownDirectives[directive.Name] = directive;
                }
            }
        }
    }
}
