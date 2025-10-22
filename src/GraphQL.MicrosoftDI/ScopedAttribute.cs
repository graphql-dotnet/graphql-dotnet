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
    private static readonly Func<IServiceProvider, FieldMiddlewareDelegate, FieldMiddlewareDelegate> _scopedTransform =
        static (_, next) => ctx => ScopedFieldMiddleware.Instance.ResolveAsync(ctx, next);

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            return;

        // Apply scoped middleware to the field using transform function
        var existingTransform = fieldType.MiddlewareFactory;
        if (existingTransform == null)
        {
            fieldType.MiddlewareFactory = _scopedTransform;
        }
        else
        {
            // Chain the middleware
            fieldType.MiddlewareFactory = (serviceProvider, next) =>
            {
                FieldMiddlewareDelegate newNext = ctx => ScopedFieldMiddleware.Instance.ResolveAsync(ctx, next);
                return existingTransform(serviceProvider, newNext);
            };
        }

        // Wrap the stream resolver, if any, to create a scope for subscriptions
        if (fieldType.StreamResolver != null && fieldType.StreamResolver is not DynamicScopedSourceStreamResolver)
            fieldType.StreamResolver = new DynamicScopedSourceStreamResolver(fieldType.StreamResolver);
    }
}
