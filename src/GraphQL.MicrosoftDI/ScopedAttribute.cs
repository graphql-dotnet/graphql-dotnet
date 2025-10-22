using GraphQL.Instrumentation;
using GraphQL.MicrosoftDI;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Creates a dedicated service scope during the field resolver's execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ScopedAttribute : GraphQLAttribute
{
    // Cached transform delegate to avoid allocations
    private static readonly Func<IServiceProvider, IFieldMiddleware> _scopedTransform = static _ => ScopedFieldMiddleware.Instance;

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            return;

        // Apply scoped middleware to the field using transform function
        var existingTransform = fieldType.Middleware;
        if (existingTransform == null)
        {
            fieldType.Middleware = _scopedTransform;
        }
        else
        {
            // Chain the middleware
            fieldType.Middleware = serviceProvider =>
            {
                var existing = existingTransform(serviceProvider);
                return new ScopedFieldMiddleware(existing);
            };
        }

        // Wrap the stream resolver, if any, to create a scope for subscriptions
        if (fieldType.StreamResolver != null && fieldType.StreamResolver is not DynamicScopedSourceStreamResolver)
            fieldType.StreamResolver = new DynamicScopedSourceStreamResolver(fieldType.StreamResolver);
    }
}
