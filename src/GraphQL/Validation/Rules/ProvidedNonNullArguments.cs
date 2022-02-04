using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Provided required arguments:
    ///
    /// A field or directive is only valid if all required (non-null) field arguments
    /// have been provided.
    /// </summary>
    public class ProvidedNonNullArguments : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly ProvidedNonNullArguments Instance = new();

        /// <inheritdoc/>
        /// <exception cref="ProvidedNonNullArgumentsError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLField>(leave: (node, context) =>
            {
                var fieldDef = context.TypeInfo.GetFieldDef();

                if (fieldDef?.Arguments?.Count > 0)
                {
                    foreach (var arg in fieldDef.Arguments.List!)
                    {
                        if (arg.DefaultValue == null &&
                            arg.ResolvedType is NonNullGraphType &&
                            node.Arguments?.ValueFor(arg.Name) == null)
                        {
                            context.ReportError(new ProvidedNonNullArgumentsError(context, node, arg));
                        }
                    }
                }
            }),

            new MatchingNodeVisitor<GraphQLDirective>(leave: (node, context) =>
            {
                var directive = context.TypeInfo.GetDirective();

                if (directive?.Arguments?.Count > 0)
                {
                    foreach (var arg in directive.Arguments.List!)
                    {
                        var argAst = node.Arguments?.ValueFor(arg.Name);
                        var type = arg.ResolvedType;

                        if (argAst == null && type is NonNullGraphType)
                        {
                            context.ReportError(new ProvidedNonNullArgumentsError(context, node, arg));
                        }
                    }
                }
            })
        );
    }
}
