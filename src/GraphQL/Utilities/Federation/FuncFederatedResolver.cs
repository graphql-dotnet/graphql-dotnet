namespace GraphQL.Utilities.Federation
{
    public class FuncFederatedResolver<T> : IFederatedResolver
    {
        private readonly Func<FederatedResolveContext, Task<T?>> _resolver;

        public FuncFederatedResolver(Func<FederatedResolveContext, Task<T?>> func)
        {
            _resolver = func;
        }

        public async Task<object?> Resolve(FederatedResolveContext context)
        {
            return await _resolver(context).ConfigureAwait(false);
        }
    }
}
