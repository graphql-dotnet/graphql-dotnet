using GraphQL.Federation.Sample2.Schema;
using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.Sample2;

public class Data
{
    private readonly List<Product> _products = new List<Product>() {
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

    public IFederatedResolver GetResolver<T>()
        where T : IHasId
        => typeof(T).Name switch
        {
            nameof(Category) => new MyPseudoFederatedResolver<Category>(),
            nameof(Product) => new MyFederatedResolver<Product>(_products),
            _ => throw new InvalidOperationException("Invalid type")
        };

    /// <summary>
    /// Creates a new instance of <typeparamref name="T"/> for any object requested to be resolved.
    /// Used for <see cref="Category"/> since there is no data stored in this repository for categories.
    /// </summary>
    private class MyPseudoFederatedResolver<T> : IFederatedResolver
        where T : IHasId, new()
    {
        public Task<object?> Resolve(FederatedResolveContext context)
        {
            if (context.Arguments.TryGetValue("id", out var idValue) && idValue is int id)
            {
                return Task.FromResult<object?>(new T() { Id = id });
            }
            return Task.FromResult<object?>(null);
        }
    }

    /// <summary>
    /// Retrieves the local instance of <typeparamref name="T"/> from the repository.
    /// </summary>
    private class MyFederatedResolver<T> : IFederatedResolver
        where T : IHasId
    {
        private readonly List<T> _list;

        public MyFederatedResolver(List<T> list)
        {
            _list = list;
        }

        public Task<object?> Resolve(FederatedResolveContext context)
        {
            if (context.Arguments.TryGetValue("id", out var idValue) && idValue is int id)
            {
                return Task.FromResult<object?>(_list.FirstOrDefault(x => x.Id == id));
            }
            return Task.FromResult<object?>(null);
        }
    }
}
