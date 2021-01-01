using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique input field names
    ///
    /// A GraphQL input object value is only valid if all supplied fields are
    /// uniquely named.
    /// </summary>
    public class UniqueInputFieldNames : IValidationRule
    {
        public static readonly UniqueInputFieldNames Instance = new UniqueInputFieldNames();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var knownNameStack = new Stack<Dictionary<string, IValue>>();
            var knownNames = new Dictionary<string, IValue>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<ObjectValue>(
                    enter: objVal =>
                    {
                        knownNameStack.Push(knownNames);
                        knownNames = new Dictionary<string, IValue>();
                    },
                    leave: objVal => knownNames = knownNameStack.Pop());

                _.Match<ObjectField>(
                    leave: objField =>
                    {
                        if (knownNames.ContainsKey(objField.Name))
                        {
                            context.ReportError(new UniqueInputFieldNamesError(context, knownNames[objField.Name], objField));
                        }
                        else
                        {
                            knownNames[objField.Name] = objField.Value;
                        }
                    });
            }).ToTask();
        }
    }
}
