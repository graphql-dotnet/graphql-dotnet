using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No undefined variables:
    ///
    /// A GraphQL operation is only valid if all variables encountered, both directly
    /// and via fragment spreads, are defined by that operation.
    /// </summary>
    public class NoUndefinedVariables : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoUndefinedVariables Instance = new();

        /// <inheritdoc/>
        /// <exception cref="NoUndefinedVariablesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLVariableDefinition>((varDef, context) =>
            {
                var varNameDef = context.TypeInfo.NoUndefinedVariables_VariableNameDefined ??= new();
                varNameDef.Add(varDef.Variable.Name);
            }),

            new MatchingNodeVisitor<GraphQLOperationDefinition>(
                enter: (op, context) => context.TypeInfo.NoUndefinedVariables_VariableNameDefined?.Clear(),
                leave: (op, context) =>
                {
                    var varNameDef = context.TypeInfo.NoUndefinedVariables_VariableNameDefined;
                    var usages = context.GetRecursiveVariables(op);
                    if (usages != null)
                    {
                        foreach (var usage in usages)
                        {
                            var varName = usage.Node.Name;
                            if (varNameDef == null || !varNameDef.Contains(varName))
                            {
                                context.ReportError(new NoUndefinedVariablesError(context, op, usage.Node));
                            }
                        }
                    }
                })
        );
    }
}
