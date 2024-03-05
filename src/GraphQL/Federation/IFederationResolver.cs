namespace GraphQL.Federation;

internal interface IFederationResolver
{
    Type SourceType { get; }
    ValueTask<object> ResolveAsync(IResolveFieldContext context, object source);
}
