using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores
{
    public interface IProductsStore
    {
        Task<IDictionary<int, Product>> GetProductsByIdAsync(IEnumerable<int> ids);
        Task<ILookup<int, Product>> GetReviewsByProductIdsAsync(IEnumerable<int> productIds);
    }
}
