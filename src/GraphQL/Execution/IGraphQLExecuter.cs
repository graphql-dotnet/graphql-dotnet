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
        private readonly IEnumerable<Action<IServiceProvider, ExecutionOptions>> _configurations;
        private readonly ComplexityConfiguration _complexityConfiguration;
        private readonly Action<UnhandledExceptionContext> _onUnhandledExceptionDelegate; //eliminate runtime allocation by caching delegate
        //private readonly Action<UnhandledExceptionContext> _ctorUnhandledExceptionDelegate;

        //protected IEnumerable<IValidationRule> CachedDocumentValidationRules { get; init; }
        //protected IEnumerable<IValidationRule> ValidationRules { get; init; }
        //protected bool ThrowOnUnhandledException { get; init; }
        //protected bool EnableMetrics { get; init; }
        //protected int? MaxParallelExecutionCount { get; init; }

        public GraphQLExecuter(TSchema schema, IDocumentExecuter documentExecuter)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _documentExecuter = documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter));
            _onUnhandledExceptionDelegate = OnUnhandledException;
        }

        public GraphQLExecuter(TSchema schema, IDocumentExecuter documentExecuter, IEnumerable<Action<IServiceProvider, ExecutionOptions>> configurations)
            : this(schema, documentExecuter)
        {
            _configurations = configurations;
        }

        public GraphQLExecuter(TSchema schema, IDocumentExecuter documentExecuter, IEnumerable<Action<IServiceProvider, ExecutionOptions>> configurations, ComplexityConfiguration complexityConfiguration)
            : this(schema, documentExecuter, configurations)
        {
            _complexityConfiguration = complexityConfiguration; // Support null so that if the DI returns a null value (due to IOptions mapping) it will still work
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
                //CachedDocumentValidationRules = CachedDocumentValidationRules,
                //ValidationRules = ValidationRules,
                CancellationToken = cancellationToken,
                ComplexityConfiguration = _complexityConfiguration,
                //EnableMetrics = EnableMetrics,
                Inputs = variables,
                //ThrowOnUnhandledException = ThrowOnUnhandledException,
                UnhandledExceptionDelegate = _onUnhandledExceptionDelegate,
                //MaxParallelExecutionCount = MaxParallelExecutionCount,
                OperationName = operationName,
                Query = query,
                RequestServices = requestServices,
                Schema = _schema,
                UserContext = context,
            };

            if (_configurations != null)
            {
                foreach (var configuration in _configurations)
                {
                    configuration(requestServices, options);
                }
            }

            return options;
        }

        protected virtual void OnUnhandledException(UnhandledExceptionContext context)
        {
            //_ctorUnhandledExceptionDelegate?.Invoke(context);
        }
    }

}
