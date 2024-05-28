using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Federation.Extensions;
using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Indicates that the method is a GraphQL Federation resolver.
/// The method should return the same CLR type as the field it resolves.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class FederationResolverAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="FederationResolverAttribute"/>
    public FederationResolverAttribute()
    {
    }

    /// <inheritdoc/>
    public override bool ShouldInclude(MemberInfo memberInfo, bool? isInputType)
        => !(isInputType ?? false);

    /// <inheritdoc/>
    public override void Modify(TypeInformation typeInformation)
    {
        if (typeInformation.Type != typeInformation.MemberInfo.DeclaringType)
        {
            var (clrType, memberDescription, memberType) = GetMemberInfo();
            throw new InvalidOperationException($"The return type of the {memberDescription} {memberType} must be {clrType} or an asynchronous variation.");
        }
        if (typeInformation.IsList)
        {
            var (_, memberDescription, memberType) = GetMemberInfo();
            throw new InvalidOperationException($"The return type of the {memberDescription} {memberType} must not be a list type.");
        }

        (string ClrType, string MemberDescription, string MemberType) GetMemberInfo()
        {
            var clrType = typeInformation.MemberInfo.DeclaringType!.GetFriendlyName();
            var memberDescription = $"{clrType}.{typeInformation.MemberInfo.Name}";
            var memberType = typeInformation.MemberInfo switch
            {
                PropertyInfo => "property",
                MethodInfo => "method",
                FieldInfo => "field",
                _ => "member"
            };
            return (clrType, memberDescription, memberType);
        }
    }

    /// <inheritdoc/>
    public override void Modify(IGraphType graphType, MemberInfo memberInfo, FieldType fieldType, bool isInputType, ref bool ignore)
    {
        if (!isInputType)
        {
            fieldType.IsPrivate = true;
            var isStatic =
                (memberInfo is PropertyInfo pi && (pi.GetMethod?.IsStatic ?? false)) ||
                (memberInfo is MethodInfo mi && mi.IsStatic) ||
                (memberInfo is FieldInfo fi && fi.IsStatic);

            // for static members, generate an IResolveFieldContext where the arguments are the various
            //   properties provided from Apollo Router, and a null source
            // for instance members, generate an IResolveFieldContext where the source is coerced from
            //   the properties provided from Apollo Router, and null arguments
            graphType.Metadata[FederationHelper.RESOLVER_METADATA] = isStatic
                ? new FederationStaticResolver(fieldType)
                : new FederationNonStaticResolver(fieldType, memberInfo.DeclaringType!);
        }
    }

    private class FederationNonStaticResolver : IFederationResolver
    {
        private readonly FieldType _fieldType;

        public FederationNonStaticResolver(FieldType fieldType, Type sourceType)
        {
            _fieldType = fieldType;
            SourceType = sourceType; // EntityResolver will coerce the keys onto the 'source type'
        }

        public Type SourceType { get; }

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source)
        {
            var context2 = new Context(context, source);
            var resolver = _fieldType.Resolver ?? ThrowForNoResolver();
            return resolver.ResolveAsync(context2);

            [StackTraceHidden]
            IFieldResolver ThrowForNoResolver()
                => throw new InvalidOperationException($"The field resolver for {_fieldType.Name} must be set on the field type.");
        }

        private class Context : IResolveFieldContext
        {
            private readonly IResolveFieldContext _context;

            public Context(IResolveFieldContext context, object source)
            {
                _context = context;
                Source = source;
            }

            public GraphQLField FieldAst => _context.FieldAst;
            public FieldType FieldDefinition => _context.FieldDefinition;
            public IObjectGraphType ParentType => _context.ParentType;
            public IResolveFieldContext? Parent => _context.Parent;
            public IDictionary<string, ArgumentValue>? Arguments => null;
            public IDictionary<string, DirectiveInfo>? Directives => _context.Directives;
            public object? RootValue => _context.RootValue;
            public object? Source { get; }
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
        }
    }

    private class FederationStaticResolver : IFederationResolver
    {
        private readonly FieldType _fieldType;

        public FederationStaticResolver(FieldType fieldType)
        {
            _fieldType = fieldType;
        }

        public Type SourceType => typeof(Dictionary<string, object?>);

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source)
        {
            var context2 = new Context(context, _fieldType, (Dictionary<string, object?>)source);
            var resolver = _fieldType.Resolver ?? ThrowForNoResolver();
            return resolver.ResolveAsync(context2);

            [StackTraceHidden]
            IFieldResolver ThrowForNoResolver()
                => throw new InvalidOperationException($"The field resolver for {_fieldType.Name} must be set on the field type.");
        }

        private class Context : IResolveFieldContext
        {
            private readonly IResolveFieldContext _context;

            public Context(IResolveFieldContext context, FieldType fieldType, Dictionary<string, object?> arguments)
            {
                _context = context;
                FieldDefinition = fieldType;

                if (fieldType.Arguments != null && fieldType.Arguments.Count > 0)
                {
                    Arguments = new Dictionary<string, ArgumentValue>();
                    foreach (var arg in fieldType.Arguments)
                    {
                        var matched = arguments.TryGetValue(arg.Name, out var value);
                        if (matched)
                        {
                            value = EntityResolver.Deserialize(arg.Name, arg.ResolvedType!, typeof(object), value);
                        }
                        Arguments[arg.Name] = matched
                            ? new ArgumentValue(value, ArgumentSource.Literal)
                            : new ArgumentValue(arg.DefaultValue, ArgumentSource.FieldDefault);
                    }
                }
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
        }
    }

}
