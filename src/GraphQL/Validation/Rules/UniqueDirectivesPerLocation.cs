using System.Collections.Generic;
using GraphQL.Language.AST;

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
        public string DuplicateDirectiveMessage(string directiveName)
        {
            return $"The directive \"{directiveName}\" can only be used once at this location.";
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(f =>
                {
                    CheckDirectives(context, f.Directives);
                });

                _.Match<Field>(f =>
                {
                    CheckDirectives(context, f.Directives);
                });

                _.Match<FragmentDefinition>(f =>
                {
                    CheckDirectives(context, f.Directives);
                });

                _.Match<FragmentSpread>(f =>
                {
                    CheckDirectives(context, f.Directives);
                });

                _.Match<InlineFragment>(f =>
                {
                    CheckDirectives(context, f.Directives);
                });
            });
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
                    var error = new ValidationError(
                        context.OriginalQuery,
                        "5.6.3",
                        DuplicateDirectiveMessage(directive.Name),
                        knownDirectives[directive.Name],
                        directive);

                    context.ReportError(error);
                }
                else
                {
                    knownDirectives[directive.Name] = directive;
                }
            }
        }
    }
}
