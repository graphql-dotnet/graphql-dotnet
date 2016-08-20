using System;
using GraphQL.Language;

namespace GraphQL.Validation.Rules
{
    public class KnownDirectives : IValidationRule
    {
        public static string UnknownDirectiveMessage(string directiveName)
        {
            return $"Unknown directive \"{directiveName}\".";
        }

        public static string MisplacedDirectiveMessage(string directiveName, string location)
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
                        context.ReportError(new ValidationError("5.6.1", UnknownDirectiveMessage(node.Name), node));
                    }
                });
            });
        }
    }
}
