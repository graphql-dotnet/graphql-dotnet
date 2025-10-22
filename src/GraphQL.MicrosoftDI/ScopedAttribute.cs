using GraphQL.MicrosoftDI;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Creates a dedicated service scope during the field resolver's execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ScopedAttribute : GraphQLAttribute
{
    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            return;

        // Apply scoped middleware to the field resolver
        fieldType.ApplyMiddleware(ScopedFieldMiddleware.Instance);

        // Wrap the stream resolver, if any, to create a scope for subscriptions
        if (fieldType.StreamResolver != null && fieldType.StreamResolver is not DynamicScopedSourceStreamResolver)
            fieldType.StreamResolver = new DynamicScopedSourceStreamResolver(fieldType.StreamResolver);
    }
}
