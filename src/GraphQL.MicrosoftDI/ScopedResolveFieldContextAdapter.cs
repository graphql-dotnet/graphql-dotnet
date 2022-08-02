using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.MicrosoftDI;

internal sealed class ScopedResolveFieldContextAdapter<TSource> : IResolveFieldContext<TSource>
{
    private static readonly bool _acceptNulls = !typeof(TSource).IsValueType || (typeof(TSource).IsGenericType && typeof(TSource).GetGenericTypeDefinition() == typeof(Nullable<>));

    private readonly IResolveFieldContext _baseContext;

    public ScopedResolveFieldContextAdapter(IResolveFieldContext baseContext, IServiceProvider serviceProvider)
    {
        _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));
        if (baseContext.Source == null && !_acceptNulls)
        {
            throw new ArgumentException("baseContext.Source is null and cannot be cast to non-nullable value type " + typeof(TSource).Name, nameof(baseContext));
        }
        else
        {
            try
            {
                Source = (TSource)baseContext.Source!;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("baseContext.Source is not of type " + typeof(TSource).Name, nameof(baseContext));
            }
        }
        RequestServices = serviceProvider;
    }

    public TSource Source { get; }

    public GraphQLField FieldAst => _baseContext.FieldAst;

    public FieldType FieldDefinition => _baseContext.FieldDefinition;

    public IObjectGraphType ParentType => _baseContext.ParentType;

    public IResolveFieldContext? Parent => _baseContext.Parent;

    public IDictionary<string, ArgumentValue>? Arguments => _baseContext.Arguments;

    public IDictionary<string, DirectiveInfo>? Directives => _baseContext.Directives;

    public object? RootValue => _baseContext.RootValue;

    public ISchema Schema => _baseContext.Schema;

    public GraphQLDocument Document => _baseContext.Document;

    public GraphQLOperationDefinition Operation => _baseContext.Operation;

    public Variables Variables => _baseContext.Variables;

    public CancellationToken CancellationToken => _baseContext.CancellationToken;

    public Metrics Metrics => _baseContext.Metrics;

    public ExecutionErrors Errors => _baseContext.Errors;

    public IEnumerable<object> Path => _baseContext.Path;

    public IEnumerable<object> ResponsePath => _baseContext.ResponsePath;

    public Dictionary<string, (GraphQLField Field, FieldType FieldType)>? SubFields => _baseContext.SubFields;

    public IServiceProvider RequestServices { get; }

    public IDictionary<string, object?> UserContext => _baseContext.UserContext;

    public IReadOnlyDictionary<string, object?> InputExtensions => _baseContext.InputExtensions;

    public IDictionary<string, object?> OutputExtensions => _baseContext.OutputExtensions;

    object? IResolveFieldContext.Source => _baseContext.Source;

    public IExecutionArrayPool ArrayPool => _baseContext.ArrayPool;

    public ClaimsPrincipal? User => _baseContext.User;
}
