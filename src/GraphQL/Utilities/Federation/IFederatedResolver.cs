namespace GraphQL.Utilities.Federation
{
    public interface IFederatedResolver
    {
        Task<object?> Resolve(FederatedResolveContext context);
    }
}
