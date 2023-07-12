using GraphQL.Federation.SchemaFirst.Sample1.Schema;
using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.SchemaFirst.Sample1;

public class Data
{
    private readonly List<Category> _categories = new List<Category>() {
        new Category { Id = 1, Name = "Category 1" },
        new Category { Id = 2, Name = "Category 2" },
    };

    public Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return Task.FromResult(_categories.AsEnumerable());
    }

    public IFederatedResolver GetResolver<T>()
        where T : IHasId
        => typeof(T).Name switch
        {
            nameof(Category) => new MyFederatedResolver<Category>(_categories),
            _ => throw new InvalidOperationException("Invalid type")
        };

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
            if (context.Arguments.TryGetValue("id", out var idValue))
            {
                return Task.FromResult<object?>(_list.FirstOrDefault(x => x.Id == (int)Convert.ChangeType(idValue, typeof(int))!));
            }
            return Task.FromResult<object?>(null);
        }
    }
}
