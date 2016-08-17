using GraphQL.Language.AST;

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
                    if (!context.Schema.Directives.Any(x => x.Name == node.Name))
                    {
                        context.ReportError(new ValidationError(context.OriginalQuery, "5.6.1", UnknownDirectiveMessage(node.Name), node));
                    }
                });
            });
        }
    }
}
