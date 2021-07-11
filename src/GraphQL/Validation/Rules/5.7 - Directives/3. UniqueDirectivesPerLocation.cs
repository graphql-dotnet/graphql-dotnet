#nullable enable

using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique directive names per location:
    ///
    /// A GraphQL document is only valid if all not repeatable directives
    /// at a given location are uniquely named.
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
            new MatchingNodeVisitor<Operation>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<Field>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<FragmentDefinition>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<FragmentSpread>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<InlineFragment>((f, context) => CheckDuplicates(context, f.Directives))
        ).ToTask();

        private static void CheckDuplicates(ValidationContext context, Directives? directives)
        {
            if (directives?.Count > 0)
            {
                foreach (var directive in directives)
                {
                    var directiveDef = context.Schema.Directives.Find(directive.Name);
                    if (directiveDef != null && !directiveDef.Repeatable && GetCount(directives, directive.Name) > 1)
                    {
                        context.ReportError(new UniqueDirectivesPerLocationError(context, directive));
                    }
                }
            }
        }

        private static int GetCount(Directives directives, string name)
        {
            int count = 0;

            foreach (var directive in directives)
            {
                if (directive.Name == name)
                    ++count;
            }

            return count;
        }
    }
}
