using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using ExecutionContext = GraphQL.Execution.ExecutionContext;
using Field = GraphQL.Language.AST.Field;
using Type = System.Type;

namespace GraphQL.Types
{
    public class ResolveFieldContext
    {
        public ResolveFieldContext()
        {
        }

        public ResolveFieldContext(ResolveFieldContext context)
        {
            SourceObject = context.SourceObject;
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
            Errors = context.Errors;
            SubFields = context.SubFields;
        }

        public ResolveFieldContext(ExecutionContext context, Field field, FieldType type, object source,
            IObjectGraphType parentType, Dictionary<string, object> arguments, IEnumerable<string> path)
        {
            SourceObject = source;
            FieldName = field.Name;
            FieldAst = field;
            FieldDefinition = type;
            ReturnType = type.ResolvedType;
            ParentType = parentType;
            Arguments = arguments;
            Schema = context.Schema;
            Document = context.Document;
            Fragments = context.Fragments;
            RootValue = context.RootValue;
            UserContext = context.UserContext;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
            Metrics = context.Metrics;
            Errors = context.Errors;
            Path = path;
        }

        public string FieldName { get; set; }

        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public IGraphType ReturnType { get; set; }

        public IObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object RootValue { get; set; }

        public object UserContext { get; set; }

        public object SourceObject { get; set; }

        public ISchema Schema { get; set; }

        public Document Document { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public Metrics Metrics { get; set; }

        public ExecutionErrors Errors { get; set; }

        public IEnumerable<string> Path { get; set; }

        /// <summary>
        ///     Queried sub fields
        /// </summary>
        public IDictionary<string, Field> SubFields { get; set; }

        public TType GetArgument<TType>(string name, TType defaultValue = default(TType))
        {
            return (TType) GetArgument(typeof(TType), name, defaultValue);
        }

        public object GetArgument(Type argumentType, string name, object defaultValue = null)
        {
            if (!HasArgument(name)) return defaultValue;

            var arg = Arguments[name];
            if (arg is Dictionary<string, object> inputObject)
            {
                var type = argumentType;
                if (type.Namespace?.StartsWith("System") == true) return arg;

                return inputObject.ToObject(type);
            }

            return arg.GetPropertyValue(argumentType);
        }

        public bool HasArgument(string argumentName)
        {
            return Arguments?.ContainsKey(argumentName) ?? false;
        }

        public async Task<object> TryAsyncResolve(Func<ResolveFieldContext, Task<object>> resolve, Func<ExecutionErrors, Task<object>> error = null)
        {
            try
            {
                return await resolve(this);
            }
            catch (Exception ex)
            {
                if (error == null)
                {
                    var er = new ExecutionError(ex.Message, ex);
                    er.AddLocation(FieldAst, Document);
                    er.Path = Path;
                    Errors.Add(er);
                    return null;
                }
                else
                {
                    return error(Errors);
                }
            }
        }
    }

    public class ResolveFieldContext<TSource> : ResolveFieldContext
    {
        public ResolveFieldContext(ResolveFieldContext context) : base(context)
        {
        }

        public TSource Source => (TSource) SourceObject;
    }
}
