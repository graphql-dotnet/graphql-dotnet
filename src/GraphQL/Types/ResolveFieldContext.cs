using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Field = GraphQL.Language.AST.Field;
using GraphQL.Execution;

namespace GraphQL.Types
{
    public class ResolveFieldContext<TSource> : IProvideUserContext, IResolveFieldContext<TSource>
    {
        public string FieldName { get; set; }

        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public IGraphType ReturnType { get; set; }

        public IObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object RootValue { get; set; }

        public IDictionary<string, object> UserContext { get; set; }

        public TSource Source { get; set; }

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
        /// Queried sub fields
        /// </summary>
        public IDictionary<string, Field> SubFields { get; set; }

        object IResolveFieldContext.Source => Source;

        public ResolveFieldContext() { }

        public ResolveFieldContext(IResolveFieldContext context)
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
            Errors = context.Errors;
            SubFields = context.SubFields;
            Path = context.Path;
        }

        public TType GetArgument<TType>(string name, TType defaultValue = default)
        {
            return ResolveFieldContextExtensions.GetArgument(this, name, defaultValue);
        }

        public object GetArgument(System.Type argumentType, string name, object defaultValue = null)
        {
            return ResolveFieldContextExtensions.GetArgument(this, argumentType, name, defaultValue);
        }

        public bool HasArgument(string argumentName) => ResolveFieldContextExtensions.HasArgument(this, argumentName);

        public Task<object> TryAsyncResolve(Func<ResolveFieldContext<TSource>, Task<object>> resolve, Func<ExecutionErrors, Task<object>> error = null)
        {
            return TryAsyncResolve<object>(resolve, error);
        }

        public async Task<TResult> TryAsyncResolve<TResult>(Func<ResolveFieldContext<TSource>, Task<TResult>> resolve, Func<ExecutionErrors, Task<TResult>> error = null)
        {
            try
            {
                return await resolve(this).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (error == null)
                {
                    var er = new ExecutionError(ex.Message, ex);
                    er.AddLocation(FieldAst, Document);
                    er.Path = Path;
                    Errors.Add(er);
                    return default;
                }
                else
                {
                    var result = error(Errors);
                    return result == null ? default : await result.ConfigureAwait(false);
                }
            }
        }
    }

    public class ResolveFieldContext : ResolveFieldContext<object>
    {
        internal ResolveFieldContext<TSourceType> As<TSourceType>()
        {
            if (this is ResolveFieldContext<TSourceType> typedContext)
                return typedContext;

            return new ResolveFieldContext<TSourceType>(this);
        }

        public ResolveFieldContext()
        {
        }

        public ResolveFieldContext(GraphQL.Execution.ExecutionContext context, Field field, FieldType type, object source, IObjectGraphType parentType, Dictionary<string, object> arguments, IEnumerable<string> path)
        {
            Source = source;
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
    }
}
