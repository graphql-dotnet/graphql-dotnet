using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

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
        public static readonly UniqueDirectivesPerLocation Instance = new();

        /// <inheritdoc/>
        /// <exception cref="UniqueDirectivesPerLocationError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLOperationDefinition>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<GraphQLField>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<GraphQLFragmentDefinition>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<GraphQLFragmentSpread>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<GraphQLInlineFragment>((f, context) => CheckDuplicates(context, f.Directives)),

            new MatchingNodeVisitor<GraphQLVariableDefinition>((f, context) => CheckDuplicates(context, f.Directives))
        );

        private static void CheckDuplicates(ValidationContext context, GraphQLDirectives? directives)
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

        private static int GetCount(GraphQLDirectives directives, ROM name)
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
