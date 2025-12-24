using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores;

public interface IProductsStore
{
    public Task<IDictionary<int, Product>> GetProductsByIdAsync(IEnumerable<int> ids);
    public Task<ILookup<int, Product>> GetReviewsByProductIdsAsync(IEnumerable<int> productIds);
}
