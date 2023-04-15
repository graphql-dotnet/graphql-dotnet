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

        if (fieldType is ObjectFieldType oft && oft.Resolver != null)
        {
            oft.Resolver = new DynamicScopedFieldResolver(oft.Resolver);
        }

        if (fieldType is SubscriptionRootFieldType field && field.StreamResolver != null)
        {
            field.StreamResolver = new DynamicScopedSourceStreamResolver(field.StreamResolver);
        }
    }
}
