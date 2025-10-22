using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Applies field-specific middleware to field resolvers before schema-wide middleware is applied.
/// This visitor wraps field resolvers with any middleware configured on individual fields via
/// <see cref="FieldType.MiddlewareFactory"/>.
/// </summary>
internal sealed class FieldMiddlewareVisitor : BaseSchemaNodeVisitor
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve middleware instances.</param>
    public FieldMiddlewareVisitor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        var middlewareTransform = field.MiddlewareFactory;
        if (middlewareTransform != null)
        {
            var inner = field.Resolver ?? (field.StreamResolver == null ? NameFieldResolver.Instance : SourceFieldResolver.Instance);

            // Apply the middleware transform to wrap the resolver
            FieldMiddlewareDelegate wrappedDelegate = middlewareTransform(_serviceProvider, inner.ResolveAsync);
            field.Resolver = new FuncFieldResolver<object>(wrappedDelegate.Invoke);

            // Clear the middleware transform as it has been applied
            field.MiddlewareFactory = null;
        }
    }
}
