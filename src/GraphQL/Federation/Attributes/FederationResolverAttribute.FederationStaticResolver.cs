using System.Diagnostics;
using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Federation.Resolvers;
using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Federation;

public partial class FederationResolverAttribute
{
    /// <summary>
    /// This federation resolver creates an <see cref="IResolveFieldContext"/> which has arguments matching
    /// the entity representation properties provided from Apollo Router, and a null source. It then calls
    /// the configured field resolver with the context. It is intended to be used for static federation
    /// resolvers in a type-first schema.
    /// </summary>
    /// <remarks>
    /// When the field is resolved, the type-first resolver will be called with the context, so when
    /// the resolver generated by <see cref="AutoRegisteringInputObjectGraphType"/> is called, it will
    /// pull the arguments (via <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)"/>)
    /// from the entity representation properties. In this way all type-first attributes can be used with
    /// federation resolvers, such as <see cref="FromServicesAttribute"/>. Note that any input objects
    /// will be registered as input object graph types within the schema; mark them with
    /// <see cref="IGraphType.IsPrivate"/> to prevent them from being exposed.
    /// </remarks>
    private partial class FederationStaticResolver : IFederationResolver
    {
        private readonly FieldType _fieldType;

        public FederationStaticResolver(FieldType fieldType)
        {
            _fieldType = fieldType;
        }

        public bool MatchKeys(IDictionary<string, object?> representation)
        {
            var args = _fieldType.Arguments;

            if (args == null || args.Count == 0)
                return true;

            foreach (var arg in args)
            {
                if (!representation.ContainsKey(arg.Name))
                    return false;
            }

            return true;
        }

        public object ParseRepresentation(IComplexGraphType graphType, IDictionary<string, object?> representation)
        {
            // Creates a dictionary of arguments for the field type based on the entity representation properties.
            // The argument dictionary is returned as the parsed representation, to be used by the synthesized IResolveFieldContext.

            if (_fieldType.Arguments == null || _fieldType.Arguments.Count == 0)
            {
                // IResolveFieldContext.Arguments may return null if the field has no arguments, so just return null here
                return null!;
            }

            var arguments = new Dictionary<string, ArgumentValue>();
            foreach (var arg in _fieldType.Arguments)
            {
                // check if the representation contains a value for the argument
                var matched = representation.TryGetValue(arg.Name, out var value);
                if (matched)
                {
                    // deserialize the value based on the argument's graph type
                    value = Deserialize(arg.ResolvedType!, arg.Name, value);
                    // coerce the value based on the argument's parser, useful for coercing ID strings to integers, etc
                    if (arg.Parser != null && value != null)
                        value = arg.Parser(value);
                }
                // set the argument value based on the matched value or the default value otherwise
                arguments[arg.Name] = matched
                    ? new ArgumentValue(value, ArgumentSource.Literal) // ArgumentSource not used by GetArgument or the AutoRegisteringObjectGraphType field resolver
                    : new ArgumentValue(arg.DefaultValue, ArgumentSource.FieldDefault);
            }
            return arguments;
        }

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, IComplexGraphType graphType, object parsedRepresentation)
        {
            // create a synthesized IResolveFieldContext with the arguments and a null source
            var context2 = new Context(context, _fieldType, (Dictionary<string, ArgumentValue>?)parsedRepresentation);
            // call the field resolver with the synthesized context
            var resolver = _fieldType.Resolver ?? ThrowForNoResolver();
            return resolver.ResolveAsync(context2);

            [DoesNotReturn]
            [StackTraceHidden]
            IFieldResolver ThrowForNoResolver()
                => throw new InvalidOperationException($"The field resolver for {_fieldType.Name} must be set on the field type.");
        }

        private class Context : IResolveFieldContext
        {
            private readonly IResolveFieldContext _context;

            /// <summary>
            /// Initializes the context with <see cref="Arguments"/> for each field argument specified within
            /// <paramref name="fieldType"/>. The arguments are coerced from the entity representation properties
            /// supplied by <paramref name="arguments"/>.
            /// </summary>
            public Context(IResolveFieldContext context, FieldType fieldType, Dictionary<string, ArgumentValue>? arguments)
            {
                _context = context;
                FieldDefinition = fieldType;
                Arguments = arguments;
            }

            public GraphQLField FieldAst => _context.FieldAst;
            public FieldType FieldDefinition { get; }
            public IObjectGraphType ParentType => _context.ParentType;
            public IResolveFieldContext? Parent => _context.Parent;
            public IDictionary<string, ArgumentValue>? Arguments { get; }
            public IDictionary<string, DirectiveInfo>? Directives => _context.Directives;
            public object? RootValue => _context.RootValue;
            public object? Source => null;
            public ISchema Schema => _context.Schema;
            public GraphQLDocument Document => _context.Document;
            public GraphQLOperationDefinition Operation => _context.Operation;
            public Variables Variables => _context.Variables;
            public CancellationToken CancellationToken => _context.CancellationToken;
            public Metrics Metrics => _context.Metrics;
            public ExecutionErrors Errors => _context.Errors;
            public IEnumerable<object> Path => _context.Path;
            public IEnumerable<object> ResponsePath => _context.ResponsePath;
            public Dictionary<string, (GraphQLField Field, FieldType FieldType)>? SubFields => _context.SubFields;
            public IReadOnlyDictionary<string, object?> InputExtensions => _context.InputExtensions;
            public IDictionary<string, object?> OutputExtensions => _context.OutputExtensions;
            public IServiceProvider? RequestServices => _context.RequestServices;
            public IExecutionArrayPool ArrayPool => _context.ArrayPool;
            public ClaimsPrincipal? User => _context.User;
            public IDictionary<string, object?> UserContext => _context.UserContext;
            public IExecutionContext ExecutionContext => _context.ExecutionContext;
        }
    }
}
