using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}

namespace GraphQL.Execution
{
    public interface IGraphQLExecuter
    {
        Task<ExecutionResult> ExecuteAsync(
            string operationName,
            string query,
            Inputs variables,
            IDictionary<string, object> context,
            IServiceProvider requestServices,
            CancellationToken cancellationToken = default);
    }

    public interface IGraphQLExecuter<TSchema> : IGraphQLExecuter where TSchema : ISchema
    {
    }

    public class GraphQLExecuter<TSchema>
        : IGraphQLExecuter<TSchema>
        where TSchema : ISchema
    {
        private readonly TSchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly IEnumerable<IDocumentExecutionListener> _listeners;
        private readonly Action<UnhandledExceptionContext> _unhandledExceptionDelegate;

        protected IEnumerable<IValidationRule> CachedDocumentValidationRules { get; init; }
        protected IEnumerable<IValidationRule> ValidationRules { get; init; }
        protected ComplexityConfiguration ComplexityConfiguration { get; init; }
        protected bool ThrowOnUnhandledException { get; init; }
        protected bool EnableMetrics { get; init; }
        protected int? MaxParallelExecutionCount { get; init; }

        public GraphQLExecuter(TSchema schema, IDocumentExecuter documentExecuter, IEnumerable<IDocumentExecutionListener> documentListeners)
        {
            _schema = schema;
            _documentExecuter = documentExecuter;
            _unhandledExceptionDelegate = OnUnhandledException;
            _listeners = documentListeners;
        }

        public virtual Task<ExecutionResult> ExecuteAsync(
            string operationName,
            string query,
            Inputs variables,
            IDictionary<string, object> context,
            IServiceProvider requestServices,
            CancellationToken cancellationToken = default)
        {
            var options = GenerateExecutionOptions(
                operationName,
                query,
                variables,
                context,
                requestServices,
                cancellationToken);
            return _documentExecuter.ExecuteAsync(options);
        }

        protected virtual ExecutionOptions GenerateExecutionOptions(
            string operationName,
            string query,
            Inputs variables,
            IDictionary<string, object> context,
            IServiceProvider requestServices,
            CancellationToken cancellationToken = default)
        {
            var options = new ExecutionOptions
            {
                CachedDocumentValidationRules = CachedDocumentValidationRules,
                ValidationRules = ValidationRules,
                CancellationToken = cancellationToken,
                ComplexityConfiguration = ComplexityConfiguration,
                EnableMetrics = EnableMetrics,
                Inputs = variables,
                ThrowOnUnhandledException = ThrowOnUnhandledException,
                UnhandledExceptionDelegate = _unhandledExceptionDelegate,
                MaxParallelExecutionCount = MaxParallelExecutionCount,
                OperationName = operationName,
                Query = query,
                RequestServices = requestServices,
                Schema = _schema,
                UserContext = context,
            };
            foreach (var listener in _listeners)
            {
                options.Listeners.Add(listener);
            }
            return options;
        }

        protected virtual void OnUnhandledException(UnhandledExceptionContext context)
        {
        }
    }

}
