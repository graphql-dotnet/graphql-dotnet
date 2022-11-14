namespace GraphQL.Federation;

internal interface IFederationResolver
{
    Type SourceType { get; }
    object Resolve(IResolveFieldContext context, object source);
}
