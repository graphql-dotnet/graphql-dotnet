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
        private sealed class Names
        {
            public Stack<Dictionary<string, IValue>> KnownNameStack { get; set; } = new Stack<Dictionary<string, IValue>>();

            public Dictionary<string, IValue> KnownNames { get; set; } = new Dictionary<string, IValue>();
        }

        public static readonly UniqueInputFieldNames Instance = new UniqueInputFieldNames();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Document>((_, context) => context.Set<UniqueInputFieldNames>(new Names()));
                _.Match<ObjectValue>(
                    enter: (objVal, context) =>
                    {
                        var data = context.Get<UniqueInputFieldNames, Names>();
                        data.KnownNameStack.Push(data.KnownNames);
                        data.KnownNames = new Dictionary<string, IValue>();
                    },
                    leave: (objVal, context) =>
                    {
                        var data = context.Get<UniqueInputFieldNames, Names>();
                        data.KnownNames = data.KnownNameStack.Pop();
                    });

                _.Match<ObjectField>(
                    leave: (objField, context) =>
                    {
                        var data = context.Get<UniqueInputFieldNames, Names>();
                        if (data.KnownNames.ContainsKey(objField.Name))
                        {
                            context.ReportError(new UniqueInputFieldNamesError(context, data.KnownNames[objField.Name], objField));
                        }
                        else
                        {
                            data.KnownNames[objField.Name] = objField.Value;
                        }
                    });
            }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
