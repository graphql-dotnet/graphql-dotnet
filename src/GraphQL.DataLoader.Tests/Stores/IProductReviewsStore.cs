using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores
{
    public interface IProductReviewsStore
    {
        Task<IEnumerable<ProductReview>> GetAllProductReviewsAsync();
    }
}
