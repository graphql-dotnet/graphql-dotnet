using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique input field names:
    ///
    /// A GraphQL input object value is only valid if all supplied fields are
    /// uniquely named.
    /// </summary>
    public class UniqueInputFieldNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueInputFieldNames Instance = new UniqueInputFieldNames();

        /// <inheritdoc/>
        /// <exception cref="UniqueInputFieldNamesError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
                new MatchingNodeVisitor<GraphQLObjectValue>(
                    enter: (objVal, context) =>
                    {
                        var knownNameStack = context.TypeInfo.UniqueInputFieldNames_KnownNameStack ??= new Stack<Dictionary<ROM, GraphQLValue>>();

                        knownNameStack.Push(context.TypeInfo.UniqueInputFieldNames_KnownNames!);
                        context.TypeInfo.UniqueInputFieldNames_KnownNames = null;
                    },
                    leave: (objVal, context) => context.TypeInfo.UniqueInputFieldNames_KnownNames = context.TypeInfo.UniqueInputFieldNames_KnownNameStack!.Pop()),

                new MatchingNodeVisitor<GraphQLObjectField>(
                    leave: (objField, context) =>
                    {
                        var knownNames = context.TypeInfo.UniqueInputFieldNames_KnownNames ??= new Dictionary<ROM, GraphQLValue>();

                        if (knownNames.TryGetValue(objField.Name, out var value))
                        {
                            context.ReportError(new UniqueInputFieldNamesError(context, value, objField));
                        }
                        else
                        {
                            knownNames[objField.Name] = objField.Value;
                        }
                    })
            );
    }
}
