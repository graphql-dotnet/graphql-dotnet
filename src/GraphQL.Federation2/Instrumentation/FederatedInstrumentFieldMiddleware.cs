using GraphQL.Instrumentation;

namespace GraphQL.Federation.Instrumentation;

/// <summary>
/// Middleware required for Apollo federated tracing to record performance metrics of field.
/// </summary>
public class FederatedInstrumentFieldMiddleware : IFieldMiddleware
{
    /// <inheritdoc/>
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        var metadata = new Dictionary<string, object?>
        {
            { "responseName", context.FieldDefinition.Name },
            { "type", context.FieldDefinition.ResolvedType!.ToString() },
            { "parentType", context.ParentType.Name },
            { "path", context.Path },
            { "errors", context.Errors },
        };

        using (context.Metrics.Subject("federatedfield", context.FieldAst.Name.StringValue, metadata))
        {
            return await next(context).ConfigureAwait(false);
        }
    }
}
