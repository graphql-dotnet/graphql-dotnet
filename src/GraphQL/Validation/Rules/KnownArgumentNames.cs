using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known argument names
    ///
    /// A GraphQL field is only valid if all supplied arguments are defined by
    /// that field.
    /// </summary>
    public class KnownArgumentNames : IValidationRule
    {
        public string UnknownArgMessage(string argName, string fieldName, string type, string[] suggestedArgs)
        {
            var message = $"Unknown argument \"{argName}\" on field \"{fieldName}\" of type \"{type}\".";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $"Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }

        public string UnknownDirectiveArgMessage(string argName, string directiveName, string[] suggestedArgs)
        {
            var message = $"Unknown argument \"{argName}\" on directive \"{directiveName}\".";
            if (suggestedArgs != null && suggestedArgs.Length > 0)
            {
                message += $"Did you mean {StringUtils.QuotedOrList(suggestedArgs)}";
            }
            return message;
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Argument>(node =>
                {
                    var ancestors = context.TypeInfo.GetAncestors();
                    var argumentOf = ancestors[ancestors.Length - 2];
                    if (argumentOf is Field)
                    {
                        var fieldDef = context.TypeInfo.GetFieldDef();
                        if (fieldDef != null)
                        {
                            var fieldArgDef = fieldDef.Arguments?.Find(node.Name);
                            if (fieldArgDef == null)
                            {
                                var parentType = context.TypeInfo.GetParentType();
                                Invariant.Check(parentType != null, "Parent type must not be null.");
                                context.ReportError(new ValidationError(
                                    context.OriginalQuery,
                                    "5.3.1",
                                    UnknownArgMessage(
                                        node.Name,
                                        fieldDef.Name,
                                        context.Print(parentType),
                                        StringUtils.SuggestionList(node.Name, fieldDef.Arguments?.Select(q => q.Name))),
                                    node));
                            }
                        }
                    } else if (argumentOf is Directive)
                    {
                        var directive = context.TypeInfo.GetDirective();
                        if (directive != null)
                        {
                            var directiveArgDef = directive.Arguments?.Find(node.Name);
                            if (directiveArgDef == null)
                            {
                                context.ReportError(new ValidationError(
                                    context.OriginalQuery,
                                    "5.3.1",
                                    UnknownDirectiveArgMessage(
                                        node.Name,
                                        directive.Name,
                                        StringUtils.SuggestionList(node.Name, directive.Arguments?.Select(q => q.Name))),
                                    node));
                            }
                        }
                    }
                });
            });
        }
    }
}
