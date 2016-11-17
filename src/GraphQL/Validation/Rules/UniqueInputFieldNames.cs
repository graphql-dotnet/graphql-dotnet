using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

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
        public Func<string, string> DuplicateInputField =
            fieldName => $"There can be only one input field named {fieldName}.";

        public INodeVisitor Validate(ValidationContext context)
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
                    leave: objVal =>
                    {
                        knownNames = knownNameStack.Pop();
                    });

                _.Match<ObjectField>(
                    leave: objField =>
                    {
                        if (knownNames.ContainsKey(objField.Name))
                        {
                            context.ReportError(new ValidationError(
                                context.OriginalQuery,
                                "5.5.1",
                                DuplicateInputField(objField.Name),
                                knownNames[objField.Name],
                                objField.Value));
                        }
                        else
                        {
                            knownNames[objField.Name] = objField.Value;
                        }
                    });
            });
        }
    }
}
