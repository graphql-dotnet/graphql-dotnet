using GraphQL.Federation.CodeFirst.Sample3.Models;

namespace GraphQL.Federation.CodeFirst.Sample3;

public class Data
{
    private readonly List<Review> _reviews =
    [
        new Review { Id = 1, ProductId = 1, UserId = 1, Content = "Review 1" },
        new Review { Id = 2, ProductId = 1, UserId = 2, Content = "Review 2" },
        new Review { Id = 3, ProductId = 2, UserId = 1, Content = "Review 3" }
    ];

    public Task<IEnumerable<Review>> GetReviewsAsync() => Task.FromResult<IEnumerable<Review>>(_reviews);

    public Task<Review?> GetReviewByIdAsync(int id)
    {
        return Task.FromResult(_reviews.SingleOrDefault(x => x.Id == id));
    }

    public Task<IEnumerable<Review>> GetReviewsByProductIdAsync(int productId)
    {
        return Task.FromResult(_reviews.Where(x => x.ProductId == productId));
    }

    public Task<IEnumerable<Review>> GetReviewsByUserIdAsync(int userId)
    {
        return Task.FromResult(_reviews.Where(x => x.UserId == userId));
    }
}
