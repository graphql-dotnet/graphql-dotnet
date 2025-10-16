using GraphQL.Federation.SchemaFirst.Sample1.Schema;

namespace GraphQL.Federation.SchemaFirst.Sample1;

public class Data
{
    private readonly List<Category> _categories =
    [
        new Category { Id = 1, Name = "Category 1" },
        new Category { Id = 2, Name = "Category 2" }
    ];

    public Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return Task.FromResult(_categories.AsEnumerable());
    }

    public Task<Category?> GetCategoryById(int id)
    {
        return Task.FromResult(_categories.SingleOrDefault(x => x.Id == id));
    }
}
