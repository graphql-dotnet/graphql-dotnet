using GraphQL.Language.AST;
using GraphQL.Types;
using System.Threading.Tasks;

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
            return $"Argument \"{argName}\" of type \"{type}\" is required for field \"{fieldName}\" but not provided.";
        }

        public string MissingDirectiveArgMessage(string directiveName, string argName, string type)
        {
            return $"Argument \"{argName}\" of type \"{type}\" is required for directive \"{directiveName}\" but not provided.";
        }

        public static readonly ProvidedNonNullArguments Instance = new ProvidedNonNullArguments();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Field>(leave: node =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();

                    if (fieldDef == null || fieldDef.Arguments == null)
                    {
                        return;
                    }

                    foreach (var arg in fieldDef.Arguments)
                    {
                        if (arg.DefaultValue == null &&
                            arg.ResolvedType is NonNullGraphType &&
                            node.Arguments?.ValueFor(arg.Name) == null)
                        {
                            context.ReportError(
                                new ValidationError(
                                    context.OriginalQuery,
                                    "5.3.3.2",
                                    MissingFieldArgMessage(node.Name, arg.Name, context.Print(arg.ResolvedType)),
                                    node));
                        }
                    }
                });

                _.Match<Directive>(leave: node =>
                {
                    var directive = context.TypeInfo.GetDirective();

                    if (directive?.Arguments?.ArgumentsList == null)
                    {
                        return;
                    }

                    foreach (var arg in directive.Arguments.ArgumentsList)
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
                    }
                });
            }).ToTask();
        }
    }
}
