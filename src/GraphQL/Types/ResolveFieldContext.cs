using System.Collections.Generic;
using GraphQL.Language;
using System.Threading;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ResolveFieldContext<TSource>
    {
        public string FieldName { get; set; }

        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public GraphType ReturnType { get; set; }

        public ObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object RootValue { get; set; }

        public TSource Source { get; set; }

        public ISchema Schema { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public ResolveFieldContext() { }

        public ResolveFieldContext(ResolveFieldContext context)
        {
            Source = (TSource)context.Source;
            FieldName = context.FieldName;
            FieldAst = context.FieldAst;
            FieldDefinition = context.FieldDefinition;
            ReturnType = context.ReturnType;
            ParentType = context.ParentType;
            Arguments = context.Arguments;
            Schema = context.Schema;
            Fragments = context.Fragments;
            RootValue = context.RootValue;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
        }

        public TType GetArgument<TType>(string name, TType defaultValue = default(TType))
        {
            if (!HasArgument(name))
            {
                return defaultValue;
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

        public bool HasArgument(string argumentName)
        {
            return Arguments?.ContainsKey(argumentName) ?? false;
        }
    }

    public class ResolveFieldContext : ResolveFieldContext<object> {}
}
