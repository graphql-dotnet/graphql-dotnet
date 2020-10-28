using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique directive names per location
    ///
    /// A GraphQL document is only valid if all directives at a given location
    /// are uniquely named.
    /// </summary>
    public class UniqueDirectivesPerLocation : IValidationRule
    {
        public static readonly UniqueDirectivesPerLocation Instance = new UniqueDirectivesPerLocation();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(f => CheckDirectives(context, f.Directives));

                _.Match<Field>(f => CheckDirectives(context, f.Directives));

                _.Match<FragmentDefinition>(f => CheckDirectives(context, f.Directives));

                _.Match<FragmentSpread>(f => CheckDirectives(context, f.Directives));

                _.Match<InlineFragment>(f => CheckDirectives(context, f.Directives));
            }).ToTask();
        }

        private void CheckDirectives(ValidationContext context, Directives directives)
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
