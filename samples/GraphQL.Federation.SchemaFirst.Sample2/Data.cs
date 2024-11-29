using GraphQL.Federation.SchemaFirst.Sample2.Schema;

namespace GraphQL.Federation.SchemaFirst.Sample2;

public class Data
{
    private readonly List<Product> _products = new() {
        new Product { Id = 1, Name = "Product 1", CategoryId = 1 },
        new Product { Id = 2, Name = "Product 2", CategoryId = 1 },
        new Product { Id = 3, Name = "Product 3", CategoryId = 2 },
        new Product { Id = 4, Name = "Product 4", CategoryId = 2 },
    };

    public Task<IEnumerable<Product>> GetProductsAsync()
    {
        return Task.FromResult(_products.AsEnumerable());
    }

    public Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int id)
    {
        return Task.FromResult(_products.Where(x => x.CategoryId == id));
    }

    public Task<Product?> GetProductById(int id)
    {
        return Task.FromResult(_products.SingleOrDefault(x => x.Id == id));
    }
}
