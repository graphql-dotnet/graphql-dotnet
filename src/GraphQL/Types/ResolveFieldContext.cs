using System.Collections.Generic;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Types
{
    public class ResolveFieldContext<TSource>
    {
        public string FieldName { get; set; }

        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public IGraphType ReturnType { get; set; }

        public IObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object RootValue { get; set; }

        public object UserContext { get; set; }

        public TSource Source { get; set; }

        public ISchema Schema { get; set; }

        public Document Document { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public Metrics Metrics { get; set; }

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
            Document = context.Document;
            Fragments = context.Fragments;
            RootValue = context.RootValue;
            UserContext = context.UserContext;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
            Metrics = context.Metrics;
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

    public class ResolveFieldContext : ResolveFieldContext<object>
    {
        internal ResolveFieldContext<TSourceType> As<TSourceType>()
        {
            return new ResolveFieldContext<TSourceType>(this);
        }
    }
}
