using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
                new MatchingNodeVisitor<ObjectValue>(
                    enter: (objVal, context) =>
                    {
                        var knownNameStack = context.TypeInfo.UniqueInputFieldNames_KnownNameStack ??= new Stack<Dictionary<string, IValue>>();

                        knownNameStack.Push(context.TypeInfo.UniqueInputFieldNames_KnownNames);
                        context.TypeInfo.UniqueInputFieldNames_KnownNames = null;
                    },
                    leave: (objVal, context) => context.TypeInfo.UniqueInputFieldNames_KnownNames = context.TypeInfo.UniqueInputFieldNames_KnownNameStack.Pop()),

                new MatchingNodeVisitor<ObjectField>(
                    leave: (objField, context) =>
                    {
                        var knownNames = context.TypeInfo.UniqueInputFieldNames_KnownNames ??= new Dictionary<string, IValue>();

                        if (knownNames.ContainsKey(objField.Name))
                        {
                            context.ReportError(new UniqueInputFieldNamesError(context, knownNames[objField.Name], objField));
                        }
                        else
                        {
                            knownNames[objField.Name] = objField.Value;
                        }
                    })
            ).ToTask();
    }
}
