using System.Collections.Generic;
using GraphQL.Language;
using System.Threading;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ResolveFieldContext
    {
        public string FieldName { get; set; }

        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public GraphType ReturnType { get; set; }

        public ObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object RootValue { get; set; }

        public object Source { get; set; }

        public ISchema Schema { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public TType Argument<TType>(string name)
        {
            if (!Arguments.ContainsKey(name))
            {
                var error = new ExecutionError($"An argument with name \"{name}\" does not exist.");
                if (FieldAst?.SourceLocation != null)
                {
                    error.AddLocation(FieldAst.SourceLocation.Line, FieldAst.SourceLocation.Column);
                }
                throw error;
            }

            var arg = Arguments[name];
            var inputObject = arg as Dictionary<string, object>;
            if (inputObject != null)
            {
                var type = typeof(TType);
                if (type.Namespace?.StartsWith("System") == true)
                {
                    return (TType)arg;
                }

                return (TType)inputObject.ToObject(type);
            }

            return arg.GetPropertyValue<TType>();
        }
    }
}
