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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var knownNameStack = new Stack<Dictionary<string, IValue>>();
            var knownNames = new Dictionary<string, IValue>();

            return new NodeVisitors(
                new MatchingNodeVisitor<ObjectValue>(
                    enter: (objVal, context) =>
                    {
                        knownNameStack.Push(knownNames);
                        knownNames = new Dictionary<string, IValue>();
                    },
                    leave: (objVal, context) => knownNames = knownNameStack.Pop()),

                new MatchingNodeVisitor<ObjectField>(
                    leave: (objField, context) =>
                    {
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
}
