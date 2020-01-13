using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Execution
{
    public static class ResolveFieldContextSlim
    {
        private static readonly ConcurrentDictionary<System.Type, Func<ExecutionNode, ExecutionContext, IResolveFieldContext>> dic =
            new ConcurrentDictionary<System.Type, Func<ExecutionNode, ExecutionContext, IResolveFieldContext>>();

        public static IResolveFieldContext Create(ExecutionNode node, ExecutionContext context)
        {
            //get the type of the source object, and just use typeof(object) when the source is null
            var sourceType = node.Source?.GetType() ?? typeof(object);

            //retrieve the constructor
            var func = dic.GetOrAdd(sourceType, LazyCreate);

            //execute the constructor and return the new ResolveFieldContextSlim<T> (cast as IResolveFieldContext)
            return func(node, context);
        }

        private static Func<ExecutionNode, ExecutionContext, IResolveFieldContext> LazyCreate(System.Type sourceType)
        {
            //rfcsType = typeof(ResolveFieldContextSlim<sourceType>);
            var rfcsType = typeof(ResolveFieldContextSlim<>).MakeGenericType(sourceType);

            //constructor = (the only constructor)
            var constructor = rfcsType.GetConstructors()[0];

            //create the 2 parameters to the lambda: executionNode and executionContext
            var executionNodeParameter = Expression.Parameter(typeof(ExecutionNode), "executionNode");
            var executionContextParameter = Expression.Parameter(typeof(ExecutionContext), "executionContext");

            //body = new ResolveFieldContextSlim<sourceType>(executionNode, executionContext);
            var expressionBody = Expression.New(constructor, executionNodeParameter, executionContextParameter);

            //lambda expression = (executionNode, executionContext) => new ResolveFieldContextSlim<sourceType>(executionNode, executionContext);
            var lambdaExpression = Expression.Lambda<Func<ExecutionNode, ExecutionContext, IResolveFieldContext>>(expressionBody, executionNodeParameter, executionContextParameter);

            //compile the expression into a function and return it
            return lambdaExpression.Compile();
        }
    }

    public class ResolveFieldContextSlim<T> : IResolveFieldContext<T>
    {
        private readonly ExecutionNode _executionNode;
        private readonly ExecutionContext _executionContext;
        private readonly Lazy<Dictionary<string, object>> _arguments;
        private readonly Lazy<IDictionary<string, Field>> _subFields;

        public ResolveFieldContextSlim(ExecutionNode node, ExecutionContext context)
        {
            _executionNode = node;
            _executionContext = context;
            _arguments = new Lazy<Dictionary<string, object>>(LazyArgumentInitializer, LazyThreadSafetyMode.PublicationOnly);
            _subFields = new Lazy<IDictionary<string, Field>>(LazySubfieldInitializer, LazyThreadSafetyMode.PublicationOnly);
        }

        private IDictionary<string, Field> LazySubfieldInitializer()
        {
            return ExecutionHelper.SubFieldsFor(_executionContext, _executionNode.FieldDefinition.ResolvedType, _executionNode.Field);
        }

        private Dictionary<string, object> LazyArgumentInitializer()
        {
            return ExecutionHelper.GetArgumentValues(_executionContext.Schema, _executionNode.FieldDefinition.Arguments, _executionNode.Field.Arguments, _executionContext.Variables);
        }

        public T Source => (T)_executionNode.Source;

        public string FieldName => _executionNode.Field.Name;

        public Language.AST.Field FieldAst => _executionNode.Field;

        public FieldType FieldDefinition => _executionNode.FieldDefinition;

        public IGraphType ReturnType => _executionNode.FieldDefinition.ResolvedType;

        public IObjectGraphType ParentType => _executionNode.GetParentType(_executionContext.Schema);

        public Dictionary<string, object> Arguments => _arguments.Value;

        public object RootValue => _executionContext.RootValue;

        public ISchema Schema => _executionContext.Schema;

        public Document Document => _executionContext.Document;

        public Operation Operation => _executionContext.Operation;

        public Fragments Fragments => _executionContext.Fragments;

        public Variables Variables => _executionContext.Variables;

        public CancellationToken CancellationToken => _executionContext.CancellationToken;

        public Metrics Metrics => _executionContext.Metrics;

        public ExecutionErrors Errors => _executionContext.Errors;

        public IEnumerable<string> Path => _executionNode.Path;

        public IDictionary<string, Language.AST.Field> SubFields => _subFields.Value;

        public IDictionary<string, object> UserContext => _executionContext.UserContext;

        object IResolveFieldContext.Source => _executionNode.Source;
    }
}
