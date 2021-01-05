using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

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
        public static readonly ProvidedNonNullArguments Instance = new ProvidedNonNullArguments();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Field>(leave: (node, context) =>
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
                            context.ReportError(new ProvidedNonNullArgumentsError(context, node, arg));
                        }
                    }
                });

                _.Match<Directive>(leave: (node, context) =>
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
                            context.ReportError(new ProvidedNonNullArgumentsError(context, node, arg));
                        }
                    }
                });
            }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
