using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Provides extension methods for configuring field metadata.
/// </summary>
public static class FieldExtensions
{
    /// <summary>
    /// Instructs the GraphQL input object type to bypass automatic CLR mapping for the field.
    /// </summary>
    /// <remarks>
    /// This extension method sets a specific metadata flag on the field (using the keys defined on <see cref="InputObjectGraphType"/>)
    /// to indicate that the field should not be automatically bound to a property on the corresponding CLR type.
    /// This is particularly useful when the input type defines a field that is computed or otherwise does not have a matching
    /// CLR property. In such cases, developers typically override <see cref="InputObjectGraphType{TSourceType}.ParseDictionary(IDictionary{string, object?})"/>
    /// to handle the conversion between the input and CLR object.
    /// </remarks>
    [AllowedOn<IInputObjectGraphType>]
    public static TMetadataWriter NoClrMapping<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.WithMetadata(InputObjectGraphType.ORIGINAL_EXPRESSION_PROPERTY_NAME, InputObjectGraphType.SKIP_EXPRESSION_VALUE_NAME);

    /// <summary>
    /// Specifies that the field depends on a specific service type provided by the dependency injection provider.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public static TMetadataWriter DependsOn<TMetadataWriter>(this TMetadataWriter fieldType, Type serviceType)
        where TMetadataWriter : IFieldMetadataWriter
    {
        var keys = fieldType.GetMetadata<List<Type>>(FromServicesAttribute.REQUIRED_SERVICES_METADATA);
        if (keys == null)
        {
            keys = [];
            fieldType.Metadata[FromServicesAttribute.REQUIRED_SERVICES_METADATA] = keys;
        }
        keys.Add(serviceType);
        return fieldType;
    }

    /// <summary>
    /// Applies middleware to the field. If middleware is already set, the new middleware will be chained after the existing middleware.
    /// </summary>
    /// <param name="fieldType">The field type to apply middleware to.</param>
    /// <param name="middleware">The middleware to apply.</param>
    /// <remarks>
    /// Note: Schema-level field middleware (configured via <see cref="ISchema.FieldMiddleware"/>) executes before
    /// field-specific middleware in the middleware pipeline.
    /// </remarks>
    [AllowedOn<IObjectGraphType>]
    public static void ApplyMiddleware(this FieldType fieldType, IFieldMiddleware middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        var existingTransform = fieldType.Middleware;
        fieldType.Middleware = (serviceProvider, next) =>
        {
            FieldMiddlewareDelegate newNext = ctx => middleware.ResolveAsync(ctx, next);
            return existingTransform == null
                ? newNext
                : existingTransform(serviceProvider, newNext);
        };
    }

    /// <summary>
    /// Applies middleware to the field by resolving it from the service provider. If middleware is already set, the new middleware will be chained after the existing middleware.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of middleware to resolve from the service provider.</typeparam>
    /// <param name="fieldType">The field type to apply middleware to.</param>
    /// <remarks>
    /// Note: Schema-level field middleware (configured via <see cref="ISchema.FieldMiddleware"/>) executes before
    /// field-specific middleware in the middleware pipeline.
    /// </remarks>
    [AllowedOn<IObjectGraphType>]
    public static void ApplyMiddleware<TMiddleware>(this FieldType fieldType)
        where TMiddleware : IFieldMiddleware
    {
        var existingTransform = fieldType.Middleware;
        if (existingTransform == null)
        {
            // Use static lambda if possible
            fieldType.Middleware = static (serviceProvider, next) =>
            {
                var middleware = serviceProvider.GetRequiredService<TMiddleware>();
                return ctx => middleware.ResolveAsync(ctx, next);
            };
        }
        else
        {
            // Chain the middleware
            fieldType.Middleware = (serviceProvider, next) =>
            {
                var middleware = serviceProvider.GetRequiredService<TMiddleware>();
                FieldMiddlewareDelegate newNext = ctx => middleware.ResolveAsync(ctx, next);
                return existingTransform(serviceProvider, newNext);
            };
        }
    }

    /// <summary>
    /// Applies middleware to the field using a delegate. If middleware is already set, the new middleware will be chained after the existing middleware.
    /// </summary>
    /// <param name="fieldType">The field type to apply middleware to.</param>
    /// <param name="middleware">The middleware delegate to apply.</param>
    /// <remarks>
    /// Note: Schema-level field middleware (configured via <see cref="ISchema.FieldMiddleware"/>) executes before
    /// field-specific middleware in the middleware pipeline.
    /// </remarks>
    [AllowedOn<IObjectGraphType>]
    public static void ApplyMiddleware(this FieldType fieldType, Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        var existingTransform = fieldType.Middleware;
        fieldType.Middleware = (serviceProvider, next) =>
        {
            FieldMiddlewareDelegate newNext = middleware(next);
            return existingTransform == null
                ? newNext
                : existingTransform(serviceProvider, newNext);
        };
    }
}
