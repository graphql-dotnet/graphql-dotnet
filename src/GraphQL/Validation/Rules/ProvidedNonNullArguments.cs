using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Provided required arguments
    ///
    /// A field or directive is only valid if all required (non-null) field arguments
    /// have been provided.
    /// </summary>
    public class ProvidedNonNullArguments : IValidationRule
    {
        public string MissingFieldArgMessage(string fieldName, string argName, string type)
        {
            return $"Field \"{fieldName}\" argument \"{argName}\" of type \"{type}\" is required but not provided.";
        }

        public string MissingDirectiveArgMessage(string directiveName, string argName, string type)
        {
            return $"Directive \"{directiveName}\" argument \"{argName}\" of type \"{type}\" is required but not provided.";
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Field>(leave: node =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();

                    if (fieldDef == null)
                    {
                        return;
                    }

                    fieldDef.Arguments?.Apply(arg =>
                    {
                        var argAst = node.Arguments?.ValueFor(arg.Name);
                        var type = arg.ResolvedType;

                        if (argAst == null && type is NonNullGraphType)
                        {
                            context.ReportError(
                                new ValidationError(
                                    context.OriginalQuery,
                                    "5.3.3.2",
                                    MissingFieldArgMessage(node.Name, arg.Name, context.Print(type)),
                                    node));
                        }
                    });
                });

                _.Match<Directive>(leave: node =>
                {
                    var directive = context.TypeInfo.GetDirective();

                    if (directive == null)
                    {
                        return;
                    }

                    directive.Arguments?.Apply(arg =>
                    {
                        var argAst = node.Arguments?.ValueFor(arg.Name);
                        var type = arg.ResolvedType;

                        if (argAst == null && type is NonNullGraphType)
                        {
                            context.ReportError(
                                new ValidationError(
                                    context.OriginalQuery,
                                    "5.3.3.2",
                                    MissingDirectiveArgMessage(node.Name, arg.Name, context.Print(type)),
                                    node));
                        }
                    });
                });
            });
        }
    }
}
